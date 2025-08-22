using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;

namespace Document.Intelligence.Agent.Features.Chat.Models;

public class DocChunk
{
    /// <summary>
    /// docId_page_seq
    /// </summary>
    [SimpleField(IsKey = true)]
    public string ChunkId { get; set; }

    /// <summary>
    /// 소스 문서 ID/해시
    /// </summary>
    [SimpleField(IsFilterable = true, IsSortable = true)]
    public string DocId { get; set; }

    /// <summary>
    /// pdf|docx|pptx
    /// </summary>
    [SimpleField(IsFilterable = true)]
    public string SourceFileType { get; set; }
    
    [SimpleField(IsFilterable = true)]
    public string SourceFilePath { get; set; }
    
    [SimpleField(IsFilterable = true)]
    public string SourceFileName { get; set; }

    /// <summary>
    /// 1-based
    /// </summary>
    [SimpleField(IsFilterable = true, IsSortable = true)]
    public int Page { get; set; }

    /// <summary>
    /// 문단 텍스트(슬라이드 본문 포함)
    /// </summary>
    [SearchableField(AnalyzerName = LexicalAnalyzerName.Values.KoLucene)]
    public string Content { get; set; }
    
    /// <summary>
    /// 벡터(임베딩)
    /// </summary>
    [VectorSearchField(VectorSearchDimensions = 1536, VectorSearchProfileName = "hnsw")]
    public float[] ContentVector { get; set; } 
}