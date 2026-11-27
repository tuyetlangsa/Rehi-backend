#!/usr/bin/env bash
# Drive an isolated Rehi API instance for verification.
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../../../.." && pwd)"
API_PORT="${REHI_VERIFY_API_PORT:-5299}"
PG_PORT="${REHI_VERIFY_PG_PORT:-55432}"
PG_CONTAINER="rehi-verify-pg-${PG_PORT}"
PG_PASSWORD="verify"
SMTP_PORT="${REHI_VERIFY_SMTP_PORT:-51025}"
MAIL_PORT="${REHI_VERIFY_MAIL_PORT:-58025}"
MAIL_CONTAINER="rehi-verify-mailpit-${SMTP_PORT}"
MAIL_IMAGE="axllent/mailpit:v1.21.8"
MAIL_URL="http://127.0.0.1:${MAIL_PORT}"
CACHE_ROOT="${XDG_CACHE_HOME:-$HOME/.cache}/rehi-verify"
STATE_DIR="${CACHE_ROOT}/run-${API_PORT}"
EVIDENCE_ROOT="${CACHE_ROOT}/evidence"
BASE_URL="http://127.0.0.1:${API_PORT}"
DLL="${REPO_ROOT}/Rehi.Apis/bin/Debug/net8.0/Rehi.Apis.dll"

die() { echo "verify: $*" >&2; exit 1; }

api_pid() { [[ -f "${STATE_DIR}/api.pid" ]] && cat "${STATE_DIR}/api.pid" || true; }

pid_is_ours() {
  local pid="$1"
  [[ -n "$pid" ]] && ps -p "$pid" -o command= 2>/dev/null | grep -q "Rehi.Apis.dll"
}

evidence_dir() {
  [[ -f "${STATE_DIR}/run-id" ]] || die "no running instance on port ${API_PORT}; run 'verify.sh up' first"
  echo "${EVIDENCE_ROOT}/$(cat "${STATE_DIR}/run-id")"
}

psql_q() {
  docker exec "${PG_CONTAINER}" psql -h 127.0.0.1 -U postgres -d rehi -v ON_ERROR_STOP=1 -tAc "$1"
}

cmd_up() {
  if pid_is_ours "$(api_pid)"; then
    die "an instance is already running on port ${API_PORT} (pid $(api_pid)). Reuse it, or set REHI_VERIFY_API_PORT/REHI_VERIFY_PG_PORT for a second one."
  fi
  for port in "${API_PORT}" "${PG_PORT}" "${SMTP_PORT}" "${MAIL_PORT}"; do
    if lsof -nP -iTCP:"${port}" -sTCP:LISTEN >/dev/null 2>&1; then
      die "port ${port} is already in use by something this script did not start. Pick other ports via REHI_VERIFY_API_PORT/REHI_VERIFY_PG_PORT/REHI_VERIFY_SMTP_PORT/REHI_VERIFY_MAIL_PORT."
    fi
  done
  docker info >/dev/null 2>&1 || die "docker is not running"

  mkdir -p "${STATE_DIR}"
  local run_id; run_id="$(date +%Y%m%d-%H%M%S)-${API_PORT}"
  echo "${run_id}" > "${STATE_DIR}/run-id"
  mkdir -p "${EVIDENCE_ROOT}/${run_id}"

  echo "verify: starting postgres ${PG_CONTAINER} on 127.0.0.1:${PG_PORT} (tmpfs, discarded on down)"
  docker rm -f "${PG_CONTAINER}" >/dev/null 2>&1 || true
  docker run -d --name "${PG_CONTAINER}" --label rehi-verify=1 \
    -e POSTGRES_DB=rehi -e POSTGRES_USER=postgres -e POSTGRES_PASSWORD="${PG_PASSWORD}" \
    -p "127.0.0.1:${PG_PORT}:5432" --tmpfs /var/lib/postgresql/data postgres:17 >/dev/null

  echo "verify: starting mailpit ${MAIL_CONTAINER} (SMTP 127.0.0.1:${SMTP_PORT}, HTTP ${MAIL_URL})"
  docker rm -f "${MAIL_CONTAINER}" >/dev/null 2>&1 || true
  docker run -d --name "${MAIL_CONTAINER}" --label rehi-verify=1 \
    -e MP_SMTP_AUTH_ACCEPT_ANY=1 -e MP_SMTP_AUTH_ALLOW_INSECURE=1 \
    -p "127.0.0.1:${SMTP_PORT}:1025" -p "127.0.0.1:${MAIL_PORT}:8025" "${MAIL_IMAGE}" >/dev/null

  # pg_isready can pass while the image's init scripts still run; a TCP query only succeeds after init.
  local i
  for i in $(seq 1 60); do
    psql_q "select 1" >/dev/null 2>&1 && break
    [[ $i -eq 60 ]] && die "postgres did not accept TCP queries within 60s (docker logs ${PG_CONTAINER})"
    sleep 1
  done
  for i in $(seq 1 30); do
    [[ "$(curl -s -o /dev/null -w '%{http_code}' "${MAIL_URL}/api/v1/info")" == "200" ]] && break
    [[ $i -eq 30 ]] && die "mailpit did not answer within 30s (docker logs ${MAIL_CONTAINER})"
    sleep 1
  done

  echo "verify: building Rehi.Apis"
  dotnet build "${REPO_ROOT}/Rehi.Apis/Rehi.Apis.csproj" -c Debug -nologo -v q > "${STATE_DIR}/build.log" 2>&1 \
    || { tail -30 "${STATE_DIR}/build.log"; die "build failed (full log: ${STATE_DIR}/build.log)"; }

  echo "verify: starting API on ${BASE_URL}"
  local jwt_secret; jwt_secret="$(openssl rand -base64 48)"
  (
    cd "${REPO_ROOT}/Rehi.Apis"
    ASPNETCORE_ENVIRONMENT=Development \
    ASPNETCORE_URLS="${BASE_URL}" \
    Jwt__Secret="${jwt_secret}" \
    Jwt__Issuer=rehi-verify \
    Jwt__Audience=rehi-verify-api \
    Rehi__WebBaseUrl=http://localhost:3000 \
    ConnectionStrings__Database="Host=127.0.0.1;Port=${PG_PORT};Database=rehi;Username=postgres;Password=${PG_PASSWORD}" \
    Serilog__WriteTo__1__Args__ServerUrl="http://127.0.0.1:1" \
    SendEmail__Host=127.0.0.1 \
    SendEmail__Port="${SMTP_PORT}" \
    SendEmail__Security=None \
    SendEmail__SmtpUser=verify@rehi.local \
    SendEmail__SmtpPass=verify \
    nohup dotnet "${DLL}" > "${STATE_DIR}/api.log" 2>&1 &
    echo $! > "${STATE_DIR}/api.pid"
  )

  # The API applies EF migrations before it starts listening, so a 200 here means the schema is in place.
  for i in $(seq 1 120); do
    if ! pid_is_ours "$(api_pid)"; then
      tail -40 "${STATE_DIR}/api.log"
      die "API exited during startup (log: ${STATE_DIR}/api.log)"
    fi
    [[ "$(curl -s -o /dev/null -w '%{http_code}' "${BASE_URL}/swagger/v1/swagger.json")" == "200" ]] && break
    [[ $i -eq 120 ]] && die "API not ready within 120s (log: ${STATE_DIR}/api.log)"
    sleep 1
  done

  echo "verify: ready"
  echo "  base url : ${BASE_URL}"
  echo "  api pid  : $(api_pid)"
  echo "  mailpit  : ${MAIL_URL}"
  echo "  evidence : ${EVIDENCE_ROOT}/${run_id}"
}

cmd_doctor() {
  local fail=0 pid; pid="$(api_pid)"
  check() { if eval "$2" >/dev/null 2>&1; then echo "OK   $1"; else echo "FAIL $1"; fail=1; fi; }

  check "API process ${pid:-<none>} is ours" "pid_is_ours '${pid}'"
  check "port ${API_PORT} is held by that process" "lsof -nP -a -p '${pid}' -iTCP:${API_PORT} -sTCP:LISTEN"
  check "swagger.json answers 200" "[[ \$(curl -s -o /dev/null -w '%{http_code}' ${BASE_URL}/swagger/v1/swagger.json) == 200 ]]"
  check "protected route /states answers 401 without a token" "[[ \$(curl -s -o /dev/null -w '%{http_code}' ${BASE_URL}/states) == 401 ]]"
  check "postgres ${PG_CONTAINER} is running" "[[ \$(docker inspect -f '{{.State.Running}}' ${PG_CONTAINER}) == true ]]"
  check "mailpit ${MAIL_CONTAINER} is running and ${MAIL_URL}/api/v1/info answers 200" "[[ \$(docker inspect -f '{{.State.Running}}' ${MAIL_CONTAINER}) == true && \$(curl -s -o /dev/null -w '%{http_code}' ${MAIL_URL}/api/v1/info) == 200 ]]"

  local expected applied
  expected="$(find "${REPO_ROOT}/Rehi.Infrastructure/Database/Migrations" -name '*.cs' ! -name '*.Designer.cs' ! -name '*ModelSnapshot.cs' | wc -l | tr -d ' ')"
  applied="$(psql_q 'select count(*) from "__EFMigrationsHistory"' 2>/dev/null || echo '?')"
  if [[ "${applied}" == "${expected}" ]]; then echo "OK   migrations applied: ${applied}/${expected}"
  else echo "FAIL migrations applied: ${applied}/${expected}"; fail=1; fi

  return "${fail}"
}

cmd_call() {
  [[ $# -ge 2 ]] || die "usage: verify.sh call <name> <curl args...>   (paths are relative to ${BASE_URL})"
  local name="$1"; shift
  local dir; dir="$(evidence_dir)"
  local args=() a
  for a in "$@"; do
    if [[ "$a" == /* ]]; then args+=("${BASE_URL}${a}"); else args+=("$a"); fi
  done
  if [[ -n "${REHI_VERIFY_TOKEN:-}" ]]; then args+=(-H "Authorization: Bearer ${REHI_VERIFY_TOKEN}"); fi

  printf '%q ' curl "$@" > "${dir}/${name}.cmd"; echo >> "${dir}/${name}.cmd"
  local status
  status="$(curl -sS -o "${dir}/${name}.body" -D "${dir}/${name}.headers" -w '%{http_code}' "${args[@]}")"
  echo "${status}" > "${dir}/${name}.status"
  echo "HTTP ${status}  (${dir}/${name}.body)"
  head -c 2000 "${dir}/${name}.body"; echo
}

cmd_sql() {
  [[ $# -eq 1 ]] || die 'usage: verify.sh sql "<read-only query>"'
  psql_q "$1"
}

cmd_mail_token() {
  [[ $# -eq 2 ]] || die "usage: verify.sh mail-token <to> <verify|reset>"
  local to="$1" path
  case "$2" in
    verify) path="verify-email" ;;
    reset) path="reset-password" ;;
    *) die "mail-token kind must be verify or reset, got '$2'" ;;
  esac
  local dir; dir="$(evidence_dir)"
  local id
  id="$(curl -sfG "${MAIL_URL}/api/v1/search" --data-urlencode "query=to:\"${to}\"" --data-urlencode "limit=1" | jq -r '.messages[0].ID // empty')" \
    || die "mailpit search failed at ${MAIL_URL}; is the instance up?"
  [[ -n "${id}" ]] || die "mailpit has no email to ${to}"
  local msg="${dir}/mail-${2}-${to}.json"
  curl -sf "${MAIL_URL}/api/v1/message/${id}" > "${msg}"
  local token
  token="$(jq -r '.Text, .HTML' "${msg}" | grep -oE "/${path}\?token=[A-Za-z0-9_=-]+" | head -1 | cut -d= -f2- || true)"
  [[ -n "${token}" ]] || die "newest email to ${to} has no /${path}?token= link (${msg})"
  echo "${token}"
}

cmd_mail_selftest() {
  local to verify reset
  to="selftest-$(date +%s)@rehi.local"
  verify="v$(openssl rand -hex 8)"
  reset="r$(openssl rand -hex 8)"
  printf 'From: verify@rehi.local\r\nTo: %s\r\nSubject: verify.sh mail-selftest\r\n\r\nhttp://localhost:3000/verify-email?token=%s\r\nhttp://localhost:3000/reset-password?token=%s\r\n' \
    "${to}" "${verify}" "${reset}" \
    | curl -sS "smtp://127.0.0.1:${SMTP_PORT}" --mail-from verify@rehi.local --mail-rcpt "${to}" --user verify:verify --upload-file -
  local got_verify got_reset
  got_verify="$(cmd_mail_token "${to}" verify)"
  got_reset="$(cmd_mail_token "${to}" reset)"
  if [[ "${got_verify}" == "${verify}" && "${got_reset}" == "${reset}" ]]; then
    echo "OK   mail-selftest: SMTP send to ${to} read back verify=${got_verify} reset=${got_reset}"
  else
    echo "FAIL mail-selftest: sent verify=${verify} reset=${reset}, read verify=${got_verify} reset=${got_reset}"
    return 1
  fi
}

cmd_down() {
  local pid; pid="$(api_pid)"
  if [[ -f "${STATE_DIR}/run-id" && -f "${STATE_DIR}/api.log" ]]; then
    cp "${STATE_DIR}/api.log" "${EVIDENCE_ROOT}/$(cat "${STATE_DIR}/run-id")/api.log" || true
  fi
  if pid_is_ours "${pid}"; then
    kill "${pid}"
    for _ in $(seq 1 20); do pid_is_ours "${pid}" || break; sleep 0.5; done
    pid_is_ours "${pid}" && kill -9 "${pid}"
    echo "verify: stopped API pid ${pid}"
  fi
  local c
  for c in "${PG_CONTAINER}" "${MAIL_CONTAINER}"; do
    if [[ "$(docker inspect -f '{{index .Config.Labels "rehi-verify"}}' "${c}" 2>/dev/null)" == "1" ]]; then
      docker rm -f "${c}" >/dev/null
      echo "verify: removed ${c}"
    fi
  done
  local run_id=""; [[ -f "${STATE_DIR}/run-id" ]] && run_id="$(cat "${STATE_DIR}/run-id")"
  rm -rf "${STATE_DIR}"
  [[ -n "${run_id}" ]] && echo "verify: evidence kept at ${EVIDENCE_ROOT}/${run_id}"
  return 0
}

case "${1:-}" in
  up) cmd_up ;;
  doctor) cmd_doctor ;;
  call) shift; cmd_call "$@" ;;
  sql) shift; cmd_sql "$@" ;;
  mail-token) shift; cmd_mail_token "$@" ;;
  mail-selftest) cmd_mail_selftest ;;
  evidence) evidence_dir ;;
  down) cmd_down ;;
  *) echo "usage: verify.sh up | doctor | call <name> <curl args...> | sql \"<query>\" | mail-token <to> <verify|reset> | mail-selftest | evidence | down" >&2; exit 2 ;;
esac
