using System.Diagnostics;
using Azure.Identity;
using Document.Intelligence.Agent;
using Document.Intelligence.Agent.Entities;
using Document.Intelligence.Agent.Features.Document.Chat;
using Document.Intelligence.Agent.Features.Document.Models;
using Document.Intelligence.Agent.Features.Graph;
using DocumentFormat.OpenXml.Packaging;
using eXtensionSharp;
using Microsoft.EntityFrameworkCore;
using Microsoft.Graph;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);
builder.Configuration.AddUserSecrets<Program>();

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Services.AddLogging(config => {
    config.AddSerilog(Log.Logger, dispose: true);
});

builder.Services.AddDbContext<DiaDbContext>(optionsBuilder =>
{
    var connection = builder.Configuration.GetConnectionString("DbConnection");
    optionsBuilder.UseSqlServer(connection);
    if (builder.Environment.IsDevelopment())
    {
        optionsBuilder.EnableSensitiveDataLogging()
            .EnableThreadSafetyChecks()
            .EnableDetailedErrors();
    }
});

builder.Services.AddDiaService(builder.Configuration);

var tenantId     = builder.Configuration["GRAPH:TENANT"];
var clientId     = builder.Configuration["GRAPH:CLIENT_ID"];
var clientSecret = builder.Configuration["GRAPH:CLIENT_SECRET"];
var authUrl = builder.Configuration["GRAPH:AUTH_URL"];

// Azure.Identity 자격증명
var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);

// scopes는 ".default" 고정 (앱 권한)
var scopes = new[] { authUrl };
        
builder.Services.AddScoped(_ => new GraphServiceClient(credential, scopes));

Log.Logger.Information("Starting app...");
try
{
    var host = builder.Build();
    await using var scope = host.Services.CreateAsyncScope();

    #region [chat test]

    // var chatService = scope.ServiceProvider.GetRequiredService<IChatService>();
    // var questions = new[]
    // {
    //     "근속년수에 따른 복지 제도 안내",
    //     "데이터 표준화란?",
    //     "표준 용어란?",
    //     "데이터 오프젝트 명명 규칙",
    //     "GEN AI 파트장은 누구?",
    //     "데이터 표준화 기대효과",
    //     "표준 단어란?"
    // };
    // var expectedAnswers = new[]
    // {
    //     "5년 근속자, 현금 100만원",
    //     "원칙을 수립",
    //     "일관된 이해",
    //     "단어 결합 시 밑줄",
    //     "김경백",
    //     "의사소통",
    //     "표준단어"
    // };
    //
    // var threadId = Guid.Empty;
    // var questionId = Guid.Empty;
    // var sw = Stopwatch.StartNew();
    // for(var i=0;i<questions.Length;i++)
    // {
    //     var question = questions[i];
    //     sw.Restart();
    //     
    //     var result = await chatService.ExecuteAsync(new ChatRequest()
    //     {
    //         ThreadId = threadId,
    //         PreviousQuestionId = questionId,
    //         CurrentQuestion = question
    //     });
    //     threadId = result.ThreadId;
    //     questionId = result.QuestionId;
    //
    //     Log.Logger.Information("llm answer: {answer}, question: {question}, expected: {expected}", result.Answer, question, expectedAnswers[i]);
    //
    //     if (result.Citations.xIsNotEmpty())
    //     {
    //         foreach (var citation in result.Citations)
    //         {
    //             Log.Logger.Information("llm answer citaion file: {file}, page: {page}", citation.File.xValue<string>("NONE"), citation.Page.xValue<string>("NONE"));    
    //         }
    //     }
    //
    //     var correct = result.Answer.Contains(expectedAnswers[i]);
    //         
    //     if (i == 0)
    //     {
    //         var split = expectedAnswers[i].xSplit();
    //         correct = result.Answer.Contains(split[0]) && result.Answer.Contains(split[1]);
    //     }
    //
    //     Log.Logger.Information("llm answer correct: {correct}", correct);
    //     sw.Stop();
    //     Log.Logger.Information("llm answer time: {time}/ms", sw.Elapsed.Milliseconds);
    // }

    #endregion

    var graph = scope.ServiceProvider.GetRequiredService<GraphServiceClient>();
    var sites = await graph.Sites.GetAsync(m =>
    {
        m.QueryParameters.Search = "gowitco";
        m.QueryParameters.Top = 50;
        m.QueryParameters.Select = ["id","name","displayName","webUrl","siteCollection"];
    });
    var selectedSite = sites.Value.First(m => m.DisplayName == "CS팀");
    var drives = await graph.Sites[selectedSite.Id].Drives.GetAsync(r =>
    {
        r.QueryParameters.Select = ["id", "name", "driveType", "webUrl"];
        r.QueryParameters.Top = 50;
    });
    var selectedDrive = drives.Value.First(m => m.Name == "문서");
    var items = await graph.Drives[selectedDrive.Id].Items["root"].Children
        .GetAsync(r =>
        {
            r.QueryParameters.Top = 200;
        });

    if (items.xIsNotEmpty())
    {
        var files = items.Value.Where(m => m.File != null).ToList();
        var selectedFiles = files.Where(m =>
            m.File.MimeType == "application/vnd.openxmlformats-officedocument.wordprocessingml.document" ||
            m.File.MimeType == "application/msword").ToList();
        if(selectedFiles.xIsNotEmpty())
        {
            var driveId = selectedFiles[0].ParentReference.DriveId;
            var itemId = selectedFiles[0].Id;
            var item = await graph.Drives[driveId].Items[itemId].GetAsync(r => r.QueryParameters.Select =
                ["name", "file", "size", "fileSystemInfo"]);

            await using (var content = await graph.Drives[driveId].Items[itemId].Content.GetAsync())
            {
                await using (var fs = File.Create($"./{selectedFiles[0].Name}"))
                {
                    await content.CopyToAsync(fs);    
                }
            }
            
            await using var stream = File.OpenRead($"./{selectedFiles[0].Name}");
            using var doc = WordprocessingDocument.Open(stream, false);
            var text = doc.MainDocumentPart.Document.Body.Descendants<DocumentFormat.OpenXml.Wordprocessing.Text>();
            if (text.xIsNotEmpty())
            {
                
            }
        }
    }

}
catch (Exception ex)
{
    Log.Logger.Error(ex, "An error occurred during application.");
}
finally 
{
    Log.Logger.Debug("Ending app...");
    Log.CloseAndFlush();
}