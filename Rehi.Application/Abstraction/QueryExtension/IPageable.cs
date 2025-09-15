namespace Rehi.Application.Abstraction.QueryExtension;

public interface IPageable
{
    public int PageNumber { get; }
    public int PageSize { get; }
}