using Document.Intelligence.Agent.Worker;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<DocumentIndexingWorker>();

var host = builder.Build();
host.Run();