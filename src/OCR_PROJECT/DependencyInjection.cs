using System.ClientModel;
using Azure;
using Azure.AI.DocumentIntelligence;
using Azure.AI.OpenAI;
using Azure.Identity;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Document.Intelligence.Agent.Entities;
using Document.Intelligence.Agent.Features;
using Document.Intelligence.Agent.Features.Document.AiSearch;
using Document.Intelligence.Agent.Features.Document.Chat;
using Document.Intelligence.Agent.Features.Drm.M365;
using Document.Intelligence.Agent.Features.Drm.M365.Models;
using Document.Intelligence.Agent.Features.Graph;
using Document.Intelligence.Agent.Features.Receipt;
using Document.Intelligence.Agent.Infrastructure.Session;
using eXtensionSharp;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Graph;

namespace Document.Intelligence.Agent;

public static class DependencyInjection
{
    public static void AddDiaService(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<DiaDbContext>(optionsBuilder =>
        {
            var connection = configuration.GetConnectionString("DbConnection");
            optionsBuilder.UseSqlServer(connection, o =>
            {
                o.UseVectorSearch();
            });
            optionsBuilder.UseLazyLoadingProxies();
#if DEBUG
            optionsBuilder.EnableSensitiveDataLogging()
                .EnableThreadSafetyChecks()
                .EnableDetailedErrors();
#endif
        });
        
        services.AddScoped(_ =>
        {
            var service = new BlobServiceClient(configuration["OCR:AZURE_BLOB_STORAGE_CONNECTION"].xValue<string>());
            var container =
                service.GetBlobContainerClient(configuration["OCR:AZURE_BLOB_STORAGE_CONTAINER"].xValue<string>());
            container.CreateIfNotExists(PublicAccessType.None);
            return container;
        });
        
        var tenantId     = configuration["GRAPH:TENANT"];
        var clientId     = configuration["GRAPH:CLIENT_ID"];
        var clientSecret = configuration["GRAPH:CLIENT_SECRET"];
        var authUrl = configuration["GRAPH:AUTH_URL"];

        // Azure.Identity 자격증명
        var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);

        // scopes는 ".default" 고정 (앱 권한)
        var scopes = new[] { authUrl };
        
        services.AddScoped(_ => new GraphServiceClient(credential, scopes));
        services.AddScoped<IGraphApiService, GraphApiService>();

        services.AddScoped<IBlobStorageService, BlobStorageService>();
        services.AddScoped<IDiaSessionContext, DiaSessionContext>();
        services.AddHttpClient();
        services.AddHttpContextAccessor();

        services.AddDrmHandler(configuration);
        services.AddReceiptService(configuration);
        services.AddDocumentService(configuration);
    }
    
    /// <summary>
    /// M365 DRM 등록
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configuration"></param>
    private static void AddDrmHandler(this IServiceCollection services, IConfiguration configuration)
    {
        // DRM 관련
        services.AddScoped<IDrmHandler, DrmHandler>();      
        services.Configure<DrmConfig>(configuration.GetSection("DRM"));
    }
    
    /// <summary>
    /// 영수증 서비스 등록
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configuration"></param>
    private static void AddReceiptService(this IServiceCollection services, IConfiguration configuration)
    {   
        var endpoint = new Uri(configuration["OCR:AZURE_OPENAI_ENDPOINT"]);
        var apiKey = new ApiKeyCredential(configuration["OCR:AZURE_OPENAI_API_KEY"]);
        var chatDeployment = configuration["OCR:AZURE_OPENAI_GPT_NAME"];
        var embedDeployment = configuration["OCR:AZURE_OPENAI_EMBED_MODEL"];
        
        services.AddChatClient(_ =>
            {
                var client = new AzureOpenAIClient(endpoint, apiKey);
                var chat = client.GetChatClient(chatDeployment);
                return chat.AsIChatClient();
            })
            .UseLogging(); 
            //.UseDistributedCache()

            services.AddEmbeddingGenerator(_ =>
                {
                    var client = new AzureOpenAIClient(endpoint, apiKey);
                    var embeddingClient = client.GetEmbeddingClient(embedDeployment);
                    return embeddingClient.AsIEmbeddingGenerator();
                })
                .UseLogging();

        services.AddScoped(_ => new SearchIndexClient(
            new Uri(configuration["OCR:AZURE_AI_SEARCH_ENDPOINT"].xValue<string>()),
            new AzureKeyCredential(configuration["OCR:AZURE_AI_SEARCH_API_KEY"].xValue<string>())));

        services.AddScoped(_ => new DocumentIntelligenceClient(
            new Uri(configuration["OCR:AZURE_DOCUMENT_INTELLIGENCE_ENDPOINT"].xValue<string>()),
            new AzureKeyCredential(configuration["OCR:AZURE_DOCUMENT_INTELLIGENCE_KEY"].xValue<string>())));        

        services.AddKeyedScoped(INDEX_CONST.RECEIPT_INDEX, (_, _) => new SearchClient(
            new Uri(configuration["OCR:AZURE_AI_SEARCH_ENDPOINT"].xValue<string>()),
            "receipt-v1",
            new AzureKeyCredential(configuration["OCR:AZURE_AI_SEARCH_API_KEY"].xValue<string>())
        ));

        services.AddScoped<IReceiptService, ReceiptService>();
        services.AddScoped<IReceiptExtractService, ReceiptExtractService>();
        services.AddScoped<IReceiptAiSearchService, ReceiptAiSearchService>();
    }

    private static void AddDocumentService(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddKeyedScoped(INDEX_CONST.DOCUMENT_INDEX, (_, _) => new SearchClient(
            new Uri(configuration["OCR:AZURE_AI_SEARCH_ENDPOINT"].xValue<string>()),
            "document-v1",
            new AzureKeyCredential(configuration["OCR:AZURE_AI_SEARCH_API_KEY"].xValue<string>())
        ));

        #region [문서 데이터 관리]

        services.AddScoped<ICreateDocumentIndexService, CreateDocumentIndexService>();
        services.AddScoped<IUploadDocumentService, UploadDocumentService>();        

        #endregion

        #region [문서 LLM 채팅 관리]

        services.AddScoped<IChatService, ChatService>();
        services.AddScoped<IQuestionContextSwitchService, QuestionContextSwitchService>();        

        #endregion
    }

    public static Task UseOcrService(this WebApplication application)
    {
        // ArgumentException.ThrowIfNullOrWhiteSpace(application.Configuration["DRM:IDA:CLIENT_ID"].xValue<string>());
        // ArgumentException.ThrowIfNullOrWhiteSpace(application.Configuration["DRM:IDA:REDIRECT_URI"].xValue<string>());
        // ArgumentException.ThrowIfNullOrWhiteSpace(application.Configuration["DRM:IDA:CERT_THUMB_PRINT"].xValue<string>());
        // ArgumentException.ThrowIfNullOrWhiteSpace(application.Configuration["DRM:IDA:CLIENT_SECRET"].xValue<string>());
        // ArgumentException.ThrowIfNullOrWhiteSpace(application.Configuration["DRM:IDA:TENANT"].xValue<string>());
        //
        // ArgumentException.ThrowIfNullOrWhiteSpace(application.Configuration["DRM:APP:NAME"].xValue<string>());
        // ArgumentException.ThrowIfNullOrWhiteSpace(application.Configuration["DRM:APP:VERSION"].xValue<string>());
        //
        // ArgumentException.ThrowIfNullOrWhiteSpace(application.Configuration["DRM:MIP:TARGET_LABEL_ID"].xValue<string>());

        // await using var scope = application.Services.CreateAsyncScope();
        // var instance = scope.ServiceProvider.GetRequiredService<IReceiptAiSearchService>();
        // await ((IReceiptAiSearchIndexInitializeService)instance).InitializeIndexAsync();
        return Task.CompletedTask;
    }
}