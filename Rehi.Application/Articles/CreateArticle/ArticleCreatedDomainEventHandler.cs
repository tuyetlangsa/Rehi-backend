using Ganss.Xss;
using Rehi.Application.Abstraction.Data;
using Rehi.Application.Abstraction.Exceptions;
using Rehi.Domain.Articles;
using Rehi.Domain.Common;
using SmartReader;
using Article = SmartReader.Article;

namespace Rehi.Application.Articles.CreateArticle;

public class ArticleCreatedDomainEventHandler(IDbContext dbContext) : DomainEventHandler<ArticleCreatedDomainEvent>
{
    public override async Task Handle(ArticleCreatedDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        var articleEntity = await dbContext.Articles.FindAsync(domainEvent.ArticleId);
        if (articleEntity is null)
        {
            throw new RehiException(nameof(CreateArticle), ArticleErrors.NotFound);
        }
        
        Reader reader = new Reader(articleEntity.Url, articleEntity.RawHtml);
        Article article = await reader.GetArticleAsync();
        var sanitizer = new HtmlSanitizer();
        sanitizer.AllowedTags.Add("img");
        sanitizer.AllowedAttributes.Add("src");
        sanitizer.UriAttributes.Add("src");
        sanitizer.AllowedSchemes.Add("data");
        if (!article.IsReadable)
        {
            articleEntity.Title = "Unable to extract content";
            articleEntity.Content = "Unable to extract content";
        }
        await article.ConvertImagesToDataUriAsync();

        var santinized = sanitizer.Sanitize(article.Content);
        
        
        articleEntity.Title = article.Title;
        articleEntity.Author = article.Author;
        articleEntity.Summary = article.Excerpt;
        articleEntity.WordCount = article.Length;
        articleEntity.TimeToRead = article.TimeToRead;
        articleEntity.ImageUrl = article.FeaturedImage;
        articleEntity.Content = santinized;
        articleEntity.TextContent = article.TextContent;
        articleEntity.Language = article.Language;
        // articleEntity.PublishDate = article.PublicationDate;
        
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}