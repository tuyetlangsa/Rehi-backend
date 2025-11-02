using Rehi.Domain.Common;

namespace Rehi.Domain.Articles;

public static class ArticleErrors
{
    public static Error NotFound => new("Article.NotFound", "Article not found", ErrorType.NotFound);
}