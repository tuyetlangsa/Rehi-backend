namespace Rehi.Application.Articles.CreateArticle;

using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;

public class ArticleDocument
{
    [SimpleField(IsKey = true)]
    public string Id { get; set; }
    
    [SimpleField(IsFilterable = true, IsFacetable = true)]
    public string ArticleId { get; set; }
    
    [SearchableField]
    public string Content { get; set; }
    
    [VectorSearchField(VectorSearchDimensions = 1536, VectorSearchProfileName = "vector-profile-1761831291939")]
    public IReadOnlyList<float> Embedding { get; set; }
}