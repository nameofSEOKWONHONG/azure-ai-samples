using Document.Intelligence.Agent.Entities;
using Document.Intelligence.Agent.Infrastructure.Data;
using Document.Intelligence.Agent.Infrastructure.Session;
using Microsoft.Extensions.Logging;

namespace Document.Intelligence.Agent.Features.AiSearch;

public interface IFindDocumentService : IDiaExecuteServiceBase<string, string>
{
    
}

/// <summary>
///  AI SEARCH 문서 검색 및 blob 링크 제공
/// </summary>
public class FindDocumentService: DiaExecuteServiceBase<FindDocumentService, DiaDbContext, string, string>,
    IFindDocumentService
{
    public FindDocumentService(ILogger<FindDocumentService> logger, IDiaSessionContext session, DiaDbContext dbContext) : base(logger, session, dbContext)
    {
    }

    public override Task<Results<string>> ExecuteAsync(string request)
    {
        //TODO: INDEX 목록 조회
        throw new NotImplementedException();
    }
}