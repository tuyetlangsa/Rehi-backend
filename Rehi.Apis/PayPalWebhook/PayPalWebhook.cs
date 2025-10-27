using MediatR;
using Microsoft.AspNetCore.Mvc;
using Rehi.Apis.Endpoints;
using Rehi.Apis.Results;
using Rehi.Application.Subscriptions.PayPalWebhook;
using Rehi.Domain.Common;

namespace Rehi.Apis.PayPalWebhook;

public class PayPalWebhook : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/paypal/webhook", async ([FromBody] Request request, ISender sender) =>
            {
                // var result = await sender.Send(new ReceivePayPalWebhook.Command(request.Payload, request.TransmissionId,
                //     request.TransmissionTime, request.TransmissionSig, request.CertUrl));
                // return result.MatchOk();
                
                return Result.Success().MatchOk();
            })
            .WithTags("PayPal Webhook")
            .AllowAnonymous()
            .WithName("ReceivePayPalWebhook");
    }

    internal sealed class Request
    {
        public string Payload { get; set; }
        public string TransmissionId { get; set; }
        public string TransmissionTime { get; set; }
        public string TransmissionSig { get; set; }
        public string CertUrl { get; set; }
    }
}