using Azure.AI.Vision.ImageAnalysis;
using Document.Intelligence.Agent.Features.Receipt.Models;

namespace Document.Intelligence.Agent.Features.Receipt;

/// <summary>
/// 영수증별 추출시 사용할 기본 인터페이스 (현재 사용안함, PRE-BUILT로 처리 중)
/// </summary>
public interface IReceiptConverterBase
{
    string ProviderName { get;}
    ReceiptExtract Convert(IReadOnlyList<DetectedTextLine> lines);
}