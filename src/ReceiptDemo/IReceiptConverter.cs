using Azure.AI.Vision.ImageAnalysis;
using ReceiptDemo.Models;

namespace ReceiptDemo;

public interface IReceiptConverter
{
    string ProviderName { get;}
    ReceiptExtract Convert(IReadOnlyList<DetectedTextLine> lines);
}
