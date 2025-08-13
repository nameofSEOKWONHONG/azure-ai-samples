using System.ClientModel;
using Azure;
using Azure.AI.DocumentIntelligence;
using Azure.AI.OpenAI;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OcrSample;

var builder = Host.CreateApplicationBuilder(args);
builder.Configuration.AddUserSecrets<Program>();

var endpoint = new Uri(builder.Configuration["AZURE_OPENAI_ENDPOINT"]);
var apiKey = new ApiKeyCredential(builder.Configuration["AZURE_OPENAI_API_KEY"]);
var ChatDeployment = builder.Configuration["AZURE_OPENAI_GPT_NAME"];
var embedDeployment = builder.Configuration["AZURE_OPENAI_EMBED_MODEL"];

builder.Services.AddSingleton(
    new AzureOpenAIClient(
        endpoint,
        apiKey));

builder.Services.AddChatClient(services => 
        services.GetRequiredService<AzureOpenAIClient>()
            .GetChatClient(ChatDeployment)
            .AsIChatClient())
    //.UseDistributedCache()
    .UseLogging();

builder.Services.AddEmbeddingGenerator(services =>
    services.GetRequiredService<AzureOpenAIClient>()
        .GetEmbeddingClient(embedDeployment)
        .AsIEmbeddingGenerator());

builder.Services.AddSingleton(sp => new SearchClient(
    new Uri(builder.Configuration["AZURE_AI_SEARCH_ENDPOINT"]),
    "document-v1",
    new AzureKeyCredential(builder.Configuration["AZURE_AI_SEARCH_API_KEY"])));

builder.Services.AddSingleton(sp => new SearchIndexClient(
    new Uri(builder.Configuration["AZURE_AI_SEARCH_ENDPOINT"]),
    new AzureKeyCredential(builder.Configuration["AZURE_AI_SEARCH_API_KEY"])));

builder.Services.AddSingleton(sp => new DocumentIntelligenceClient(
    new Uri(builder.Configuration["AZURE_DOCUMENT_INTELLIGENCE_ENDPOINT"]),
    new AzureKeyCredential(builder.Configuration["AZURE_DOCUMENT_INTELLIGENCE_KEY"])));

builder.Services.AddSingleton<DocumentIntelligenceDemo>();

var host = builder.Build();
var service = host.Services.GetRequiredService<DocumentIntelligenceDemo>();
//await service.CreateIndexAsync();
//await service.UploadAsync();
await service.SearchAsync();

/*
 *   배치는 C#으로 개발.
 * OCR 프로젝트에서 개발해야 할 것
 *  -- 컨셉은 모두사인과 유사하게 가는 것이 좋아 보임. --
 * 1. 대량 처리 프로그램 개발
 *    - 테이블 설계
 *    - 분서 분류별 DB 적재 후 AI SEARCH에 데이터 적재
 *    - 파일은 blob storage에 적재
 * 2. UX 시나리오
 *    - 관리자 메뉴에 문서관리
 *    - 등록시 분류별 작성자 정보 추가하여 등록
 *    - 동록된 파일은 바로 적재
 *    - 문서 메뉴에서는 문서 조회 및 상세
 *    - 상세 화면에서 문서 표시 및 AI CHAT 검색 지원
 * 3. 데이터 시나리오
 *    - 문서 테이블
 *    - 문서 메타데이터 테이블
 *    - 문서 AI SEARCH INDEX
 *    - SQL SERVER 벡터 검색 -> 문서 테이블에서 조회 후 결과 반환 (TOP20)
 *      -> ID detail 조회 -> 파일 URL 및 AI SEARCH 결과 LLM 바인딩
 *      -> 파일 표시 및 LLM 바인딩에 따른 질의 응답
 */ 

