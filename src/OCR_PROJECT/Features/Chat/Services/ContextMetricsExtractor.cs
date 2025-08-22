using Document.Intelligence.Agent.Features.Chat.Models;

namespace Document.Intelligence.Agent.Features.Chat.Services;

internal static class ContextMetricsExtractor
{
    /// <summary>
    /// ExtractRequest에 포함된 이전 정보와 현재 정보를 비교하여 측정 수치를 반환한다.
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public static ContextMetrics Extract(this ExtractRequest request)
    {
        // 1) 임베딩 코사인 (이전 없으면 1.0로 처리: "초기 동일 문맥" 가정)
        double embCos = CosineSafe(request.PrevEmbedding, request.CurEmbedding);

        // 2) 엔터티 자카드
        var prevEnt = ToSetCI(request.PrevEntities);
        var curEnt  = ToSetCI(request.CurEntities);
        double entJ = Jaccard(prevEnt, curEnt);

        // 3) 필터 변화율
        double fchg = ChangedFieldRatio(request.PrevPlan, request.CurPlan);

        // 4) 결과(ChunkId) 겹침률
        var prevIds = ToSetCS(request.PrevChunkIds);
        var curIds  = ToSetCS(request.CurChunkIds);
        double over = Jaccard(prevIds, curIds);

        return new ContextMetrics(
            embedding_cosine: embCos,
            entity_jaccard: entJ,
            filter_changed_ratio: fchg,
            result_overlap: over,
            intent_is_new: request.IntentIsNew
        );
    }

    // --- Helpers --------------------------------------------------------

    private static double CosineSafe(float[] a, float[] b)
    {
        if (a == null || b == null) return 1.0; // 초기 상태 관용 처리
        int n = Math.Min(a.Length, b.Length);
        if (n == 0) return 1.0;

        double dot = 0, na = 0, nb = 0;
        for (int i = 0; i < n; i++)
        {
            dot += a[i] * b[i];
            na  += a[i] * a[i];
            nb  += b[i] * b[i];
        }
        double denom = Math.Sqrt(na) * Math.Sqrt(nb);
        return denom > 0 ? dot / denom : 1.0;
    }

    private static ISet<string> ToSetCI(IEnumerable<string> src) =>
        (src ?? [])
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

    private static ISet<string> ToSetCS(IEnumerable<string> src) =>
        (src ?? [])
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s.Trim())
            .ToHashSet(StringComparer.Ordinal);

    private static double Jaccard<T>(ISet<T> a, ISet<T> b)
    {
        if ((a == null || a.Count == 0) && (b == null || b.Count == 0)) return 1.0;
        if (a == null || b == null) return 0.0;
        int inter = a.Intersect(b).Count();
        int union = a.Count + b.Count - inter;
        return union == 0 ? 1.0 : (double)inter / union;
    }

    // 필요 필드만 비교(프로젝트 사양에 따라 확장)
    private static double ChangedFieldRatio(QueryPlan prev, QueryPlan cur)
    {
        if (prev == null && cur == null) return 0.0;
        if (prev == null || cur == null) return 1.0;

        int total = 0, changed = 0;
        void Cmp<T>(T x, T y)
        {
            total++;
            if ((x == null) != (y == null) || (x != null && !x.Equals(y))) changed++;
        }

        Cmp(prev.DocId, cur.DocId);
        Cmp(prev.SourcePathEquals, cur.SourcePathEquals);
        Cmp(string.Join(",", prev.FileTypes ?? []),
           string.Join(",", cur.FileTypes ?? []));
        Cmp(prev.PageFrom, cur.PageFrom);
        Cmp(prev.PageTo, cur.PageTo);

        return total == 0 ? 0.0 : (double)changed / total;
    }
}