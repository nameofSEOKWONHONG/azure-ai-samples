using eXtensionSharp;
using OcrSample.Services.Documents;

namespace OcrSample.Services.Receipts;

public class ReceiptPipeline: IAiPipeline
{
    private readonly ITextEmbeddingService _textEmbeddingService;
    private readonly IReceiptSearchService _receiptSearchService;
    private readonly IReceiptAnalysisService _receiptAnalysisService;
    private readonly IReceiptLlmService _receiptLlmService;

    public ReceiptPipeline(ITextEmbeddingService textEmbeddingService,
        IReceiptSearchService receiptSearchService,
        IReceiptAnalysisService receiptAnalysisService,
        IReceiptLlmService receiptLlmService)
    {
        _textEmbeddingService = textEmbeddingService;
        _receiptSearchService = receiptSearchService;
        _receiptAnalysisService = receiptAnalysisService;
        _receiptLlmService = receiptLlmService;
    }

    public async Task Initialize()
    {
        var imageUrls = new[]
        {
            "https://cdn.banksalad.com/entities/etc/1517463655748-%EC%98%81%EC%88%98%EC%A6%9D.jpg",
            "https://raw.githubusercontent.com/nameofSEOKWONHONG/Jennifer/refs/heads/main/doc/%EC%98%81%EC%88%98%EC%A6%9D1.jpg"
        };
        
        foreach (var imageUrl in imageUrls)
        {
            var extract = await _receiptAnalysisService.AnalysisAsync(imageUrl);
            Console.WriteLine(extract.ToString());

            //convert to azure AI embedding texts.
            var vector = await _textEmbeddingService.GetEmbeddedText(extract.ToString());
            if (vector.xIsEmpty())
            {
                Console.WriteLine("vector is null");
            }

            Console.WriteLine($"dim = {vector.Length}");       // 인덱스 vector 차원과 일치해야 함(예: 1536/3072)

            //update azure AI search
            //update or create index before upload (as mongodb create index...)
            var isUpload = await _receiptSearchService.CreateIndexAndUploadAsync(extract, vector);
            Console.WriteLine(isUpload ? "create index, upload data and vector" : "upload failed");
        }
    }

    public async Task<string> RunAsync(string question)
    {
        var filter = await _receiptLlmService.GetQueryFilterAsync(question);

        var questionVector = await _textEmbeddingService.GetEmbeddedText(question);
        var results = await _receiptSearchService.SearchAsync(question, filter, questionVector);
        if (results.xIsNotEmpty())
        {
            return await _receiptLlmService.ReceiptAskResultAsync(results, question);
        }

        return string.Empty;
    }
}