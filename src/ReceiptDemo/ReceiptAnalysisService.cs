using Azure.AI.Vision.ImageAnalysis;
using ReceiptDemo.Models;

namespace ReceiptDemo;

public interface IReceiptAnalysisService
{
    Task<ReceiptExtract> AnalysisAsync(string imageUrl);
}

public class ReceiptAnalysisService : IReceiptAnalysisService
{
    private readonly ImageAnalysisClient _client;
    private readonly ReceiptConvertFactory _receiptConvertFactory;

    public ReceiptAnalysisService(ImageAnalysisClient client, ReceiptConvertFactory receiptConvertFactory)
    {
        _client = client;
        _receiptConvertFactory = receiptConvertFactory;
    }

    public async Task<ReceiptExtract> AnalysisAsync(string imageUrl)
    {
        ImageAnalysisResult result = await _client.AnalyzeAsync(
            new Uri(imageUrl),
            VisualFeatures.Caption | VisualFeatures.Read,
            new ImageAnalysisOptions { GenderNeutralCaption = true });
        
        Console.WriteLine("Image analysis results:");
        Console.WriteLine(" Caption:");
        Console.WriteLine($"   '{result.Caption.Text}', Confidence {result.Caption.Confidence:F4}");

        var lines = new List<DetectedTextLine>();
        Console.WriteLine(" Read:");
        foreach (DetectedTextBlock block in result.Read.Blocks)
        foreach (DetectedTextLine line in block.Lines)
        {
            Console.WriteLine($"   Line: '{line.Text}', Bounding Polygon: [{string.Join(" ", line.BoundingPolygon)}]");
            lines.Add(line);
            // foreach (DetectedTextWord word in line.Words)
            // {
            //     Console.WriteLine($"     Word: '{word.Text}', Confidence {word.Confidence:#.####}, Bounding Polygon: [{string.Join(" ", word.BoundingPolygon)}]");
            // }
        }
        
        var converter = _receiptConvertFactory.AutoPick(lines);
        return converter.Convert(lines);
    }
}



