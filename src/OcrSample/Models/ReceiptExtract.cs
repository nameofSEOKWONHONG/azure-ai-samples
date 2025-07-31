using System.Text.Json.Serialization;
using eXtensionSharp;

namespace OcrSample.Models;

/// <summary>
/// 영수증에서 추출한 핵심 메타데이터를 보관하는 DTO.
/// </summary>
public sealed class ReceiptExtract
{
    /// <summary>가맹점 상호 (가맹점명 + 지점명) </summary>
    [JsonPropertyName("merchant")]
    public string? Merchant { get; init; }
    
    /// <summary>
    /// 가맹점명
    /// </summary>
    [JsonPropertyName("merchant_brand")]
    public string? MerchantBrand { get; init; }
    
    /// <summary>
    /// 가맹점 지점명
    /// </summary>
    [JsonPropertyName("merchant_branch")]
    public string? MerchantBranch { get; init; }

    /// <summary>주소(원문)</summary>
    [JsonPropertyName("address")]
    public string? Address { get; init; }

    /// <summary>사업자등록번호(예: 536-37-00183)</summary>
    [JsonPropertyName("businessNumber")]
    public string? BusinessNumber { get; init; }

    /// <summary>합계 금액(원, 정수 형태)</summary>
    [JsonPropertyName("totalAmountWon")]
    public long? TotalAmountWon { get; init; }

    /// <summary>마스킹 포함 카드번호 원문(예: 6250-03 **-****- 4903 (C))</summary>
    [JsonPropertyName("cardNumberMasked")]
    public string? CardNumberMasked { get; init; }

    /// <summary>거래 일시 (로컬/원문 기준 파싱 결과)</summary>
    [JsonPropertyName("transactionDateTime")]
    public DateTime? TransactionDateTime { get; init; }

    public ReceiptExtract() { }

    public ReceiptExtract(
        string? merchant,
        string? merchantBrand,
        string? merchantBranch,
        string? address,
        string? businessNumber,
        long? totalAmountWon,
        string? cardNumberMasked,
        DateTime? transactionDateTime)
    {
        Merchant = merchant;
        MerchantBrand = merchantBrand.xValue<string>(merchant);
        MerchantBranch = merchantBranch.xValue<string>(merchant);
        Address = address;
        BusinessNumber = businessNumber;
        TotalAmountWon = totalAmountWon;
        CardNumberMasked = cardNumberMasked;
        TransactionDateTime = transactionDateTime;
    }

    /// <summary>
    /// 사람이 보기 쉬운 요약 문자열.
    /// </summary>
    public override string ToString()
        => $"{TransactionDateTime:yyyy-MM-dd HH:mm:ss}에 '{Merchant}'(사업자: {BusinessNumber}, 주소: {Address}, 상호: {MerchantBrand}, 지점: {MerchantBranch})에서 {CardNumberMasked.Trim()}로 {TotalAmountWon?.ToString("#,0")}원을 결재했다.";
}