using eXtensionSharp;
using Microsoft.Extensions.DependencyInjection;
using OcrSample.Services.Documents;
using OcrSample.Services.Receipts;

namespace OcrSample.Services;

public interface IMainService
{
    Task RunAsync();
}
 
public class MainService : IMainService
{
    private readonly IAiPipeline _receiptPipeline;
    private readonly IAiPipeline _documentPipeline;

    public MainService(
        [FromKeyedServices(AiFeatureConst.RECEIPT)] IAiPipeline receiptPipeline,
        [FromKeyedServices(AiFeatureConst.DOCUMENT)] IAiPipeline documentPipeline)
    {
        _receiptPipeline = receiptPipeline;
        _documentPipeline = documentPipeline;
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
        
        Console.WriteLine("모드를 선택하세요. (1. 영주증, 2.문서)");
        var input = Console.ReadLine();
        var map = new Dictionary<string, IAiPipeline>()
        {
            { "1", _receiptPipeline },
            { "2", _documentPipeline }
        };
        foreach (var keyValuePair in map)
        {
            await keyValuePair.Value.Initialize();
        }
        
        var instance = map[input];
        QUESTION:
        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.WriteLine("Enter Question (QUIT:Q) :");
        var question = Console.ReadLine();
        
        if (question.xIsEmpty())
        {
            Console.WriteLine("Question is empty, retry");
            goto QUESTION;
        }

        if (question.ToUpper().Equals("Q"))
            goto END;

        var result = await instance.RunAsync(question);
        Console.ForegroundColor = ConsoleColor.DarkYellow;
        Console.WriteLine(result);
        goto QUESTION;

        END:
        
        Console.WriteLine("Azure ocr sample end");
    }
}

public class AiFeatureConst
{
    /// <summary>
    /// 영수증
    /// </summary>
    public const string RECEIPT = nameof(RECEIPT);
    
    /// <summary>
    /// 문서 (pdf, pptx, xlsx, docx 등등...)
    /// </summary>
    public const string DOCUMENT = nameof(DOCUMENT);

    /// <summary>
    /// 영수증 인덱스
    /// </summary>
    public const string RECEIPT_INDEX_NAME = "receipt-v1";
    
    /// <summary>
    /// 문서 인덱스
    /// </summary>
    public const string DOCUMENT_INDEX_NAME = "document-v1";
}

