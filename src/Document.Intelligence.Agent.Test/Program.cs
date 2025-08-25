using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Document.Intelligence.Agent;
using Document.Intelligence.Agent.Entities;
using Document.Intelligence.Agent.Test;
using DocumentFormat.OpenXml.Drawing.Diagrams;
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

builder.Services.AddSingleton<ServiceBusClient>(sp =>
{
    var con = sp.GetRequiredService<IConfiguration>();
    return new ServiceBusClient(con["OCR:AZURE_SERVICE_BUS_ENDPOINT"]);
});
builder.Services.AddKeyedScoped("SENDER", (sp, _) =>
{
    var client = sp.GetRequiredService<ServiceBusClient>();
    return client.CreateSender("indexing-queue");
});
builder.Services.AddKeyedScoped("RECEIVER", (sp,_) =>
{
    var client = sp.GetRequiredService<ServiceBusClient>();
    return client.CreateReceiver("indexing-queue");
});

var tenantId     = builder.Configuration["GRAPH:TENANT"];
var clientId     = builder.Configuration["GRAPH:CLIENT_ID"];
var clientSecret = builder.Configuration["GRAPH:CLIENT_SECRET"];
var authUrl = builder.Configuration["GRAPH:AUTH_URL"];

// Azure.Identity 자격증명
var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);

// scopes는 ".default" 고정 (앱 권한)
var scopes = new[] { authUrl };
        
builder.Services.AddScoped(_ => new GraphServiceClient(credential, scopes));
builder.Services.AddScoped<GraphApiSample>();

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

    #region [graph api sample]

    // var service = scope.ServiceProvider.GetRequiredService<GraphApiSample>();
    // await service.RunAsync();    

    #endregion

    // var serviceBusSender = scope.ServiceProvider.GetRequiredKeyedService<ServiceBusSender>("SENDER");
    // var body = new
    // {
    //     Title = "test",
    //     Name = "hello",
    //     Tag = "world"
    // };
    // var message = new ServiceBusMessage(body.xSerialize());
    // await serviceBusSender.SendMessageAsync(message);

    var serviceBusReceiver = scope.ServiceProvider.GetRequiredKeyedService<ServiceBusReceiver>("RECEIVER");
    var recv = await serviceBusReceiver.ReceiveMessageAsync();
    var message = recv.Body.ToObjectFromJson<Message>();
    await serviceBusReceiver.CompleteMessageAsync(recv);
    //or
    //await serviceBusReceiver.DeadLetterMessageAsync(recv, "실패사유");
    Log.Logger.Information("Id:{id}, Title:{title}, Name:{name}, Tag:{tag}", message.Id, message.Title, message.Name, message.Tag);


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

class Message
{
    public Guid Id { get; set; }
    public string Title { get; set; }
    public string Name { get; set; }
    public string Tag { get; set; }
}