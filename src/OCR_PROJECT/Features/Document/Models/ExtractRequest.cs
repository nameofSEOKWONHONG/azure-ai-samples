namespace Document.Intelligence.Agent.Features.Document.Models;

public sealed record ExtractRequest(
    float[] PrevEmbedding,
    float[] CurEmbedding,
    IEnumerable<string> PrevEntities,
    IEnumerable<string> CurEntities,
    QueryPlan PrevPlan,
    QueryPlan CurPlan,
    IEnumerable<string> PrevChunkIds,
    IEnumerable<string> CurChunkIds,
    bool IntentIsNew
);

// 출력 DTO (프롬프트 변수명과 동일)
public sealed record ContextMetrics(
    double embedding_cosine,
    double entity_jaccard,
    double filter_changed_ratio,
    double result_overlap,
    bool   intent_is_new
);