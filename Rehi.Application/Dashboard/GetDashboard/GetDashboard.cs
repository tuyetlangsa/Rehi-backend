using Microsoft.EntityFrameworkCore;
using Rehi.Application.Abstraction.Authentication;
using Rehi.Application.Abstraction.Data;
using Rehi.Application.Abstraction.Messaging;
using Rehi.Domain.Common;
using Rehi.Domain.Dashboard;
using Rehi.Domain.Subscription;
using Rehi.Domain.Users;

namespace Rehi.Application.Dashboard.GetDashboard;

public abstract class GetDashboard
{
    public record Command() : ICommand<Response>;

    public record Response(Domain.Dashboard.Dashboard Dashboard);

    internal class Handler : ICommandHandler<Command, Response>
    {
        private readonly IDbContext _dbContext;
        private readonly IUserContext _userContext;

        public Handler(IDbContext dbContext, IUserContext userContext)
        {
            _dbContext = dbContext;
            _userContext = userContext;
        }

        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            var email = _userContext.Email;
            var now = DateTime.UtcNow;
            var thisMonth = now.Month;
            var thisYear = now.Year;

            // Verify user exists
            var userExists = await _dbContext.Users
                .AnyAsync(u => u.Email == email, cancellationToken);

            if (!userExists)
                return Result.Failure<Response>(UserErrors.NotFound);

            // Define date ranges for queries
            var currentMonthStart = new DateTime(thisYear, thisMonth, 1, 0, 0, 0, DateTimeKind.Utc);
            var previousMonthStart = currentMonthStart.AddMonths(-1);
            var currentMonthEnd = currentMonthStart.AddMonths(1);

            // Total users count
            var totalUsers = await _dbContext.Users.CountAsync(cancellationToken);

            // New users this month
            var newUsers = await _dbContext.Users
                .CountAsync(u => u.CreateAt >= currentMonthStart && u.CreateAt < currentMonthEnd,
                    cancellationToken);

            // New users previous month
            var previousMonthNewUsers = await _dbContext.Users
                .CountAsync(u => u.CreateAt >= previousMonthStart && u.CreateAt < currentMonthStart,
                    cancellationToken);

            // Calculate new user month-over-month change
            var newUserMonthOverMonthChange = newUsers - previousMonthNewUsers;

            // Current month revenue from active subscriptions
            var currentMonthRevenue = await _dbContext.UserSubscriptions
                .Where(us => us.StartDate >= currentMonthStart
                             && us.StartDate < currentMonthEnd
                             && us.Status != SubscriptionStatus.Pending
                             && us.CurrentPeriodEnd > now)
                .Include(us => us.SubscriptionPlan)
                .SumAsync(s => s.SubscriptionPlan.Price, cancellationToken);

            // Previous month revenue from active subscriptions
            var previousMonthRevenue = await _dbContext.UserSubscriptions
                .Where(us => us.StartDate >= previousMonthStart
                             && us.StartDate < currentMonthStart
                             && us.Status != SubscriptionStatus.Pending
                             && us.CurrentPeriodEnd > now)
                .Include(us => us.SubscriptionPlan)
                .SumAsync(s => s.SubscriptionPlan.Price, cancellationToken);

            // Calculate revenue month-over-month change
            var revenueMonthOverMonthChange = currentMonthRevenue - previousMonthRevenue;

            // Calculate revenue change percentage
            var revenueChangePercentage = previousMonthRevenue > 0
                ? (currentMonthRevenue - previousMonthRevenue)
                : currentMonthRevenue;

            // Get recent subscriptions from this month
            var newSubscriptions = await _dbContext.UserSubscriptions
                .Where(us => us.StartDate >= currentMonthStart
                             && us.StartDate < currentMonthEnd
                             && us.Status != SubscriptionStatus.Pending
                             && us.CurrentPeriodEnd > now)
                .Include(us => us.User)
                .Include(us => us.SubscriptionPlan)
                .OrderByDescending(us => us.StartDate)
                .Select(us => new NewSubscription
                {
                    Email = us.User.Email,
                    Value = us.SubscriptionPlan.Price
                })
                .ToListAsync(cancellationToken);

            // Construct the dashboard response
            
            var year = DateTime.UtcNow.Year;

            var monthlyRevenue = await _dbContext.UserSubscriptions
                .Where(us => us.StartDate.Year == year
                             && us.Status != SubscriptionStatus.Pending
                             && us.CurrentPeriodEnd > DateTime.UtcNow)
                .GroupBy(us => us.StartDate.Month)
                .Select(g => new RevenueChartData
                {
                    Month = g.Key, 
                    Value = g.Sum(x => x.SubscriptionPlan.Price) // sum by plan price
                })
                .ToListAsync(cancellationToken);
            
            var total = await _dbContext.UserSubscriptions
                .Where(us => us.Status != SubscriptionStatus.Pending)
                .CountAsync(cancellationToken);

// dictionary for mapping plan → display name + color
            var planMap = new Dictionary<Guid, (string Name, string Color)>
            {
                { Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa5"), ("% Premium", "#FCD34D") },
                { Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa3"), ("% Premium Individual", "#FB923C") },
                { Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa4"), ("% Group Plan", "#60A5FA") },
                { Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa2"), ("% Business Plan", "#22D3EE") },
            };

            var planDistributionData = await _dbContext.UserSubscriptions
                .Where(us => us.Status != SubscriptionStatus.Pending)
                .GroupBy(us => us.SubscriptionPlanId)
                .Select(g => new
                {
                    PlanId = g.Key,
                    Count = g.Count()
                })
                .ToListAsync(cancellationToken);

            var result = planDistributionData
                .Where(p => planMap.ContainsKey(p.PlanId))
                .Select(p => new PlanDistributionChartData
                {
                    Name = planMap[p.PlanId].Name,
                    Value = total == 0 ? 0 : (int)(p.Count * 100.0 / total),
                    Color = planMap[p.PlanId].Color
                })
                .ToList();
            
            var subscriptions = await _dbContext.UserSubscriptions
                .Where(us => us.Status != SubscriptionStatus.Pending)
                .Include(us => us.SubscriptionPlan)
                .ToListAsync(cancellationToken);

            var totalBilling = subscriptions.Count;

            var monthlyCount = subscriptions.Count(s => s.SubscriptionPlan.TypeOfSubscription == "month");
            var yearlyCount = subscriptions.Count(s => s.SubscriptionPlan.TypeOfSubscription == "year");

            int ToPercent(int count) => total == 0 ? 0 : (int)(count * 100.0 / total);

            var billingData = new List<BillingDistributionChartData>
            {
                new BillingDistributionChartData
                {
                    Name = "% Monthly",
                    Value = ToPercent(monthlyCount),
                    Color = "#E5E7EB"
                },
                new BillingDistributionChartData
                {
                    Name = "% Yearly",
                    Value = ToPercent(yearlyCount),
                    Color = "#14B8A6"
                }
            };
            
            var dashboard = new Domain.Dashboard.Dashboard
            {
                TotalUser = new TotalUser { Value = totalUsers },
                NewUser = new NewUser
                {
                    Value = newUsers,
                    MonthOverMonthChange = newUserMonthOverMonthChange
                },
                TotalRevenue = new TotalRevenue
                {
                    Value = currentMonthRevenue,
                    MonthOverMonthChange = revenueMonthOverMonthChange
                },
                RevenueChange = new RevenueChange { Value = revenueChangePercentage },
                RecentSubscriptions = newSubscriptions,
                RevenueChartDatas = monthlyRevenue,
                PlanDistributionChartDatas = result,
                BillingDistributionChartDatas = billingData
            };

            return Result.Success(new Response(dashboard));
        }
    }
}