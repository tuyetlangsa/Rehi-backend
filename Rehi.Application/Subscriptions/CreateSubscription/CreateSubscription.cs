using Microsoft.EntityFrameworkCore;
using Rehi.Application.Abstraction.Authentication;
using Rehi.Application.Abstraction.Data;
using Rehi.Application.Abstraction.Messaging;
using Rehi.Application.Abstraction.Payments;
using Rehi.Application.States;
using Rehi.Domain.Common;
using Rehi.Domain.Subscription;
using Rehi.Domain.Users;

namespace Rehi.Application.Subscriptions.CreateSubscription;

public abstract class CreateSubscription
{
    public record Command(Guid SubscriptionPlanId,string PayPalPlanId, string Provider) : ICommand<Response>;

    public record Response(
        string ApprovalUrl,
        string SubscriptionId,
        string Provider
    );

    internal class Handler(IDbContext dbContext, IUserContext userContext, IPaymentFactory paymentFactory) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            var userEmail = userContext.Email;
            var user = await dbContext.Users.SingleOrDefaultAsync(u => u.Email ==userEmail, cancellationToken);
            if (user is null)
            {
                return Result.Failure<Response>(UserErrors.NotFound);
            }
            var paymentService = paymentFactory.Create(request.Provider);
            var result = await paymentService.CreateSubscriptionAsync(request.PayPalPlanId);

            var subsription = new UserSubscription()
            {
                UserId = user.Id,
                PaymentProvider = request.Provider,
                SubscriptionPlanId = request.SubscriptionPlanId,
                ExternalSubscriptionId = result.SubscriptionId,
                Status = SubscriptionStatus.Pending
            };
            
            dbContext.UserSubscriptions.Add(subsription);
            await dbContext.SaveChangesAsync(cancellationToken);

            return new Response(result.ApprovalUrl, result.SubscriptionId, request.Provider);
        }
    }
}