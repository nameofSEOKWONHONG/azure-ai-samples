using Azure.AI.OpenAI;
using eXtensionSharp;
using Microsoft.Extensions.Configuration;
using OpenAI.Chat;

namespace OcrSample.Services.Documents;

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

public class DocumentPipeline : IAiPipeline
{
    private readonly AzureOpenAIClient _azureOpenAiClient;
    private readonly ITextEmbeddingService _textEmbeddingService;
    private readonly IDocumentSearchService _documentSearchService;
    private readonly IDocumentLlmService _documentLlmService;
    private readonly IDocumentInitializer _documentInitializer;
    private readonly IConfiguration _configuration;

    public DocumentPipeline(
        AzureOpenAIClient azureOpenAiClient,
        ITextEmbeddingService textEmbeddingService,
        IDocumentSearchService documentSearchService,
        IDocumentLlmService documentLlmService,
        IDocumentInitializer documentInitializer,
        IConfiguration configuration)
    {
        _azureOpenAiClient = azureOpenAiClient;
        _textEmbeddingService = textEmbeddingService;
        _documentSearchService = documentSearchService;
        _documentLlmService = documentLlmService;
        _documentInitializer = documentInitializer;
        _configuration = configuration;
    }

    public async Task Initialize()
    {
        await _documentInitializer.InitializeAsync();
    }

    public async Task<string> RunAsync(string question)
    {
        // 1. 파일 첨부시 DRM 해제 또는 DRM 라벨 변경.
        // 2. Document Intelligence API를 이용한 페이지별 문자 추출
        // 3. 추출 문자 벡터화
        // 4. 페이지별 추출 문자 및 벡터를 KEY 생성하여 AI SEARCH에 업로드
        // 5. 사용자 잘의 형태소 분석
        // 6. 업로드된 문서 포함 AI SEARCH에 VECTOR 포함 질의 검색
        // 7. 검색 결과 중 일치하는 내역을 LLM에 전달하여 질의 수행
        // 8. 결과 표시
        
        // 질문별 형태소 분석
        var client = _azureOpenAiClient.GetChatClient(_configuration["AZURE_OPENAI_GPT_NAME"]);
        var chatMessages = new List<ChatMessage>()
        {
            new SystemChatMessage("너는 형태소 분석 및 추출 전문가다. 입력받은 문장을 형태소 단위로 추출하고 명사만 표시한다."),
            new UserChatMessage(question)
        };
        var resp = await client.CompleteChatAsync(chatMessages);
        var text = resp.Value.Content[0].Text;
        
        Console.WriteLine($"형태소:{text}");

        var questionVector = await _textEmbeddingService.GetEmbeddedText(text);
        var result = await _documentSearchService.SearchAsync(text, questionVector);

        if (result.xIsNotEmpty())
        {
            // foreach (var documentSearchResult in result)
            // {
            //     var docText = await _documentLlmService.DocumentSummary(documentSearchResult);
            //     if (docText.xIsNotEmpty())
            //     {
            //         documentSearchResult.Content = docText;
            //     }
            // }
            return await _documentLlmService.DocumentAskResultAsync(result, question);
        }

        return string.Empty;
    }
}