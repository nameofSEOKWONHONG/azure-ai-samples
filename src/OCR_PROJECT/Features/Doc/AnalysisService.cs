using Document.Intelligence.Agent.Entities;
using Document.Intelligence.Agent.Infrastructure.Data;
using Document.Intelligence.Agent.Infrastructure.Session;
using Microsoft.Extensions.Logging;

namespace Document.Intelligence.Agent.Features.Doc;

public interface IAnalysisService : IDiaExecuteServiceBase<string, Results<bool>>;
public class AnalysisService : DiaExecuteServiceBase<AnalysisService, DiaDbContext, string, Results<bool>>, IAnalysisService
{
    public AnalysisService(ILogger<AnalysisService> logger, IDiaSessionContext session, DiaDbContext dbContext) : base(logger, session, dbContext)
    {
    }

    public override Task<Results<bool>> ExecuteAsync(string request, CancellationToken ct = default)
    {
        
    }
}