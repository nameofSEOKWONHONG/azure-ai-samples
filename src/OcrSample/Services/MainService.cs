using eXtensionSharp;

namespace OcrSample.Services;

public interface IMainService
{
    Task RunAsync();
}
 
public class MainService : IMainService
{
    private readonly ITextEmbeddingService _textEmbeddingService;
    private readonly IReceiptSearchService _receiptSearchService;
    private readonly IReceiptAnalysisService _receiptAnalysisService;
    private readonly ILlmService _llmService;

    public MainService(ITextEmbeddingService textEmbeddingService,
        IReceiptSearchService receiptSearchService,
        IReceiptAnalysisService receiptAnalysisService,
        ILlmService llmService)
    {
        _textEmbeddingService = textEmbeddingService;
        _receiptSearchService = receiptSearchService;
        _receiptAnalysisService = receiptAnalysisService;
        _llmService = llmService;
    }
    
    public async Task RunAsync()
    {
        Console.WriteLine("OCR 영수증 이미지 텍스트 검출 및 검색 예제");
        Console.WriteLine("순서는 아래와 같습니다.");
        Console.WriteLine("==========================================================================");
        Console.WriteLine("1. 이미지를 특정 포멧에 맞춰 문자열화 합니다.");
        Console.WriteLine("2. 추출된 문자열을 특정 포멧에 맞추어 Embedding 합니다.");
        Console.WriteLine("3. AZURE AI SEARCH에 INDEX를 생성합니다.");
        Console.WriteLine("4. AZURE AI SEARCH에 Vector를 포함하여 업로드 합니다.");
        Console.WriteLine("5. AZURE OPENAI GPT-4o에 질의에 관련 Filter 생성을 요청합니다.");
        Console.WriteLine("6. 생성된 필터를 조합하여 AZURE AI SEARCH에 질의합니다.");
        Console.WriteLine("7. 질의한 영수증에 대한 결과를 표시합니다.");
        Console.WriteLine("==========================================================================");
        
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
                goto END;
            }

            Console.WriteLine($"dim = {vector.Length}");       // 인덱스 vector 차원과 일치해야 함(예: 1536/3072)

            //update azure AI search
            //update or create index before upload (as mongodb create index...)
            var isUpload = await _receiptSearchService.CreateIndexAndUploadAsync(extract, vector);
            Console.WriteLine(isUpload ? "create index, upload data and vector" : "upload failed");
        }

        //question azure AI search
        QUESTION:
        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.WriteLine("Enter Question (QUIT:Q) :");
        var question = Console.ReadLine();

        if (question.xIsEmpty())
        {
            Console.WriteLine("Question is empty, retry");
            goto QUESTION;
        }

        if (question.ToUpper().Equals("Q")) goto END;

        var filter = await _llmService.GetQueryFilterAsync(question);

        var questionVector = await _textEmbeddingService.GetEmbeddedText(question);
        var results = await _receiptSearchService.SearchAsync(question, filter, questionVector);
        if (results.xIsNotEmpty())
        {
            var text = await _llmService.ReceiptAskResultAsync(results, question);
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine(text);
        }
        goto QUESTION;


        END:
        Console.WriteLine("Vision Ocr Sample End");        
    }
}