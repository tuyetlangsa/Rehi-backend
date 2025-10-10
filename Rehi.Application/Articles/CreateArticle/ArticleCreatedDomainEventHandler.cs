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

    private void AddHeadingTagId(AngleSharp.Dom.IElement element)
    {
        var headings = element.QuerySelectorAll("h1, h2, h3, h4, h5, h6");

        for (int i = 0; i < headings.Length; i++)
        {
            var heading = headings[i];
            heading.Id = i.ToString(); 
        }
    }
    public override async Task Handle(ArticleCreatedDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        var articleEntity = await dbContext.Articles.FindAsync(domainEvent.ArticleId);
        if (articleEntity is null)
        {
            throw new RehiException(nameof(CreateArticle), ArticleErrors.NotFound);
        }
        Reader reader = new Reader(articleEntity.Url, articleEntity.RawHtml);
        reader.AddCustomOperationEnd(AddHeadingTagId);
        Article article = await reader.GetArticleAsync();
        var sanitizer = new HtmlSanitizer();
        sanitizer.AllowedTags.Add("img");
        sanitizer.AllowedAttributes.Add("src");
        sanitizer.UriAttributes.Add("src");
        sanitizer.AllowedSchemes.Add("data");
        sanitizer.AllowedTags.Add("picture");
        sanitizer.AllowedTags.Add("source");
        sanitizer.AllowedAttributes.Add("srcset");
        sanitizer.AllowedAttributes.Add("media");
        sanitizer.AllowedAttributes.Add("type");
        sanitizer.AllowedAttributes.Add("id");
        if (!article.IsReadable)
        {
            articleEntity.Title = "Unable to extract content";
            articleEntity.Content = "Unable to extract content";
        }
        await article.ConvertImagesToDataUriAsync(10000);
        var sanitized = sanitizer.Sanitize(article.Content);
        articleEntity.Title = article.Title;
        articleEntity.Author = article.Author;
        articleEntity.Summary = article.Excerpt;
        articleEntity.WordCount = article.Length;
        articleEntity.TimeToRead = article.TimeToRead;
        articleEntity.ImageUrl = article.FeaturedImage;
        articleEntity.Content = sanitized;
        articleEntity.TextContent = article.TextContent;
        articleEntity.Language = article.Language;
        articleEntity.PublishDate = article.PublicationDate?.ToUniversalTime(); 
        articleEntity.UpdateAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}