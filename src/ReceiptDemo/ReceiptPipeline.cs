using eXtensionSharp;
using Microsoft.Extensions.AI;

namespace ReceiptDemo;

public interface IAiPipeline
{
    /// <summary>
    /// 인덱스 생성 및 초기화 작업, 프로그램 시작시 수행해야 함.
    /// </summary>
    /// <returns></returns>
    Task Initialize();
    
    /// <summary>
    /// 본 작업 수행.
    /// </summary>
    /// <param name="question"></param>
    /// <returns></returns>
    Task<string> RunAsync(string question);
}

public class ReceiptPipeline: IAiPipeline
{
    private readonly IEmbeddingGenerator<string,Embedding<float>> _embeddingGenerator;
    private readonly IReceiptSearchService _receiptSearchService;
    private readonly IReceiptAnalysisService _receiptAnalysisService;
    private readonly IReceiptLlmService _receiptLlmService;

    public ReceiptPipeline( 
        IEmbeddingGenerator<string,Embedding<float>> embeddingGenerator,
        IReceiptSearchService receiptSearchService,
        IReceiptAnalysisService receiptAnalysisService,
        IReceiptLlmService receiptLlmService)
    {
        _embeddingGenerator = embeddingGenerator;
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
            var vector = await _embeddingGenerator.GenerateAsync(extract.ToString());
            if (vector.xIsEmpty())
            {
                Console.WriteLine("vector is null");
            }

            Console.WriteLine($"dim = {vector.Vector.Length}");       // 인덱스 vector 차원과 일치해야 함(예: 1536/3072)

            //update azure AI search
            //update or create index before upload (as mongodb create index...)
            var isUpload = await _receiptSearchService.CreateIndexAndUploadAsync(extract, vector.Vector.ToArray());
            Console.WriteLine(isUpload ? "create index, upload data and vector" : "upload failed");
        }
    }

    public async Task<string> RunAsync(string question)
    {
        var filter = await _receiptLlmService.GetQueryFilterAsync(question);

        var questionVector = await _embeddingGenerator.GenerateAsync(question);
        var results = await _receiptSearchService.SearchAsync(question, filter, questionVector.Vector.ToArray());
        if (results.xIsNotEmpty())
        {
            return await _receiptLlmService.ReceiptAskResultAsync(results, question);
        }

        return string.Empty;
    }
}