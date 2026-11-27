using Microsoft.EntityFrameworkCore;
using Rehi.Application.Abstraction.Authentication;
using Rehi.Application.Abstraction.Data;
using Rehi.Domain.Users;

namespace Rehi.Application.Auth;

public sealed record TokenPair(
    string AccessToken,
    long AccessTokenExpiresAt,
    string RefreshToken,
    long RefreshTokenExpiresAt);

internal enum RefreshTokenState
{
    Active,
    RotatedWithinGrace,
    Reused,
    Revoked,
    Expired
}

internal static class Sessions
{
    private static readonly TimeSpan ReuseGrace = TimeSpan.FromSeconds(30);

    public static RefreshTokenState StateOf(RefreshToken token, DateTime now) => token switch
    {
        _ when token.ExpiresAt <= now => RefreshTokenState.Expired,
        { RevokedAt: null } => RefreshTokenState.Active,
        { ReplacedById: null } => RefreshTokenState.Revoked,
        _ when now - token.RevokedAt <= ReuseGrace => RefreshTokenState.RotatedWithinGrace,
        _ => RefreshTokenState.Reused
    };

    public static (RefreshToken Row, TokenPair Pair) Issue(
        IDbContext dbContext,
        ITokenIssuer tokenIssuer,
        User user,
        RefreshTokenClient client,
        Guid familyId,
        DateTime now)
    {
        var refreshToken = OpaqueToken.Create();
        var row = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = refreshToken.Hash,
            Client = client,
            FamilyId = familyId,
            CreatedAt = now,
            ExpiresAt = now + Lifetime(client)
        };
        dbContext.RefreshTokens.Add(row);

        var accessToken = tokenIssuer.IssueAccessToken(user);
        return (row, new TokenPair(
            accessToken.Value,
            EpochMilliseconds(accessToken.ExpiresAt),
            refreshToken.Value,
            EpochMilliseconds(row.ExpiresAt)));
    }

    public static Task<bool> IsFamilyRevoked(IDbContext dbContext, Guid familyId, CancellationToken cancellationToken) =>
        dbContext.RefreshTokens.AnyAsync(
            t => t.FamilyId == familyId && t.RevokedAt != null && t.ReplacedById == null,
            cancellationToken);

    public static async Task RevokeFamily(IDbContext dbContext, Guid familyId, DateTime now,
        CancellationToken cancellationToken)
    {
        var active = await dbContext.RefreshTokens
            .Where(t => t.FamilyId == familyId && t.RevokedAt == null)
            .ToListAsync(cancellationToken);
        foreach (var token in active) token.RevokedAt = now;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static TimeSpan Lifetime(RefreshTokenClient client) => client switch
    {
        RefreshTokenClient.Web => TimeSpan.FromDays(30),
        RefreshTokenClient.Extension => TimeSpan.FromDays(90),
        _ => throw new ArgumentOutOfRangeException(nameof(client), client, null)
    };

    private static long EpochMilliseconds(DateTime utc) => new DateTimeOffset(utc, TimeSpan.Zero).ToUnixTimeMilliseconds();
}
