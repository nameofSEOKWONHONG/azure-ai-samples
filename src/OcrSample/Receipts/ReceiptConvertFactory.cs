using Azure.AI.Vision.ImageAnalysis;
using OcrSample.Services;

namespace OcrSample.Receipts;

public class ReceiptConvertFactory
{
    private readonly IEnumerable<IReceiptConverter> _converters;

    public ReceiptConvertFactory(IEnumerable<IReceiptConverter> converters)
    {
        _converters = converters;
    }

    public IReceiptConverter AutoPick(IReadOnlyList<DetectedTextLine> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);
        
        var fullText = string.Join("\n", lines.Select(l => l.Text).Where(t => !string.IsNullOrWhiteSpace(t)));
        foreach (var receiptConverter in _converters)
        {
            if (fullText.Contains(receiptConverter.ProviderName))
            {
                return receiptConverter;
            }
        }

        return default;
    }
}