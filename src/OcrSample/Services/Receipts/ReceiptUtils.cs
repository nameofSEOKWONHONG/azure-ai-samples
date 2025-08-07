using System.Globalization;
using System.Text.RegularExpressions;
using Azure.AI.Vision.ImageAnalysis;
using eXtensionSharp;

namespace OcrSample.Services.Receipts;

public class ReceiptUtils
{
    // 라벨 정규식(콜론/공백/한글 간격 허용)
    public static readonly Regex RxMerchant   = new(@"가\s*맹\s*점\s*[:：]\s*(.+)", RegexOptions.Compiled);
    public static readonly Regex RxAddress    = new(@"주\s*소\s*[:：]\s*(.+)",     RegexOptions.Compiled);
    public static readonly Regex RxBizNo      = new(@"사업자\s*[:：]\s*([\d\-]+)", RegexOptions.Compiled);
    public static readonly Regex RxCardNo     = new(@"카드번호\s*[:：]\s*([0-9\-\*\s]+(?:\([A-Za-z]\))?)", RegexOptions.Compiled);
    public static readonly Regex RxDateTime   = new(@"거래일시\s*[:：]\s*(\d{4}/\d{2}/\d{2}\s+\d{2}:\d{2}:\d{2})", RegexOptions.Compiled);
    public static readonly Regex RxTotalLabel = new(@"합\s*계", RegexOptions.Compiled);

    // 금액 후보(숫자, 점/콤마/공백 허용)
    public static readonly Regex RxAmountCandidate = new(@"[0-9][0-9\.\,\s]*", RegexOptions.Compiled);

    // 금액 정규화: "7.500", "6. 818", "1,234" -> 7500, 6818, 1234
    public static long? NormalizeWon(string raw)
    {
        if (raw.xIsEmpty()) return null;
        // 공백 제거 후 소수점이 아니라 천단위로 쓰인 '.'도 제거
        var s = raw.Replace(" ", "").Replace(",", "");
        // 한국 영수증은 소수점 금액이 사실상 없음 → 모든 '.' 제거
        s = s.Replace(".", "");
        if (long.TryParse(s, NumberStyles.None, CultureInfo.InvariantCulture, out var n)) 
            return n;
        
        return null;
    }

    // Y 중심좌표 계산용
    public static (double X, double Y) Center(DetectedTextLine l)
    {
        // BoundingPolygon이 시계/반시계 4점이라고 가정
        var pts = l.BoundingPolygon;
        double x = 0, y = 0;
        foreach (var p in pts) { x += p.X; y += p.Y; }
        return (x / pts.Count, y / pts.Count);
    }
}