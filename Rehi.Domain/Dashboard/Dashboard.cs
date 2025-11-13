namespace Rehi.Domain.Dashboard;

public class Dashboard
{
    public TotalUser TotalUser { get; set; } = null!;
    public NewUser NewUser { get; set; } = null!; // Added
    public TotalRevenue TotalRevenue { get; set; } = null!; // Added
    public RevenueChange RevenueChange { get; set; } = null!; // Added
    
    public List<NewSubscription> RecentSubscriptions { get; set; } = new(); // Added
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