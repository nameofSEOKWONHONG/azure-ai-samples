using System.Text.Json.Serialization;

namespace Document.Intelligence.Agent.Features.Receipt.Models;

/// <summary>
/// 영수증에서 추출한 핵심 메타데이터를 보관하는 DTO.
/// </summary>
public sealed class ReceiptExtract
{
    /// <summary>
    /// 가맹점 상호 (가맹점명 + 지점명)
    /// </summary>
    [JsonPropertyName("merchant")]
    public string Merchant { get; set; }

    /// <summary>
    /// 주소(원문)
    /// </summary>
    [JsonPropertyName("address")]
    public string Address { get; set; }
    
    /// <summary>
    /// 결재처 연락처
    /// </summary>
    [JsonPropertyName("phoneNumber")]
    public string PhoneNumber { get; set; }

    /// <summary>
    /// 합계 금액(원, 정수 형태)
    /// </summary>
    [JsonPropertyName("totalAmountWon")]
    public double TotalAmountWon { get; set; }

    /// <summary>
    /// 마스킹 포함 카드번호 원문(예: 6250-03 **-****- 4903 (C))
    /// </summary>
    [JsonPropertyName("cardNumberMasked")]
    public string CardNumberMasked { get; set; }

    /// <summary>
    /// 거래 일자 (로컬/원문 기준 파싱 결과)
    /// </summary>
    [JsonPropertyName("transactionDateTime")]
    public DateTimeOffset? TransactionDate { get; set; }
    
    /// <summary>
    /// 거래 시간
    /// </summary>
    [JsonPropertyName("transactionTime")]
    public TimeSpan? TransactionTime { get; set; }
    
    /// <summary>
    /// 날짜+시간을 합친 DateTime (원문 시각 유지, Kind=Unspecified)
    /// </summary>
    [JsonPropertyName("transactionDateTime")]
    public DateTime? TransactionDateTime
    {
        get
        {
            if (TransactionDate is null) return null;
            var dateOnly = TransactionDate.Value.Date;            // 00:00, Kind=Unspecified
            var time = TransactionTime ?? TimeSpan.Zero;

            // 24:00같은 비정상 입력 방지(있으면 다음날로 보정)
            if (time >= TimeSpan.FromDays(1))
            {
                var days = (int)time.TotalDays;
                time = time - TimeSpan.FromDays(days);
                dateOnly = dateOnly.AddDays(days);
            }

            var dt = dateOnly.Add(time);
            return DateTime.SpecifyKind(dt, DateTimeKind.Unspecified);
        }
    }    

    public ReceiptExtract() { }

    /// <summary>
    /// 사람이 보기 쉬운 요약 문자열.
    /// </summary>
    public override string ToString()
        => $"{TransactionDate:yyyy-MM-dd} {TransactionTime:g}에 '{Merchant}'(주소: {Address}, 전화번호: {PhoneNumber})에서 카드(카드번호: {CardNumberMasked.Trim()})로 {TotalAmountWon:#,0}원을 결재했다.";
}