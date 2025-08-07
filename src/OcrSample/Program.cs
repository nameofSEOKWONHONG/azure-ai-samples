using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OcrSample.Services;
using OcrSample.Services.Documents;
using OcrSample.Services.Receipts;

var configuration = new ConfigurationBuilder()
    .AddUserSecrets<Program>()
    .AddEnvironmentVariables()
    .Build();

var services = new ServiceCollection();
services.AddSingleton<IConfiguration>(configuration);

services.AddAiReceipt(configuration);
services.AddAiDocument(configuration);

services.AddScoped<IMainService, MainService>();
var provider = services.BuildServiceProvider();
var main = provider.GetRequiredService<IMainService>();
await main.RunAsync();
