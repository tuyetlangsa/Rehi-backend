using Rehi.Domain.Common;

namespace Rehi.Application.Highlights.CreateHighlight;

public class HighlightCreatedDomainEvent(Guid highlightId) : DomainEvent
{
    public Guid HighlightId { get; } = highlightId;
}