using Rehi.Domain.Common;

namespace Rehi.Domain.Highlights;

public static class HighlightErrors
{
    public static Error NotFound => new("Highlight.NotFound", "Highlight not found", ErrorType.NotFound);
}