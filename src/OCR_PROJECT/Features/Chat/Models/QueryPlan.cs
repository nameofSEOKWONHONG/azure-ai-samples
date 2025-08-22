namespace Document.Intelligence.Agent.Features.Chat.Models;

public sealed class QueryPlan
{
    // 검색 모드
    public bool UseVector { get; set; } = false;     // 벡터 검색 사용할지
    public bool UseKeyword { get; set; } = true;     // 키워드(BM25) 사용할지
    public int TopK { get; set; } = 10;              // KNN, 또는 Size

    // 키워드
    public string Keyword { get; set; }             // 사용자 핵심 키워드(없으면 null)

    // 정규화된 필터 파라미터 (여기서만 받아서 OData는 코드가 생성)
    public string DocId { get; set; }
    public string[] FileTypes { get; set; }         // 예: ["pdf","pptx","docx"]
    public string SourcePathEquals { get; set; }    // 전체 경로 정확히 일치
    public int? PageFrom { get; set; }               // 1-base
    public int? PageTo { get; set; }

    // 반환 필드
    public string[] Select { get; set; }            // 예: ["chunk_id","doc_id","page","content"]
    
    // 벡터 관련
    public string VectorFromText { get; set; }      // 임베딩 생성에 사용할 텍스트(없으면 Keyword 사용)
    public string[] ExcludedChunkIds { get; set; }
}