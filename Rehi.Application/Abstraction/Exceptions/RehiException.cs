using Rehi.Domain.Common;

namespace Rehi.Application.Abstraction.Exceptions;

public sealed class RehiException : Exception
{
    public RehiException(string requestName, Error? error = default, Exception? innerException = default)
        : base("Application exception", innerException)
    {
        RequestName = requestName;
        Error = error;
    }

    public string RequestName { get; }

    public Error? Error { get; }
}