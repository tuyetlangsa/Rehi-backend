using System.Text;
using Azure;
using Azure.AI.OpenAI;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using Microsoft.Extensions.Configuration;
using OpenAI.Chat;
using OpenAI.Embeddings;
using Rehi.Application.Articles.CreateArticle;

namespace Rehi.Application.Chat;

public class RagChatService
{
    private readonly SearchClient _searchClient;
    private readonly EmbeddingClient _embeddingClient;
    private readonly ChatClient _chatClient;
    private readonly int _topK;

    public RagChatService(
        IConfiguration configuration)
    {
        _topK = configuration.GetValue<int>("RAG:TopK", 5);

        var searchEndpoint = configuration["AzureSearch:ServiceEndpoint"] 
            ?? throw new InvalidOperationException("AzureSearch:ServiceEndpoint is not configured");
        var searchApiKey = configuration["AzureSearch:ApiKey"] 
            ?? throw new InvalidOperationException("AzureSearch:ApiKey is not configured");
        var indexName = configuration["AzureSearch:IndexName"] 
            ?? throw new InvalidOperationException("AzureSearch:IndexName is not configured");
        var openAIEndpoint = configuration["AzureOpenAI:Endpoint"] 
            ?? throw new InvalidOperationException("AzureOpenAI:Endpoint is not configured");
        var openAIApiKey = configuration["AzureOpenAI:ApiKey"] 
            ?? throw new InvalidOperationException("AzureOpenAI:ApiKey is not configured");
        var chatApiKey = configuration["AzureOpenAI:ChatApiKey"] 
            ?? throw new InvalidOperationException("AzureOpenAI:ChatApiKey is not configured");
        var chatEndpoint = configuration["AzureOpenAI:ChatEndpoint"] 
            ?? throw new InvalidOperationException("AzureOpenAI:ChatEndpoint is not configured");
        var embeddingDeploymentName = configuration["AzureOpenAI:EmbeddingDeploymentName"] 
            ?? throw new InvalidOperationException("AzureOpenAI:EmbeddingDeploymentName is not configured");
        var chatDeploymentName = configuration["AzureOpenAI:ChatDeploymentName"] 
            ?? throw new InvalidOperationException("AzureOpenAI:ChatDeploymentName is not configured");

        var searchUri = new Uri(searchEndpoint);
        var searchCredential = new AzureKeyCredential(searchApiKey);
        _searchClient = new SearchClient(searchUri, indexName, searchCredential);

        var openAIUri = new Uri(openAIEndpoint);
        var openAICredential = new AzureKeyCredential(openAIApiKey);
        var azureOpenAIClient = new AzureOpenAIClient(openAIUri, openAICredential);
        var chatCredential = new AzureKeyCredential(chatApiKey);
        var chatUri = new Uri(chatEndpoint);
        var chatClient = new AzureOpenAIClient(chatUri, chatCredential);
        _embeddingClient = azureOpenAIClient.GetEmbeddingClient(embeddingDeploymentName);
        _chatClient = chatClient.GetChatClient(chatDeploymentName);

    }

    public async Task<ChatResponse> ChatAsync(
        string userQuestion,
        List<ChatMessage>? conversationHistory = null,
        CancellationToken cancellationToken = default)
    {
        try
        {

            var questionEmbedding = await GenerateEmbeddingAsync(userQuestion);

            var relevantDocs = await VectorSearchAsync(questionEmbedding, _topK);

            // if (!relevantDocs.Any())
            // {
            //     return new ChatResponse
            //     {
            //         Answer = "I couldn't find any relevant information to answer your question.",
            //         Sources = new List<string>(),
            //     };
            // }

            var context = BuildContext(relevantDocs);

            var messages = BuildChatMessages(userQuestion, context, conversationHistory);


            var chatResponse = await _chatClient.CompleteChatAsync(messages, cancellationToken: cancellationToken);
            var completion = chatResponse.Value;

            var answer = completion.Content[0].Text;


            return new ChatResponse
            {
                Answer = answer,
                Sources = relevantDocs.Select(d => d.ArticleId).Distinct().ToList(),
                RelevantDocuments = relevantDocs
            };
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    private async Task<ReadOnlyMemory<float>> GenerateEmbeddingAsync(string text)
    {
        try
        {
            var embeddingResult = await _embeddingClient.GenerateEmbeddingAsync(text);
            return embeddingResult.Value.ToFloats();
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    private async Task<List<ArticleDocument>> VectorSearchAsync(
        ReadOnlyMemory<float> queryEmbedding,
        int topK)
    {
        try
        {
            SearchResults<ArticleDocument> response = await _searchClient.SearchAsync<ArticleDocument>(
                new SearchOptions
                {
                    VectorSearch = new()
                    {
                        Queries = { new VectorizedQuery(queryEmbedding) { KNearestNeighborsCount = topK, Fields = { "Embedding" }, Exhaustive = true } }
                    },
                    Select = { "Id", "ArticleId", "Content" },
                });


            var results = new List<ArticleDocument>();

            
            await foreach (SearchResult<ArticleDocument> result in response.GetResultsAsync())
            {
                const double minimumScore = 0.3;
                if (result.Score.HasValue && result.Score.Value >= minimumScore)
                {
                    results.Add(result.Document);
                }
            }

            return results;
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    private string BuildContext(List<ArticleDocument> documents)
    {
        var context = new StringBuilder();
        
        for (int i = 0; i < documents.Count; i++)
        {
            var content = documents[i].Content ?? "";
            var articleId = documents[i].ArticleId ?? "";
            
            context.AppendLine($"[Document {i + 1} - Article {articleId}]");
            context.AppendLine(content);
            context.AppendLine();
        }

        return context.ToString();
    }

    private List<ChatMessage> BuildChatMessages(
        string userQuestion,
        string context,
        List<ChatMessage>? conversationHistory)
    {
        var messages = new List<ChatMessage>();

        var systemPrompt = $@"You are a helpful AI assistant. Answer the user's question based on the provided context.

IMPORTANT RULES:
1. Use information from the provided context and your own knowledge to answer the question.
2. If the context doesn't contain relevant information, give user your answer and  some links to the relevant information.
3. Be concise and accurate
4. Cite which articleId of the document you used to get the information
5. If you're not sure, say you don't have enough information to answer the question.

CONTEXT:
{context}";

        messages.Add(ChatMessage.CreateSystemMessage(systemPrompt));

        if (conversationHistory != null && conversationHistory.Any())
        {
            messages.AddRange(conversationHistory);
        }

        messages.Add(ChatMessage.CreateUserMessage(userQuestion));

        return messages;
    }

    public async Task<ChatResponse> ChatStreamAsync(
        string userQuestion,
        List<ChatMessage>? conversationHistory = null,
        Action<string>? onChunkReceived = null,
        CancellationToken cancellationToken = default)
    {
        try
        {

            var questionEmbedding = await GenerateEmbeddingAsync(userQuestion);
            var relevantDocs = await VectorSearchAsync(questionEmbedding, _topK);

            if (!relevantDocs.Any())
            {
                var noResultMessage = "I couldn't find any relevant information to answer your question.";
                onChunkReceived?.Invoke(noResultMessage);
                
                return new ChatResponse
                {
                    Answer = noResultMessage,
                    Sources = new List<string>()
                    
                };
            }

            var context = BuildContext(relevantDocs);
            var messages = BuildChatMessages(userQuestion, context, conversationHistory);

            var fullAnswer = new StringBuilder();
            
            await foreach (var update in _chatClient.CompleteChatStreamingAsync(messages, cancellationToken: cancellationToken))
            {
                foreach (var contentPart in update.ContentUpdate)
                {
                    var chunk = contentPart.Text;
                    fullAnswer.Append(chunk);
                    onChunkReceived?.Invoke(chunk);
                }
            }

            return new ChatResponse
            {
                Answer = fullAnswer.ToString(),
                Sources = relevantDocs.Select(d => d.ArticleId).Distinct().ToList(),
                RelevantDocuments = relevantDocs
            };
        }
        catch (Exception ex)
        {
            throw;
        }
    }
    
    public async Task<ChatResponse> ChatWithArticleAsync(
        Guid articleId,
        string userQuestion,
        List<ChatMessage>? conversationHistory = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var relevantDocs = await SearchByArticleIdAsync(articleId);

            if (!relevantDocs.Any())
            {
                return new ChatResponse
                {
                    Answer = $"I couldn't find any documents for article {articleId}.",
                    Sources = new List<string>(),
                };
            }

            var context = BuildContext(relevantDocs);
            var messages = BuildChatMessages(userQuestion, context, conversationHistory);

            var chatResponse = await _chatClient.CompleteChatAsync(messages, cancellationToken: cancellationToken);
            var completion = chatResponse.Value;

            var answer = completion.Content[0].Text;

            return new ChatResponse
            {
                Answer = answer,
                Sources = new List<string> { articleId.ToString() },
                RelevantDocuments = relevantDocs
            };
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    private async Task<List<ArticleDocument>> SearchByArticleIdAsync(Guid articleId)
    {
        try
        {
            SearchResults<ArticleDocument> response = await _searchClient.SearchAsync<ArticleDocument>(
                searchText: "*", 
                new SearchOptions
                {
                    Filter = $"ArticleId eq '{articleId}'", 
                    Select = { "Id", "ArticleId", "Content" },
                    Size = 100 
                });

            var results = new List<ArticleDocument>();

            await foreach (SearchResult<ArticleDocument> result in response.GetResultsAsync())
            {
                results.Add(result.Document);
            }

            return results;
        }
        catch (Exception ex)
        {
            throw;
        }
    }
    
}

public class ChatResponse
{
    public string Answer { get; set; } = string.Empty;
    public List<string> Sources { get; set; } = new();
    public List<ArticleDocument>? RelevantDocuments { get; set; }
    
}
