using System.Text.RegularExpressions;

namespace OcrSample;

public static class Chunker
{
    // 한국어/영문 혼합 문장 경계 정규식(완벽하진 않지만 실무 충분)
    // 마침표/물음표/느낌표/줄바꿈 등을 기준으로 split
    static readonly Regex SentenceSplit = new(@"(?<=[\.?!。！？])\s+|\n{2,}", RegexOptions.Compiled);

    public static IEnumerable<string> ChunkByBoundary(
        IEnumerable<string> paragraphs,
        int targetMin = 800,
        int targetMax = 1200,
        int overlap = 200)
    {
        var buf = new List<string>();
        int bufLen = 0;

        // 1) 문단 단위로 순회
        foreach (var para in paragraphs.Select(p => p?.Trim()).Where(p => !string.IsNullOrWhiteSpace(p)))
        {
            // 2) 문장으로 더 세분화
            var sentences = SentenceSplit.Split(para).Select(s => s.Trim()).Where(s => s.Length > 0);
            foreach (var sent in sentences)
            {
                // 길이가 아주 긴 “문장”이면(예: 표를 텍스트화), 안전하게 줄바꿈 기준으로 더 쪼갬
                foreach (var piece in SplitHard(sent, max: targetMax))
                {
                    if (bufLen + piece.Length <= targetMax)
                    {
                        buf.Add(piece);
                        bufLen += piece.Length + 1;
                    }
                    else
                    {
                        // 3) 완성된 청크 배출
                        yield return string.Join(" ", buf);

                        // 4) 오버랩 구성: 뒤에서부터 overlap만큼 재사용
                        var back = TakeFromBack(buf, overlap);
                        buf = new List<string> { back, piece };
                        bufLen = back.Length + 1 + piece.Length;
                    }
                }
            }
        }

        if (bufLen > 0)
            yield return string.Join(" ", buf);
    }

    // 줄바꿈/공백을 이용해 큰 조각을 강제로 자름
    static IEnumerable<string> SplitHard(string text, int max)
    {
        if (text.Length <= max) { yield return text; yield break; }

        var parts = Regex.Split(text, @"(\n+|\s{2,})").Where(p => !string.IsNullOrEmpty(p));
        var cur = new List<string>();
        int len = 0;

        foreach (var p in parts)
        {
            if (len + p.Length > max && len > 0)
            {
                yield return string.Join("", cur).Trim();
                cur.Clear();
                len = 0;
            }
            cur.Add(p);
            len += p.Length;
        }
        if (len > 0) yield return string.Join("", cur).Trim();
    }

    static string TakeFromBack(List<string> tokens, int overlap)
    {
        if (overlap <= 0) return string.Empty;
        var s = string.Join(" ", tokens);
        if (s.Length <= overlap) return s;
        return s.Substring(Math.Max(0, s.Length - overlap));
    }
}