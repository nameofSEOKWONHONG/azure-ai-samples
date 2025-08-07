using System.Globalization;
using System.Text.RegularExpressions;
using Azure.AI.Vision.ImageAnalysis;
using OcrSample.Models;

namespace OcrSample.Services.Receipts;

public sealed class IcReceiptConverter : ReceiptConverterBase
{
    static readonly Regex RxMerchant   = new(@"가\s*맹\s*점\s*[:：]\s*(.+)", RegexOptions.Compiled);
    static readonly Regex RxAddress    = new(@"주\s*소\s*[:：]\s*(.+)",     RegexOptions.Compiled);
    static readonly Regex RxBizNo      = new(@"사업자\s*[:：]\s*([\d\-]+)", RegexOptions.Compiled);
    static readonly Regex RxCardNo     = new(@"카드번호\s*[:：]\s*([0-9\-\*\s]+(?:\([A-Za-z]\))?)", RegexOptions.Compiled);
    static readonly Regex RxDateTime   = new(@"거래일시\s*[:：]\s*(\d{4}/\d{2}/\d{2}\s+\d{2}:\d{2}:\d{2})", RegexOptions.Compiled);
    static readonly Regex RxTotalLabel = new(@"합\s*계", RegexOptions.Compiled);
    
    static readonly Regex RxAmountCandidate = new(@"[0-9][0-9\.\,\s]*", RegexOptions.Compiled);

    // 금액 정규화: "7.500", "6. 818", "1,234" -> 7500, 6818, 1234
    static long? NormalizeWon(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        // 공백 제거 후 소수점이 아니라 천단위로 쓰인 '.'도 제거
        var s = raw.Replace(" ", "").Replace(",", "");
        // 한국 영수증은 소수점 금액이 사실상 없음 → 모든 '.' 제거
        s = s.Replace(".", "");
        if (long.TryParse(s, NumberStyles.None, CultureInfo.InvariantCulture, out var n)) return n;
        return null;
    }

    // Y 중심좌표 계산용
    static (double X, double Y) Center(DetectedTextLine l)
    {
        // BoundingPolygon이 시계/반시계 4점이라고 가정
        var pts = l.BoundingPolygon;
        double x = 0, y = 0;
        foreach (var p in pts) { x += p.X; y += p.Y; }
        return (x / pts.Count, y / pts.Count);
    }
    
    public IcReceiptConverter() : base("IC신용승인")
    {
    }

    public override ReceiptExtract Convert(IReadOnlyList<DetectedTextLine> lines)
    {
        string? merchant = null, address = null, bizNo = null, cardNo = null;
        DateTime? trxAt = null;
        long? total = null;

        // 라인 전처리: 텍스트/센터좌표를 함께 보유
        var enriched = lines.Select(l => new
        {
            Line = l,
            Text = l.Text?.Trim() ?? "",
            Center = ReceiptUtils.Center(l)
        }).ToList();

        // 1) 라벨 기반 직접 추출
        foreach (var e in enriched)
        {
            var t = e.Text;

            if (merchant is null)
            {
                var m = ReceiptUtils.RxMerchant.Match(t);
                if (m.Success) merchant = m.Groups[1].Value.Trim();
            }
            if (address is null)
            {
                var m = ReceiptUtils.RxAddress.Match(t);
                if (m.Success) address = m.Groups[1].Value.Trim();
            }
            if (bizNo is null)
            {
                var m = ReceiptUtils.RxBizNo.Match(t);
                if (m.Success) bizNo = m.Groups[1].Value.Trim();
            }
            if (cardNo is null)
            {
                var m = ReceiptUtils.RxCardNo.Match(t);
                if (m.Success) cardNo = Regex.Replace(m.Groups[1].Value, @"\s+", " ").Trim();
            }
            if (trxAt is null)
            {
                var m = ReceiptUtils.RxDateTime.Match(t);
                if (m.Success && DateTime.TryParseExact(
                        m.Groups[1].Value, "yyyy/MM/dd HH:mm:ss",
                        CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                {
                    trxAt = dt;
                }
            }
        }

        // 2) 합계: "합 계/합계" 라인 주변에서 금액 후보 검색
        if (total is null)
        {
            // 합계 라인들 찾기
            var totalLabelLines = enriched.Where(e => ReceiptUtils.RxTotalLabel.IsMatch(e.Text)).ToList();

            foreach (var label in totalLabelLines)
            {
                // 전략 A: 뒤쪽 3~4개 라인에서 숫자 후보 찾기
                int idx = enriched.IndexOf(label);
                var selectedLine = enriched.Skip(idx + 1).FirstOrDefault();

                if (selectedLine is not null)
                {
                    var m = ReceiptUtils.RxAmountCandidate.Match(selectedLine.Text);
                    if (m.Success)
                    {
                        var n = ReceiptUtils.NormalizeWon(m.Value);
                        if (n is not null && n > 0)
                        {
                            total = n;
                            break;
                        }
                    }
                }
                
                if (total is not null) break;
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