using System.Text.Encodings.Web;
using System.Text.Json;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using Document.Intelligence.Agent.Entities;
using Document.Intelligence.Agent.Entities.Chat;
using Document.Intelligence.Agent.Features.Chat.Models;
using Document.Intelligence.Agent.Infrastructure.Data;
using Document.Intelligence.Agent.Infrastructure.Session;
using eXtensionSharp;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;

namespace Document.Intelligence.Agent.Features.Chat.Services;

public interface IChatService : IDiaExecuteServiceBase<ChatRequest, Results<DocumentChatResult>>;

/// <summary>
/// AI SEARCH를 이용한 LLM 대화 서비스
/// </summary>
public class ChatService: DiaExecuteServiceBase<ChatService, DiaDbContext,  ChatRequest, Results<DocumentChatResult>>, IChatService
{
    private readonly IChatClient _chatClient;
    private readonly SearchClient _searchClient;
    private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingGenerator;
    private readonly IQuestionContextSwitchService _questionContextSwitchService;

    public ChatService(ILogger<ChatService> logger, IDiaSessionContext session, DiaDbContext dbContext,
        IChatClient chatClient,
        [FromKeyedServices(INDEX_CONST.DOCUMENT_INDEX)]SearchClient searchClient,
        IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
        IQuestionContextSwitchService questionContextSwitchService) : base(logger, session, dbContext)
    {
        _chatClient = chatClient;
        _searchClient = searchClient;
        _embeddingGenerator = embeddingGenerator;
        _questionContextSwitchService = questionContextSwitchService;
    }
    
    public override async Task<Results<DocumentChatResult>> ExecuteAsync(ChatRequest request, CancellationToken ct = default)
    {
        Results<DocumentChatResult> result = null;
        
        if (request.AgentId.xIsEmpty())
        {
            result = await NotImportAgentChat(request, ct);
        }
        else
        {
            result = await ImportAgentChat(request, ct);
        }

        return result;
    }

    private async Task<Results<DocumentChatResult>> NotImportAgentChat(ChatRequest request, CancellationToken ct = default)
    {
        var thread = await this.dbContext.ChatThreads.Where(m => m.Id == request.ThreadId)
            .FirstOrDefaultAsync(cancellationToken: ct);

        if (thread.xIsEmpty())
        {
            //THREAD 요약 제목 생성
            var messages = new List<ChatMessage>()
            {
                new ChatMessage(ChatRole.System, LlmConst.QUESTION_SUMMARY),
                new ChatMessage(ChatRole.User, request.CurrentQuestion)
            };
            var resp = await _chatClient.GetResponseAsync<string>(messages, cancellationToken: ct);
            var title = resp.Result.Trim();
            thread = new DOCUMENT_CHAT_THREAD()
            {
                Id = Guid.NewGuid(),
                Title = title.Length > 100 ? title[..100] : title,
                CreatedId = this.session.UserId,
                CreatedAt = this.session.GetNow()
            };
            await this.dbContext.ChatThreads.AddAsync(thread, ct);
        }
    
        var currentQuestionVector = await _embeddingGenerator.GenerateVectorAsync(request.CurrentQuestion, cancellationToken: ct);
        var question = new DOCUMENT_CHAT_QUESTION()
        {
            ThreadId = thread.Id,
            
            Id = Guid.NewGuid(),
            Question = request.CurrentQuestion,
            QuestionVector = currentQuestionVector.ToArray(),
            CreatedId = this.session.UserId,
            CreatedAt = this.session.GetNow()                  
        };

        var planResult = await GenerateQueryPlanAndResearches(thread, question, request.CurrentQuestion);
        if (planResult.researches.xIsEmpty()) return await Results<DocumentChatResult>.FailAsync("answer is empty");

        question.QueryPlan = JsonSerializer.Serialize(planResult.plan, _jsonOptions);
        question.ChunkIdList = planResult.researches
            .Select(m => m.ChunkId)
            .Where(m => !string.IsNullOrWhiteSpace(m))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        await this.dbContext.ChatQuestions.AddAsync(question, ct);
        await this.dbContext.ChatQuestionResearches.AddRangeAsync(planResult.researches, ct);

        var previous = await this.dbContext.ChatQuestions.AsNoTracking()
            .Where(m => m.ThreadId == thread.Id)
            .Where(m => m.Id == request.PreviousQuestionId)
            .FirstOrDefaultAsync(cancellationToken: ct);

        bool isContextSwitch = false;
        if (previous.xIsNotEmpty())
        {
            isContextSwitch = await _questionContextSwitchService.ExecuteAsync(
                new SearchDocumentContextSwitchRequest(
                    //이전
                    previous.Question,
                    previous.QuestionVector,
                    previous.QueryPlan,
                    previous.ChunkIdList,
                    //현재
                    question.Question,
                    question.QuestionVector,
                    question.QueryPlan,
                    question.ChunkIdList
                ), ct);
        }

        var result = await AskFromGpt(planResult.researches, thread.Id, request.CurrentQuestion, isContextSwitch);
        var cleanCitations = (result.Citations ?? Enumerable.Empty<PageCitation>())
            .Where(c => !string.IsNullOrWhiteSpace(c.File) && c.Page > 0)
            .Select(c => new { File = c.File.Trim(), c.Page })
            .DistinctBy(c => (c.File, c.Page))
            .Take(50)
            .Select(c => new DOCUMENT_CHAT_ANSWER_CITATION { File = c.File, Page = c.Page })
            .ToList();

        var answer = new DOCUMENT_CHAT_ANSWER()
        {
            QuestionId = question.Id,
            
            Id = Guid.NewGuid(),
            Answer = result.Answer,
            Citations = cleanCitations,
        };
        await this.dbContext.ChatAnswers.AddAsync(answer, ct);
        await this.dbContext.SaveChangesAsync(ct);

        if (answer.Answer.xIsEmpty() ||
            "답변할 수 없습니다".Contains(answer.Answer))
        {
            return await Results<DocumentChatResult>.FailAsync("answer is empty");
        }

        //debug
        var documentChatResult = new DocumentChatResult
        {
            ThreadId = thread.Id,
            QuestionId = question.Id,
            Answer = result.Answer,
            Citations = result.Citations
        };   

        return await Results<DocumentChatResult>.SuccessAsync(documentChatResult);        
    }

    private Task<Results<DocumentChatResult>> ImportAgentChat(ChatRequest request, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// TODO: AGENT 필더 추가해야 함.
    /// </summary>
    /// <param name="documentChatThread"></param>
    /// <param name="documentChatQuestion"></param>
    /// <param name="question"></param>
    /// <returns></returns>
    private async Task<(List<DOCUMENT_CHAT_QUESTION_RESEARCH> researches, QueryPlan plan)> GenerateQueryPlanAndResearches(DOCUMENT_CHAT_THREAD documentChatThread, DOCUMENT_CHAT_QUESTION documentChatQuestion, string question)
    {   
        var messages = new List<ChatMessage>()
        {
            new ChatMessage(ChatRole.System, LlmConst.QUERY_PLAN_PROMPT),
            new ChatMessage(ChatRole.User, question)
        };
        var resp = await _chatClient.GetResponseAsync<QueryPlan>(messages);
        var plan = resp.Result;
        
        //기존 조회 사항 제외
        // var maxExclude = 10;
        // plan.ExcludedChunkIds = plan.ExcludedChunkIds = _ocrDbContext.QuestionResearches
        //     .AsNoTracking()
        //     .Where(m => m.Question.ThreadId == documentThread.Id)
        //     .Select(m => m.ChunkId)
        //     .Take(maxExclude)
        //     .ToArray();

        ReadOnlyMemory<float> embedding = default;
        if (plan.UseVector)
        {
            var embedText = plan.VectorFromText.xIsNotEmpty() ? plan.VectorFromText : plan.Keyword.xValue<string>(string.Empty);
            embedding = await _embeddingGenerator.GenerateVectorAsync(embedText);
        }

        var options = new SearchOptions()
        {
            Size = Math.Clamp(plan.TopK, 1, 50),
            QueryType = SearchQueryType.Full
        };
        
        //TODO: INDEX에 의해 수정되어야 할 부분
        var selects = plan.Select.xIsNotEmpty()
            ? plan.Select
            : ["chunk_id", "doc_id", "page", "content", "source_file_name"];
        
        foreach (var f in selects.Distinct())
            options.Select.Add(f);

        options.Filter = BuildFilter(plan);

        if (plan.UseVector && embedding.xIsNotEmpty())
        {
            var knn = Math.Clamp(plan.TopK * 2, 10, 50);
            options.VectorSearch = new()
            {
                Queries =
                {
                    new VectorizedQuery(embedding)
                    {
                        Fields = { "content_vector" },
                        KNearestNeighborsCount = knn
                    }
                }
            };
        }

        var searchText = string.Empty;
        if (plan.UseKeyword && plan.Keyword.xIsNotEmpty())
        {
            options.SearchFields.Add("content");
            searchText = plan.Keyword!;
        }
        else
        {
            searchText = "*";
        }
        
        var searchResult = await _searchClient.SearchAsync<SearchDocument>(searchText, options);
        var results = new List<(SearchDocument Doc, double Score)>();
        await foreach (var hit in searchResult.Value.GetResultsAsync())
            results.Add((hit.Document, hit.Score ?? 0));
        
        //중위값 필터링
        var filtered = results
            .OrderByDescending(r => r.Score)
            .Take(Math.Clamp(plan.TopK, 1, 50))
            .ToList();     
        
        var currentHits = new List<DOCUMENT_CHAT_QUESTION_RESEARCH>();
        var currentChunkIds = new List<string>();
        
        foreach (var doc in filtered.Select(hit => hit.Doc))
        {
            if (!doc.TryGetValue("chunk_id", out var chunk_id)) continue;
            if (!doc.TryGetValue("content", out var content)) continue;
            if (!doc.TryGetValue("source_file_name", out var source_file_name)) continue;

            var cid = chunk_id.xValue<string>();
            var year = ExtractYear(source_file_name.xValue<string>());
            currentHits.Add(new DOCUMENT_CHAT_QUESTION_RESEARCH
            {
                Id = Guid.NewGuid(),
                ChunkId = cid,
                Content = content.ToString(),
                Year = year,
                SourceFileName = source_file_name.ToString()
            });
            currentChunkIds.Add(cid);
        }

        // var existIds = await _ocrDbContext.QuestionResearches.AsNoTracking()
        //     .Where(m => m.Question.ThreadId == documentThread.Id)
        //     .Where(m => currentChunkIds.Contains(m.ChunkId))
        //     .OrderByDescending(m => m.Id)
        //     .Select(m => m.ChunkId)
        //     .ToArrayAsync();
        
        var addList = currentHits
            //.Where(hit => !existIds.Contains(hit.ChunkId))
            .Select(hit => new DOCUMENT_CHAT_QUESTION_RESEARCH
            {
                QuestionId = documentChatQuestion.Id,
                
                Id = Guid.NewGuid(),
                ChunkId = hit.ChunkId,
                Content = hit.Content,
                Year = hit.Year,
                SourceFileName = hit.SourceFileName
            })
            .ToList();

        return (addList, plan);
    }

    /// <summary>
    /// AGENT의 필터 추가해야 함.
    /// </summary>
    /// <param name="p"></param>
    /// <returns></returns>
    private string BuildFilter(QueryPlan p)
    {
        static string Quote(string s) => $"'{s.Replace("'", "''")}'";
        var conds = new List<string>();

        if (!string.IsNullOrWhiteSpace(p.DocId))
            conds.Add($"doc_id eq {Quote(p.DocId!)}");

        if (!string.IsNullOrWhiteSpace(p.SourcePathEquals))
            conds.Add($"source_file_path eq {Quote(p.SourcePathEquals!)}");

        if (p.FileTypes != null && p.FileTypes.Length > 0)
        {
            // 화이트리스트(normalize)
            var allow = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "pdf","pptx","docx" };
            var filtered = p.FileTypes
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Select(t => t.Trim().ToLowerInvariant())
                .Where(t => allow.Contains(t))
                .ToArray();

            if (filtered.Length > 0)
            {
                var joined = string.Join(",", filtered.Select(x => x.Replace("'", "''")));
                conds.Add($"search.in(source_file_type, '{joined}', ',')");
            }
        }

        if (p.PageFrom.HasValue && p.PageTo.HasValue)
            conds.Add($"page ge {p.PageFrom.Value} and page le {p.PageTo.Value}");
        else if (p.PageFrom.HasValue)
            conds.Add($"page ge {p.PageFrom.Value}");
        else if (p.PageTo.HasValue)
            conds.Add($"page le {p.PageTo.Value}");

        if (p.ExcludedChunkIds.xIsNotEmpty())
        {
            var escaped = p.ExcludedChunkIds
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Select(id => id.Replace("'", "''"));
            var joined = string.Join(",", escaped);
            conds.Add($"not search.in(chunk_id, '{joined}', ',')");
        }

        return conds.xIsEmpty() ? null : string.Join(" and ", conds);
    }    
    
    JsonSerializerOptions _jsonOptions = new JsonSerializerOptions()
    {
        ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles,
        MaxDepth = 64,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = false,
    };
    /// <summary>
    /// TODO: AGENT로 동작하도록 해야 함.
    /// </summary>
    /// <param name="items"></param>
    /// <param name="threadId"></param>
    /// <param name="question"></param>
    /// <param name="isShift"></param>
    /// <returns></returns>
    private async Task<ChatResult> AskFromGpt(IEnumerable<DOCUMENT_CHAT_QUESTION_RESEARCH> items, Guid threadId, string question, bool isShift)
    {
        var reference = JsonSerializer.Serialize(items, _jsonOptions);
        var messages = new List<Microsoft.Extensions.AI.ChatMessage>()
        {
            new ChatMessage(ChatRole.System, LlmConst.ASK_PROMPT)
        };
        
        if (!isShift)
        {
            var history = await this.dbContext.ChatQuestions
                .AsNoTracking()
                .Where(q => q.ThreadId == threadId)
                .OrderByDescending(q => q.CreatedAt)
                .Take(3)
                .Select(q => new {
                    q.Question,
                    Answer = q.Answers.OrderByDescending(a => a.Id).Select(a => a.Answer).FirstOrDefault()
                })
                .ToListAsync();
            
            foreach (var item in history)
            {
                messages.Add(new ChatMessage(ChatRole.User, $"[Prev Q]\n{item.Question}"));
                messages.Add(new ChatMessage(ChatRole.Assistant, $"[Prev A]\n{item.Answer}"));
            }
        }

        messages.Add(new ChatMessage(ChatRole.User, $"<REF>{reference}</REF>"));
        messages.Add(new ChatMessage(ChatRole.User, question));
        
        var chatOptions = new ChatOptions()
        {
            MaxOutputTokens = 2048,
            Temperature = 0.2f,
            TopP = 0.1f,
        };
        var resp = await _chatClient.GetResponseAsync<ChatResult>(messages, chatOptions);
        resp.Result.ThreadId = threadId;
        return resp.Result;
    }

    private static int? ExtractYear(string file)
    {
        var m = System.Text.RegularExpressions.Regex.Match(file ?? "", @"\b(20\d{2})\b");
        return m.Success ? int.Parse(m.Value) : (int?)null;
    }
}