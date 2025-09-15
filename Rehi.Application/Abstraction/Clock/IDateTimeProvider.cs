namespace Rehi.Application.Abstraction.Clock;

public interface IDateTimeProvider
{
    public DateTime UtcNow { get; }
}