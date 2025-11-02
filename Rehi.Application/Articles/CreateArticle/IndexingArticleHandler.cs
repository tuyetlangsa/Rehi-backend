using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Rehi.Application.Abstraction.Data;
using Rehi.Application.Abstraction.Exceptions;
using Rehi.Domain.Common;

namespace Rehi.Application.Articles.CreateArticle;

public class IndexingArticleHandler(
    IDbContext dbContext, 
    ArticleIndexingService  articleIndexingService,
    IConfiguration configuration): DomainEventHandler<ArticleParsedDomainEvent>

{
    private const int MaxChunkSize = 8000;
    public override async Task Handle(ArticleParsedDomainEvent domainEvent,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var article = await dbContext.Articles.FindAsync(
                new object?[] { domainEvent.ArticleId }, 
                cancellationToken);
            
            if (article is null)
            {
                throw new RehiException($"Article not found: {domainEvent.ArticleId}");
            }

            if (string.IsNullOrWhiteSpace(article.Content))
            {
                return; 
            }

            if (article.Content.Length <= MaxChunkSize)
            {
                var result = await articleIndexingService.IndexArticleAsync(
                    article.Id, 
                    article.TextContent);
                
                if (!result)
                {
                    throw new RehiException($"Failed to index article: {article.Id}");
                }
            }
            else
            {
                var chunks = ChunkText(article.TextContent, MaxChunkSize);
                var articlesToIndex = chunks.Select((chunk, index) => 
                    (articleId: article.Id, content: chunk));

                var result = await articleIndexingService.IndexArticlesBatchAsync(articlesToIndex);
                
                if (!result)
                {
                    throw new RehiException($"Failed to index article chunks: {article.Id}");
                }
            }
        }
        catch (RehiException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new RehiException("Error indexing article");
        }
    }
    
    
    private static IEnumerable<string> ChunkText(string text, int maxChunkSize)
    {
        text = Regex.Replace(text, @"\s+", " ").Trim();
        
        if (text.Length <= maxChunkSize)
        {
            yield return text;
            yield break;
        }

        var paragraphs = Regex.Split(text, @"\n\n+");
        var currentChunk = new List<string>();
        var currentLength = 0;

        foreach (var paragraph in paragraphs)
        {
            if (currentLength + paragraph.Length > maxChunkSize && currentChunk.Any())
            {
                yield return string.Join("\n\n", currentChunk);
                currentChunk.Clear();
                currentLength = 0;
            }

            if (paragraph.Length > maxChunkSize)
            {
                var sentences = Regex.Split(paragraph, @"(?<=[.!?])\s+");
                foreach (var sentence in sentences)
                {
                    if (currentLength + sentence.Length > maxChunkSize && currentChunk.Any())
                    {
                        yield return string.Join(" ", currentChunk);
                        currentChunk.Clear();
                        currentLength = 0;
                    }
                    currentChunk.Add(sentence);
                    currentLength += sentence.Length;
                }
            }
            else
            {
                currentChunk.Add(paragraph);
                currentLength += paragraph.Length;
            }
        }

        if (currentChunk.Any())
        {
            yield return string.Join("\n\n", currentChunk);
        }
    }
    
}