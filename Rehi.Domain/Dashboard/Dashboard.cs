using Rehi.Domain.Subscription;

namespace Rehi.Domain.Dashboard;

public class Dashboard
{
    public TotalUser TotalUser { get; set; } = null!;
    public NewUser NewUser { get; set; } = null!; // Added
    public TotalRevenue TotalRevenue { get; set; } = null!; // Added
    public RevenueChange RevenueChange { get; set; } = null!; // Added
    
    public List<NewSubscription> RecentSubscriptions { get; set; } = new(); // Added

    public List<RevenueChartData> RevenueChartDatas { get; set; } = new();
    
    public List<PlanDistributionChartData> PlanDistributionChartDatas { get; set; } = new();
    
    public List<BillingDistributionChartData> BillingDistributionChartDatas { get; set; } = new();
}

public class TotalUser
{
    public int Value { get; set; }
}

public class NewUser
{
    public int Value { get; set; }
    public int MonthOverMonthChange { get; set; } 
}

public class TotalRevenue
{
    public decimal Value { get; set; } 
    public decimal MonthOverMonthChange { get; set; }
}

public class RevenueChange
{
    public decimal Value { get; set; } 
}

public class NewSubscription
{
    public string Email { get; set; } = string.Empty;
    public decimal Value { get; set; } 
}

public class RevenueChartData
{
    public int Month { get; set; }
    public decimal Value { get; set; }
}

public class PlanDistributionChartData
{
    public string Name { get; set; } = string.Empty;
     public int Value { get; set; }
     public string Color { get; set; }   = string.Empty; 
 }

public class BillingDistributionChartData
{
    public string Name { get; set; } = null!;
    public int Value { get; set; }
    public string Color { get; set; }   = string.Empty; 
}