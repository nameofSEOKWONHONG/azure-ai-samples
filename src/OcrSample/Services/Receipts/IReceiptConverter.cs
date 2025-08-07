using Azure.AI.Vision.ImageAnalysis;
using OcrSample.Models;

namespace OcrSample.Services.Receipts;

public interface IReceiptConverter
{
    string ProviderName { get;}
    ReceiptExtract Convert(IReadOnlyList<DetectedTextLine> lines);
}
