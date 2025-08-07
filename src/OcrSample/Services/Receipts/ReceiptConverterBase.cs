using Azure.AI.Vision.ImageAnalysis;
using OcrSample.Models;

namespace OcrSample.Services.Receipts;

public abstract class ReceiptConverterBase: IReceiptConverter
{
    protected ReceiptConverterBase(string providerName)
    {
        this.ProviderName = providerName;
    }

    public string ProviderName { get; }
    public abstract ReceiptExtract Convert(IReadOnlyList<DetectedTextLine> lines);
    
    protected (string Brand, string Branch) SplitBrandBranch(string? merchantFull)
    {
        if (string.IsNullOrWhiteSpace(merchantFull)) return ("", "");
        var s = merchantFull.Trim();

        // 1) 지점 키워드: "본점", "지점", "○○점", "○○리점", "○○동점", "○○역점" 등
        // 뒤에서부터 '점'으로 끝나는 토큰을 지점 후보로
        var tokens = s.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        for (int i = tokens.Length - 1; i >= 0; i--)
        {
            var t = tokens[i].Trim();
            // "성수점", "본점", "강남지점", "판교역점" 등을 포착
            if (t.EndsWith("점") || t.EndsWith("지점"))
            {
                var brand = string.Join(' ', tokens.Take(i));
                var branch = string.Join(' ', tokens.Skip(i)); // 다단어 지점 가능성
                if (string.IsNullOrWhiteSpace(brand)) brand = s; // 방어: 분리 실패 시 전체를 브랜드
                return (brand, branch);
            }
        }

        // 2) "점 포 명 : 브랜드 지점" 같은 라벨을 파싱한 뒤라도, 지점이 없을 수 있음
        return (s, ""); // 지점 미표기 케이스
    }    
}