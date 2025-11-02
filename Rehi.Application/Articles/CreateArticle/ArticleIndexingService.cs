using Azure;
using Azure.AI.OpenAI;
using Azure.Search.Documents;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenAI.Embeddings;

namespace Rehi.Application.Articles.CreateArticle;

public class ArticleIndexingService
{
    private readonly SearchClient _searchClient;
    private readonly EmbeddingClient _embeddingClient;
    private readonly ILogger<ArticleIndexingService> _logger;

    public ArticleIndexingService(
        IConfiguration configuration,
        ILogger<ArticleIndexingService> logger)
    {
        _logger = logger;

        // Initialize Azure Search Client
        var searchEndpoint = new Uri(configuration["AzureSearch:ServiceEndpoint"]);
        var searchCredential = new AzureKeyCredential(configuration["AzureSearch:ApiKey"]);
        var indexName = configuration["AzureSearch:IndexName"];
        _searchClient = new SearchClient(searchEndpoint, indexName, searchCredential);

        // Initialize Azure OpenAI Embedding Client
        var openAIEndpoint = new Uri(configuration["AzureOpenAI:Endpoint"]);
        var openAICredential = new AzureKeyCredential(configuration["AzureOpenAI:ApiKey"]);
        var deploymentName = configuration["AzureOpenAI:EmbeddingDeploymentName"];

        var azureOpenAIClient = new AzureOpenAIClient(openAIEndpoint, openAICredential);
        _embeddingClient = azureOpenAIClient.GetEmbeddingClient(deploymentName);
    }

    public async Task<ReadOnlyMemory<float>> GenerateEmbeddingAsync(string text)
    {
        try
        {
            var embeddingResult = await _embeddingClient.GenerateEmbeddingAsync(text);
            return embeddingResult.Value.ToFloats();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating embedding for text");
            throw;
        }
    }

    public async Task<bool> IndexArticleAsync(Guid articleId, string content)
    {
        try
        {
            var embedding = await GenerateEmbeddingAsync(content);

            var document = new ArticleDocument
            {
                Id = Guid.NewGuid().ToString(),
                ArticleId = articleId.ToString(),
                Content = content,
                Embedding = embedding.ToArray()
            };

            var response = await _searchClient.UploadDocumentsAsync(new[] { document });

            return response.Value.Results.All(r => r.Succeeded);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error indexing article {ArticleId}", articleId);
            return false;
        }
    }

    public async Task<bool> IndexArticlesBatchAsync(IEnumerable<(Guid articleId, string content)> articles)
    {
        try
        {
            var documents = new List<ArticleDocument>();

            foreach (var (articleId, content) in articles)
            {
                var embedding = await GenerateEmbeddingAsync(content);

                documents.Add(new ArticleDocument
                {
                    Id = Guid.NewGuid().ToString(),
                    ArticleId = articleId.ToString(),
                    Content = content,
                    Embedding = embedding.ToArray()
                });
            }

            var response = await _searchClient.UploadDocumentsAsync(documents);

            return response.Value.Results.All(r => r.Succeeded);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error indexing articles batch");
            return false;
        }
    }
}