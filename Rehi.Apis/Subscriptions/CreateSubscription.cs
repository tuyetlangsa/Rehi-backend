using MediatR;
using Microsoft.AspNetCore.Mvc;
using Rehi.Apis.Endpoints;
using Rehi.Apis.Results;
using CreateSubscriptionCommand = Rehi.Application.Subscriptions.CreateSubscription.CreateSubscription;

public class CreateSubscription : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/subscriptions/create", async ([FromBody] Request request, ISender sender) =>
            {
                var result =
                    await sender.Send(
                        new CreateSubscriptionCommand.Command(request.SubscriptionId, request.Provider));
                return result.MatchCreated(id => $"/subscriptions/{id}");
            })
            .WithTags("Subscriptions")
            .RequireAuthorization()
            .WithName("CreateSubscription");
    }

    internal sealed class Request
    {
        public Guid SubscriptionId { get; set; }
        public string Provider { get; set; } = null!;
    }
}