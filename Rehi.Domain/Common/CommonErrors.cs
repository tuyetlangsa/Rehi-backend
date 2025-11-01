namespace Rehi.Domain.Common;

public static class CommonErrors 
{
    public static Error StaleRequest => new("StaleRequest", "The request is stale", ErrorType.Conflict);

}