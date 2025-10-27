using MediatR;
using Microsoft.AspNetCore.Mvc;
using Rehi.Apis.Endpoints;
using Rehi.Apis.Results;

public class CreateSubscription : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/subscriptions/created", async ([FromBody] Request request, ISender sender) =>
            {
                var result = await sender.Send(new Rehi.Application.Subscriptions.CreateSubscription.CreateSubscription.Command(request.SubscriptionPlanId, request.PayPalPlanId, request.Provider));
                return result.MatchCreated(id => $"/subscriptions/{id}");
            })
            .WithTags("Subscriptions")
            .RequireAuthorization()
            .WithName("CreateSubscription");
    }
    internal sealed class Request
    {
        public Guid SubscriptionPlanId { get; set; }
        public string PayPalPlanId { get; set; } = null!;
        public string Provider { get; set; } = null!;
    }
}