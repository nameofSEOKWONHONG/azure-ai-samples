namespace Document.Intelligence.Agent.Features.Doc;

using System.Text.RegularExpressions;
using Microsoft.ML.Tokenizers;

public static class MlTokenizerChunker
{
    // 문장 경계(한/영 혼합)
    private static readonly Regex SentenceSplit =
        new(@"(?<=[\.?!。！？])\s+|\n{2,}", RegexOptions.Compiled);

    /// <summary>
    /// ML.Tokenizers(Tiktoken) 기반 토큰 청킹
    /// </summary>
    /// <param name="paragraphs">문단 나열</param>
    /// <param name="targetMinTokens">청크 배출 힌트 최소 토큰</param>
    /// <param name="targetMaxTokens">청크 최대 토큰(절대 초과 금지)</param>
    /// <param name="overlapTokens">인접 청크 간 겹침 토큰 수</param>
    /// <param name="encodingOrModel">
    /// 예: "cl100k_base", "o200k_base" 또는 "gpt-4o"(모델명) 등
    /// </param>
    public static IEnumerable<string> ChunkByTokens(
        IEnumerable<string> paragraphs,
        int targetMinTokens = 800,
        int targetMaxTokens = 1200,
        int overlapTokens = 200,
        string encodingOrModel = "cl100k_base")
    {
        // 1) 토크나이저 준비
        Tokenizer tokenizer = CreateTokenizer(encodingOrModel);

        var buffer = new List<int>(targetMaxTokens + overlapTokens);

        foreach (var para in paragraphs.Select(p => p?.Trim()).Where(p => !string.IsNullOrWhiteSpace(p)))
        {
            var sentences = SentenceSplit.Split(para)
                                         .Select(s => s.Trim())
                                         .Where(s => s.Length > 0);

            foreach (var sent in sentences)
            {
                var sentIds = tokenizer.EncodeToIds(sent); // 문자열→토큰ID :contentReference[oaicite:1]{index=1}

                // 문장 하나가 너무 긴 경우 하드 슬라이스
                if (sentIds.Count > targetMaxTokens)
                {
                    if (buffer.Count > 0)
                    {
                        yield return tokenizer.Decode(buffer); // 토큰ID→문자열 :contentReference[oaicite:2]{index=2}
                        buffer.Clear();
                    }

                    foreach (var hard in SliceWithOverlap(sentIds, targetMaxTokens, overlapTokens))
                        yield return tokenizer.Decode(hard);
                    continue;
                }

                // 버퍼에 수용 시도
                if (buffer.Count + sentIds.Count <= targetMaxTokens)
                {
                    buffer.AddRange(sentIds);
                }
                else
                {
                    // 배출 + overlap 꼬리 보존
                    yield return tokenizer.Decode(buffer);
                    var tail = TakeTail(buffer, overlapTokens);
                    buffer.Clear();
                    buffer.AddRange(tail);

                    if (buffer.Count + sentIds.Count <= targetMaxTokens)
                        buffer.AddRange(sentIds);
                    else
                    {
                        // overlap이 커서 안 들어가면 하드 슬라이스
                        foreach (var hard in SliceWithOverlap(sentIds, targetMaxTokens, overlapTokens))
                            yield return tokenizer.Decode(hard);
                    }
                }

                // targetMinTokens 도달 시 조기 배출
                if (buffer.Count >= targetMinTokens)
                {
                    yield return tokenizer.Decode(buffer);
                    var tail = TakeTail(buffer, overlapTokens);
                    buffer.Clear();
                    buffer.AddRange(tail);
                }
            }
        }

        if (buffer.Count > 0)
            yield return tokenizer.Decode(buffer);
    }

    private static Tokenizer CreateTokenizer(string encodingOrModel)
    {
        // encoding 이름으로 생성(예: cl100k_base / o200k_base)
        // 또는 모델명으로 생성(예: gpt-4o) — Data 패키지 필요
        try
        {
            return TiktokenTokenizer.CreateForEncoding(encodingOrModel);
        }
        catch
        {
            // encoding 이름이 아니면 모델명으로 시도
            return TiktokenTokenizer.CreateForModel(encodingOrModel);
        }
    }

    private static IEnumerable<IReadOnlyList<int>> SliceWithOverlap(
        IReadOnlyList<int> ids, int maxSize, int overlap)
    {
        if (maxSize <= 0) yield break;
        var step = Math.Max(1, maxSize - Math.Max(0, overlap));

        for (int start = 0; start < ids.Count; start += step)
        {
            var end = Math.Min(ids.Count, start + maxSize);
            yield return ids.Skip(start).Take(end - start).ToArray();
            if (end == ids.Count) yield break;
        }
    }

    private static IReadOnlyList<int> TakeTail(List<int> ids, int overlap)
    {
        if (overlap <= 0 || ids.Count == 0) return Array.Empty<int>();
        var take = Math.Min(overlap, ids.Count);
        return ids.GetRange(ids.Count - take, take).ToArray();
    }
}
