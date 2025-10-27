public class PayPalSubscriptionCreateRequest
{
    public string plan_id { get; set; }
    public PayPalApplicationContext application_context { get; set; }
}

public class PayPalApplicationContext
{
    public string return_url { get; set; }
    public string cancel_url { get; set; }
}

public class PayPalSubscriptionResponse
{
    public string id { get; set; }
    public string status { get; set; }
    public List<PayPalLink> links { get; set; }
}

public class PayPalLink
{
    public string href { get; set; }
    public string rel { get; set; }
    public string method { get; set; }
}

public class PayPalTokenResponse
{
    public string access_token { get; set; }
    public string token_type { get; set; }
    public int expires_in { get; set; }
}

public class PayPalSubscriptionDetails
{
    public string id { get; set; }
    public string status { get; set; }
    public PayPalBillingInfo billing_info { get; set; }
    public PayPalSubscriber subscriber { get; set; }
    public DateTime start_time { get; set; }
}

public class PayPalBillingInfo
{
    public DateTime? next_billing_time { get; set; }
}

public class PayPalSubscriber
{
    public string email_address { get; set; }
}