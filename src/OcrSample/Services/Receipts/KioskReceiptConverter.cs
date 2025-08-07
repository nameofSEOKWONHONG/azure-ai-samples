using System.Globalization;
using System.Text.RegularExpressions;
using Azure.AI.Vision.ImageAnalysis;
using OcrSample.Models;

namespace OcrSample.Services.Receipts;

public sealed class KioskReceiptConverter : ReceiptConverterBase
{
    // 라벨 정규식(띄어쓰기·콜론 변형 허용)
    static readonly Regex RxMerchant   = new(@"(가\s*맹\s*점|점\s*포\s*명)\s*[:：]\s*(.+)", RegexOptions.Compiled);
    static readonly Regex RxAddress    = new(@"주\s*소\s*[:：]\s*(.+)", RegexOptions.Compiled);
    static readonly Regex RxBizNo      = new(@"사업자\s*(등록)?\s*번\s*호?\s*[:：]\s*([\d\-]+)", RegexOptions.Compiled);
    static readonly Regex RxCardNo     = new(@"카\s*드\s*번\s*호?\s*[:：]\s*([0-9\-\*\s]+)", RegexOptions.Compiled);
    static readonly Regex RxDateTime   = new(@"(거래\s*일\s*시|결제\s*일\s*시)\s*[:：]\s*(\d{4}[-/]\d{2}[-/]\d{2}\s+\d{2}:\d{2}:\d{2})", RegexOptions.Compiled);
    static readonly Regex RxTotalLabel = new(@"(합\s*계|총\s*액)", RegexOptions.Compiled);

    // 금액 후보(쉼표/점/공백 허용), '원' 포함 라인 우선 탐지에 사용
    static readonly Regex RxAmountCandidate = new(@"[0-9][0-9\.\,\s]*", RegexOptions.Compiled);
    static readonly Regex RxHasWon          = new(@"\s*원\s*$", RegexOptions.Compiled);

    static long? NormalizeWon(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        var s = raw.Replace(" ", "").Replace(",", "").Replace(".", "");
        return long.TryParse(s, NumberStyles.None, CultureInfo.InvariantCulture, out var n) ? n : null;
    }

    static (double X, double Y) Center(DetectedTextLine l)
    {
        var pts = l.BoundingPolygon;
        double x=0, y=0;
        foreach (var p in pts) { x += p.X; y += p.Y; }
        return (x / pts.Count, y / pts.Count);
    }

    public KioskReceiptConverter() : base("UNOSPAY") // 이 양식의 특징 문자열(식별용)
    {
    }

    public override ReceiptExtract Convert(IReadOnlyList<DetectedTextLine> lines)
    {
        string? merchant = null, address = null, bizNo = null, cardNo = null;
        DateTime? trxAt = null;
        long? total = null;

        // 라인 전처리
        var enriched = lines.Select(l => new
        {
            Line = l,
            Text = (l.Text ?? string.Empty).Trim(),
            Center = Center(l)
        }).ToList();

        // 1) 라벨 기반 직접 추출
        foreach (var e in enriched)
        {
            var t = e.Text;

            if (merchant is null)
            {
                var m = RxMerchant.Match(t);
                if (m.Success) merchant = m.Groups[2].Value.Trim();
            }
            if (address is null)
            {
                var m = RxAddress.Match(t);
                if (m.Success) address = m.Groups[1].Value.Trim();
            }
            if (bizNo is null)
            {
                var m = RxBizNo.Match(t);
                if (m.Success) bizNo = m.Groups[2].Value.Trim();
            }
            if (cardNo is null)
            {
                var m = RxCardNo.Match(t);
                if (m.Success) cardNo = Regex.Replace(m.Groups[1].Value, @"\s+", " ").Trim();
            }
            if (trxAt is null)
            {
                var m = RxDateTime.Match(t);
                if (m.Success)
                {
                    var s = m.Groups[2].Value;
                    // '-' 또는 '/' 포맷 모두 시도
                    if (DateTime.TryParseExact(s, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d1))
                        trxAt = d1;
                    else if (DateTime.TryParseExact(s, "yyyy/MM/dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d2))
                        trxAt = d2;
                }
            }
        }

        // 2) 합계 금액 탐색
        // 우선순위:
        //   A) '총액/총 액/합계' 라인 주변에서 금액 후보
        //   B) '원'으로 끝나는 금액 라인 중 가장 아래(또는 가장 큰 금액)
        if (total is null)
        {
            var totalLabels = enriched.Where(e => RxTotalLabel.IsMatch(e.Text)).ToList();
            if (totalLabels.Count > 0)
            {
                foreach (var label in totalLabels)
                {
                    // 레이블과 Y좌표가 가까운(+아래쪽) 라인들 우선
                    var near = enriched
                        .Where(e => e.Center.Y >= label.Center.Y - 40 && e.Center.Y <= label.Center.Y + 200)
                        .OrderBy(e => Math.Abs(e.Center.Y - label.Center.Y))
                        .ToList();

                    foreach (var cand in near)
                    {
                        // 같은 라인에도 숫자가 있을 수 있음(예: "총액 : 57,000원")
                        var text = cand.Text;
                        if (RxHasWon.IsMatch(text) || RxAmountCandidate.IsMatch(text))
                        {
                            var m = RxAmountCandidate.Match(text);
                            if (m.Success)
                            {
                                var n = NormalizeWon(m.Value);
                                if (n is > 0)
                                {
                                    total = n;
                                    break;
                                }
                            }
                        }
                    }
                    if (total is not null) break;
                }
            }

            // 보조 전략: '원'으로 끝나는 라인 전체에서 가장 큰 금액 사용
            if (total is null)
            {
                long max = 0;
                foreach (var e in enriched)
                {
                    if (!RxHasWon.IsMatch(e.Text)) continue;
                    var m = RxAmountCandidate.Match(e.Text);
                    if (!m.Success) continue;
                    var n = NormalizeWon(m.Value);
                    if (n > max) max = n.Value;
                }
                if (max > 0) total = max;
            }
        }
        
        var splitBrand = this.SplitBrandBranch(merchant);

        return new ReceiptExtract(
            merchant,
            splitBrand.Brand,
            splitBrand.Branch,
            address,
            bizNo,
            total,
            cardNo,
            trxAt
        );
    }
}