namespace Rehi.Application.Abstraction.QueryExtension;

public interface ISortable
{
    public string? SortColumn { get; }
    public SortOrder SortOrder { get; }
}