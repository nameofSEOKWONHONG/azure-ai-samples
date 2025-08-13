using System.Text.RegularExpressions;
using Azure.AI.DocumentIntelligence;

namespace OcrSample;

public class DiExtractors
{
        // 헤딩으로 볼 역할들
    private static readonly HashSet<string> HeadingRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "title", "sectionHeading", "pageHeader"
    };

    // 제외할 역할들(문단에서)
    private static readonly HashSet<string> ExcludedParagraphRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "pageHeader", "pageFooter", "footnote", "pageNumber"
    };

    /// <summary>
    /// 페이지별 헤딩(제목/섹션/헤더)을 상단→하단 순서로 반환.
    /// 중복/노이즈를 제거하고 공백 정규화.
    /// </summary>
    public static List<string> ExtractHeadings(AnalyzeResult result, int pageNumber,
        int minLen = 2, bool dedupe = true)
    {
        var list = new List<string>();
        if (result?.Paragraphs == null) return list;

        foreach (var p in result.Paragraphs)
        {
            if (!IsOnPage(p, pageNumber)) continue;
            if (string.IsNullOrWhiteSpace(p.Content)) continue;
            if (!p.Role.HasValue) continue;
            var role = p.Role.Value.ToString();
            if (!HeadingRoles.Contains(role)) continue;
            
            var t = CleanText(p.Content);
            if (t.Length < minLen) continue;
            list.Add(t);
        }

        // 순서 유지 dedupe
        if (dedupe) list = list.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        return list;
    }

    /// <summary>
    /// 페이지 본문 문단을 반환. 헤더/푸터/각주/페이지번호는 제외.
    /// 문장 경계 보존을 위해 문단 단위 그대로 돌려주고, 가벼운 노이즈 정리 적용.
    /// </summary>
    public static List<string> ExtractParagraphs(AnalyzeResult result, int pageNumber,
        int minLen = 2, bool dedupe = false)
    {
        var list = new List<string>();
        if (result?.Paragraphs == null) return list;

        foreach (var p in result.Paragraphs)
        {
            if (!IsOnPage(p, pageNumber)) continue;
            if (string.IsNullOrWhiteSpace(p.Content)) continue;

            if (p.Role.HasValue)
            {
                // 역할 기반 제외
                var role = p.Role.Value.ToString();
                if (p.Role != null && ExcludedParagraphRoles.Contains(role)) continue;
                // 헤딩 역할은 본문에서 제외(헤딩은 ExtractHeadings로만 수집)
                if (p.Role != null && HeadingRoles.Contains(role)) continue;
            }

            var t = CleanText(p.Content);
            if (t.Length < minLen) continue;

            list.Add(t);
        }

        // 옵션: 짧은 조각들 합치기(문단 사이 불필요한 분절 완화)
        list = MergeTinyParagraphs(list, targetMin: 120);

        if (dedupe) list = list.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        return list;
    }

    // === Helpers ===

    private static bool IsOnPage(DocumentParagraph p, int pageNumber)
        => p?.BoundingRegions?.Any(br => br.PageNumber == pageNumber) == true;

    // 1) 줄바꿈은 보존하고, 줄 내 연속 공백만 압축
    // 기존: new Regex(@"\s+", ...)
    // 공백/탭만, \r/\n은 제외
    private static readonly Regex MultiSpace =
        new(@"[^\S\r\n]+", RegexOptions.Compiled);
    // (선택) 과도한 빈 줄 줄이기: 3줄 이상 연속 → 2줄
    private static readonly Regex CollapseBlankLines =
        new(@"(\r?\n){3,}", RegexOptions.Compiled);    
    private static readonly Regex SelectedTag = new(@":selected:\s*", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex BracketBullet = new(@"^\s*(\[\]|\-|•|∙|·|\*)\s*", RegexOptions.Compiled);
    private static readonly Regex LeadingQMark = new(@"^\s*[?]\s*", RegexOptions.Compiled);

    private static string CleanText(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return string.Empty;

        var t = s.Replace("\r", "\n")
            .Replace("\u00A0", " ")
            .Replace("\u200B", "");

        t = SelectedTag.Replace(t, "");
        t = t.Trim();

        var lines = t.Split('\n')
            .Select(line =>
            {
                var x = line.Trim();
                x = BracketBullet.Replace(x, "");
                x = LeadingQMark.Replace(x, "");
                // 줄 내 여러 공백 → 하나 (줄바꿈은 유지)
                x = MultiSpace.Replace(x, " ");
                return x;
            })
            .Where(x => x.Length > 0);

        t = string.Join("\n", lines);

        // (선택) 빈 줄 너무 많으면 2줄로 정규화
        t = CollapseBlankLines.Replace(t, "\n\n");

        return t.Trim();
    }

    // 2) 짧은 문단 병합 시, 문단/줄 경계 보존 (공백 대신 줄바꿈으로 연결)
    private static List<string> MergeTinyParagraphs(List<string> input, int targetMin)
    {
        if (input.Count == 0) return input;

        var merged = new List<string>();
        var buf = new List<string>();
        int len = 0;

        void Flush()
        {
            if (buf.Count > 0)
            {
                // 기존: string.Join(" ", buf)
                merged.Add(string.Join(Environment.NewLine, buf).Trim());
                buf.Clear();
                len = 0;
            }
        }

        foreach (var p in input)
        {
            if (p.Length >= targetMin)
            {
                Flush();
                merged.Add(p);
            }
            else
            {
                buf.Add(p);
                len += p.Length;
                if (len >= targetMin) Flush();
            }
        }
        Flush();
        return merged;
    }
}