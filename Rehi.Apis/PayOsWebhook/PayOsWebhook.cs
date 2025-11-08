// using MediatR;
// using PayOS.Models.Webhooks;
// using Rehi.Apis.Endpoints;
// using Rehi.Apis.Results;
// using Rehi.Application.Subscriptions.PayPalWebhook;
//
// namespace Rehi.Apis.PayOsWebhook;
//
// public class PayOsWebhook : IEndpoint
// {
//     public void MapEndpoint(IEndpointRouteBuilder app)
//     {
//         app.MapPost("/paypal/webhook", async (Webhook webhook, ISender sender) =>
//             {
//                 var result = await sender.Send();
//                 return result.MatchOk();
//             })
//             .WithTags("PayPal Webhook")
//             .AllowAnonymous()
//             .WithName("ReceivePayPalWebhook");
//     }
// }