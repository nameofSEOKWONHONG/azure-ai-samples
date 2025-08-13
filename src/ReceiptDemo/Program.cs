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

var builder = Host.CreateApplicationBuilder(args);
builder.Configuration.AddUserSecrets<Program>();

var endpoint = new Uri(builder.Configuration["AZURE_OPENAI_ENDPOINT"]);
var apiKey = new ApiKeyCredential(builder.Configuration["AZURE_OPENAI_API_KEY"]);
var chatDeployment = builder.Configuration["AZURE_OPENAI_GPT_NAME"];
var embedDeployment = builder.Configuration["AZURE_OPENAI_EMBED_MODEL"];

builder.Services.AddSingleton(
    new AzureOpenAIClient(
        endpoint,
        apiKey));

builder.Services.AddChatClient(services => 
        services.GetRequiredService<AzureOpenAIClient>()
            .GetChatClient(chatDeployment)
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

var receiptUri = new Uri("https://raw.githubusercontent.com/nameofSEOKWONHONG/Jennifer/refs/heads/main/doc/%EC%98%81%EC%88%98%EC%A6%9D1.jpg");
var host = builder.Build();
var client = host.Services.GetRequiredService<DocumentIntelligenceClient>();

var options = new AnalyzeDocumentOptions("prebuilt-receipt", receiptUri);
options.Features.Add(DocumentAnalysisFeature.QueryFields);
options.QueryFields.Add("대표자명");
var operation = await client.AnalyzeDocumentAsync(WaitUntil.Completed, options);
var receipts = operation.Value;

foreach (AnalyzedDocument receipt in receipts.Documents)
{
    if (receipt.Fields.TryGetValue("대표자명", out var 대표자필드))
    {
        if (대표자필드.FieldType == DocumentFieldType.String)
        {
            var 대표자명 = 대표자필드.ValueString;
            
            Console.WriteLine($"대표자 Name: '{대표자명}', with confidence {대표자필드.Confidence}");
        }
    }
    
    if (receipt.Fields.TryGetValue("MerchantName", out DocumentField merchantNameField))
    {
        if (merchantNameField.FieldType == DocumentFieldType.String)
        {
            string merchantName = merchantNameField.ValueString;

            Console.WriteLine($"Merchant Name: '{merchantName}', with confidence {merchantNameField.Confidence}");
        }
    }

    if (receipt.Fields.TryGetValue("TransactionDate", out DocumentField transactionDateField))
    {
        if (transactionDateField.FieldType == DocumentFieldType.Date)
        {
            DateTimeOffset? transactionDate = transactionDateField.ValueDate;

            Console.WriteLine($"Transaction Date: '{transactionDate}', with confidence {transactionDateField.Confidence}");
        }
    }

    if (receipt.Fields.TryGetValue("TransactionTime", out var transactionTimeField))
    {
        if (transactionTimeField.FieldType == DocumentFieldType.Time)
        {
            TimeSpan? transactionTime = transactionTimeField.ValueTime;
            
            Console.WriteLine($"Transaction Time: '{transactionTime}', with confidence {transactionTimeField.Confidence}");
        }
    }

    if (receipt.Fields.TryGetValue("Items", out DocumentField itemsField))
    {
        if (itemsField.FieldType == DocumentFieldType.List)
        {
            foreach (DocumentField itemField in itemsField.ValueList)
            {
                Console.WriteLine("Item:");

                if (itemField.FieldType == DocumentFieldType.Dictionary)
                {
                    IReadOnlyDictionary<string, DocumentField> itemFields = itemField.ValueDictionary;

                    if (itemFields.TryGetValue("Description", out DocumentField itemDescriptionField))
                    {
                        if (itemDescriptionField.FieldType == DocumentFieldType.String)
                        {
                            string itemDescription = itemDescriptionField.ValueString;

                            Console.WriteLine($"  Description: '{itemDescription}', with confidence {itemDescriptionField.Confidence}");
                        }
                    }

                    if (itemFields.TryGetValue("TotalPrice", out DocumentField itemTotalPriceField))
                    {
                        if (itemTotalPriceField.FieldType == DocumentFieldType.Currency)
                        {
                            double? itemTotalPrice = itemTotalPriceField.ValueCurrency.Amount;

                            Console.WriteLine($"  Total Price: '{itemTotalPrice}', with confidence {itemTotalPriceField.Confidence}");
                        }
                    }
                }
            }
        }
    }

    if (receipt.Fields.TryGetValue("Total", out DocumentField totalField))
    {
        if (totalField.FieldType == DocumentFieldType.Currency)
        {
            double total = totalField.ValueCurrency.Amount;

            Console.WriteLine($"Total: '{total}', with confidence '{totalField.Confidence}'");
        }
    }
}