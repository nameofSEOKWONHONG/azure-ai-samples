using System.Diagnostics;
using Azure;
using Azure.AI.DocumentIntelligence;
using Document.Intelligence.Agent.Entities;
using Document.Intelligence.Agent.Features.Chat.Models;
using Document.Intelligence.Agent.Infrastructure.Data;
using Document.Intelligence.Agent.Infrastructure.Session;
using eXtensionSharp;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace Document.Intelligence.Agent.Features.Doc;

public interface IDocumentAnalysisService : IDiaExecuteServiceBase<string, IEnumerable<DocChunk>>;
public class DocumentAnalysisService : DiaExecuteServiceBase<DocumentAnalysisService, DiaDbContext, string, IEnumerable<DocChunk>>, IDocumentAnalysisService
{
    private readonly DocumentIntelligenceClient _documentIntelligenceClient;
    private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingGenerator;

    public DocumentAnalysisService(ILogger<DocumentAnalysisService> logger, IDiaSessionContext session, DiaDbContext dbContext,
        DocumentIntelligenceClient documentIntelligenceClient,
        IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator) : base(logger, session, dbContext)
    {
        _documentIntelligenceClient = documentIntelligenceClient;
        _embeddingGenerator = embeddingGenerator;
    }

    public override async Task<IEnumerable<DocChunk>> ExecuteAsync(string request, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        await using var stream = File.OpenRead(request);
        var binary = await BinaryData.FromStreamAsync(stream, ct);
        var operation = await _documentIntelligenceClient.AnalyzeDocumentAsync(WaitUntil.Completed, "prebuilt-layout", binary, ct);
        var result = operation.Value;
        logger.LogInformation("File: {file}, Pages: {count}", request, result.Pages.Count);
        
        var list = new List<DocChunk>();
        foreach (var documentPage in result.Pages)
        {
            int seq = 0;
            var docId = request.xGetFileName().xGetHashCode();
            var paragraphs = DiExtractors.ExtractParagraphs(result, documentPage.PageNumber);
            var chunks = MlTokenizerChunker.ChunkByTokens(paragraphs, 
                targetMinTokens:800,
                targetMaxTokens:1200,
                overlapTokens:200,
                encodingOrModel:"o200k_base");

            var throttler = new SemaphoreSlim(2);
            var tasks = chunks.Select(async text =>
            {
                await throttler.WaitAsync(ct);

                try
                {
                    var extension = request.xGetExtension();
                    var vec = await _embeddingGenerator.GenerateVectorAsync(text, cancellationToken: ct);
                    return new DocChunk {
                        ChunkId = $"{docId}_{documentPage.PageNumber:D4}_{seq++:D3}",
                        DocId = docId,
                        SourceFileType = extension.Replace(".", ""),
                        SourceFilePath = request,
                        SourceFileName = request.xGetFileName(),
                        Page = documentPage.PageNumber,
                        Content = text,
                        ContentVector = vec.ToArray()
                    };
                }
                finally
                {
                    throttler.Release();
                }
            });
            var docs = await Task.WhenAll(tasks);
            list.AddRange(docs);
        }            
        sw.Stop();
        logger.LogInformation("File: {file}, Time: {time}", request, sw.Elapsed.TotalMilliseconds);

        return list;
    }
}