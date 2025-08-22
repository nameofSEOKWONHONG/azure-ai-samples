using Document.Intelligence.Agent.Entities;
using Document.Intelligence.Agent.Infrastructure.Data;
using Document.Intelligence.Agent.Infrastructure.Session;
using Microsoft.Extensions.Logging;

namespace Document.Intelligence.Agent.Features.AiSearch;

public interface IGetDocumentService : IDiaExecuteServiceBase<string, string>;

/// <summary>
/// 문서 조회 및 blob 링크 제공
/// </summary>
public class GetDocumentService : DiaExecuteServiceBase<GetDocumentService, DiaDbContext, string, string>, IGetDocumentService
{
    public GetDocumentService(ILogger<GetDocumentService> logger, IDiaSessionContext session, DiaDbContext dbContext) : base(logger, session, dbContext)
    {
    }

    public override Task<Results<string>> ExecuteAsync(string request)
    {
        //TODO: INDEX 상세 조회
        throw new NotImplementedException();
    }
}