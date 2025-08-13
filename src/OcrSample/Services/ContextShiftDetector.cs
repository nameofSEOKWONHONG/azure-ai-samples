using OcrSample.Services.Documents;

namespace OcrSample.Services;

public sealed class ContextShiftDetector
{
    private float[] _topicCenter; // EMA 중심
    private HashSet<string> _lastEntities = new();
    private HashSet<string> _lastChunkIds = new();
    private QueryPlan _lastPlan;

    public record Signal(
        double EmbSimToCenter,
        double EntityJaccard,
        bool   IntentIsNew,
        double ChangedFilterRatio,
        double ResultOverlap
    );

    public Signal ComputeSignals(
        float[] curEmbedding,
        IEnumerable<string> curEntities,
        bool intentIsNew,
        QueryPlan curPlan,
        IEnumerable<string> curChunkIds)
    {
        // 1) 임베딩
        double embSim = (_topicCenter == null) ? 1.0
                      : Cosine(curEmbedding.AsMemory(), _topicCenter.AsMemory());

        // 2) 엔터티 자카드
        var curSet = curEntities?.Where(x=>!string.IsNullOrWhiteSpace(x))
                                 .Select(x=>x.Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase) 
                                 ?? new HashSet<string>();
        double entityJ = Jaccard(_lastEntities, curSet);

        // 3) 필터 변화율
        double changedRatio = ChangedFieldRatio(_lastPlan, curPlan);

        // 4) 결과 겹침률
        var curIds = curChunkIds?.ToHashSet(StringComparer.Ordinal) ?? new();
        double overlap = Jaccard(_lastChunkIds, curIds);

        return new Signal(embSim, entityJ, intentIsNew, changedRatio, overlap);
    }

    public bool IsShift(Signal s)
    {
        double w1=0.35, w2=0.2, w3=0.2, w4=0.15, w5=0.1;
        double score = w1*(1 - s.EmbSimToCenter)
                     + w2*(1 - s.EntityJaccard)
                     + w3*(s.IntentIsNew ? 1 : 0)
                     + w4*(s.ChangedFilterRatio)
                     + w5*(1 - s.ResultOverlap);
        return score >= 0.5;
    }

    public void Commit(float[] curEmbedding, IEnumerable<string> curEntities,
                       QueryPlan curPlan, IEnumerable<string> curChunkIds, bool shifted)
    {
        if (shifted) {
            _topicCenter = (float[])curEmbedding.Clone();
            _lastEntities = curEntities?.ToHashSet(StringComparer.OrdinalIgnoreCase) ?? new();
            _lastPlan = curPlan;
            _lastChunkIds = curChunkIds?.ToHashSet(StringComparer.Ordinal) ?? new();
        } else {
            _topicCenter = UpdateTopicCenter(_topicCenter, curEmbedding);
            _lastEntities = curEntities?.ToHashSet(StringComparer.OrdinalIgnoreCase) ?? _lastEntities;
            _lastPlan = curPlan ?? _lastPlan;
            _lastChunkIds = curChunkIds?.ToHashSet(StringComparer.Ordinal) ?? _lastChunkIds;
        }
    }

    static double Jaccard<T>(ISet<T> a, ISet<T> b)
    {
        if ((a == null || a.Count == 0) && (b == null || b.Count == 0)) return 1.0;
        if (a == null || b == null) return 0.0;
        int inter = a.Intersect(b).Count();
        int union = a.Count + b.Count - inter;
        return union == 0 ? 1.0 : (double)inter / union;
    }

    static double ChangedFieldRatio(QueryPlan prev, QueryPlan cur)
    {
        if (prev == null || cur == null) return prev == cur ? 0 : 1;
        int total=0, changed=0;
        void Cmp<T>(string name, T x, T y)
        {
            total++;
            if ((x==null) != (y==null) || (x!=null && !x.Equals(y))) changed++;
        }
        Cmp(nameof(QueryPlan.DocId), prev.DocId, cur.DocId);
        Cmp(nameof(QueryPlan.SourcePathEquals), prev.SourcePathEquals, cur.SourcePathEquals);
        Cmp(nameof(QueryPlan.FileTypes), string.Join(",", prev.FileTypes ?? Array.Empty<string>()),
                                      string.Join(",", cur.FileTypes ?? Array.Empty<string>()));
        Cmp(nameof(QueryPlan.PageFrom), prev.PageFrom, cur.PageFrom);
        Cmp(nameof(QueryPlan.PageTo), prev.PageTo, cur.PageTo);
        return total==0 ? 0 : (double)changed/total;
    }
    
    static double Cosine(ReadOnlyMemory<float> a, ReadOnlyMemory<float> b)
    {
        var va = a.Span; var vb = b.Span;
        double dot=0, na=0, nb=0;
        for (int i=0; i<va.Length; i++) { dot += va[i]*vb[i]; na += va[i]*va[i]; nb += vb[i]*vb[i]; }
        return dot / (Math.Sqrt(na) * Math.Sqrt(nb) + 1e-12);
    }

// 토픽 중심(EMA) 업데이트
    static float[] UpdateTopicCenter(float[] center, float[] cur, double alpha = 0.3)
    {
        if (center is null) return (float[])cur.Clone();
        for (int i=0; i<center.Length; i++)
            center[i] = (float)(alpha*cur[i] + (1-alpha)*center[i]);
        return center;
    }
}
