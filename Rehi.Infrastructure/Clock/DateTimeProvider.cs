using Rehi.Application.Abstraction.Clock;

namespace Rehi.Infrastructure.Clock;

public class DateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}