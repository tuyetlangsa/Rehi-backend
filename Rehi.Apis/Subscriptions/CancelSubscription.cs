using MediatR;
using Microsoft.AspNetCore.Mvc;
using Rehi.Apis.Endpoints;
using Rehi.Apis.Results;

namespace Rehi.Apis.Subscriptions;

public class CancelSubscription : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPatch("/subscriptions/cancel",
                async ([FromBody] Request request, ISender sender) =>
                {
                    var result =
                        await sender.Send(
                            new Application.Subscriptions.CancelSubscription.CancelSubscription.Command(
                                request.Provider));
                    return result.MatchOk();
                })
            .WithTags("Subscriptions")
            .RequireAuthorization()
            .WithName("CancelSubscription");
    }

    internal sealed class Request
    {
        public string Provider { get; set; } = null!;
    }
}