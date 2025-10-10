using Rehi.Domain.Common;

namespace Rehi.Domain.Tags;

public static class TagErrors 
{
    public static Error NotFound => new("Tag.NotFound", "Tag is not found", ErrorType.NotFound);
    public static Error AlreadyExisted => new("Tag.AlreadyExisted", "Tag is already existed",ErrorType.Conflict);
}