using MediatR;
using Rehi.Apis.Endpoints;
using Rehi.Apis.Results;

namespace Rehi.Apis.Subscriptions;

public class GetSubscriptionPlanByUserEmail : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/subscriptions/validate", async (ISender sender) =>
            {
                var result =
                    await sender.Send(
                        new Application.Subscriptions.GetSubscriptionPlanByUserEmail.GetSubscriptionPlanByUserEmail.
                            Command());
                return result.MatchOk();
            })
            .WithTags("Subscriptions")
            .RequireAuthorization()
            .WithName("GetSubscriptionPlanByUserEmail");
    }
}