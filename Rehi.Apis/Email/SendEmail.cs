using MediatR;
using Rehi.Apis.Endpoints;
using Rehi.Apis.Results;

namespace Rehi.Apis.Email;

public class SendEmail : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/email/schedule", async (Request request, ISender sender) =>
            {
                var result =
                    await sender.Send(
                        new Application.Email.SendEmail.SendEmail.Command(request.UserEmail, request.ScheduleTime));
                return result.MatchOk();
            })
            .WithTags("Email")
            .RequireAuthorization()
            .WithName("SendEmail");
    }

    internal sealed class Request
    {
        public string? UserEmail { get; set; }
        public DateTime ScheduleTime { get; set; }
    }
}