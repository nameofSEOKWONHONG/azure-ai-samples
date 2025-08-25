using Document.Intelligence.Agent.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Document.Intelligence.Agent.Infrastructure.Session;

/// <summary>
/// 기본 서비스
/// </summary>
/// <typeparam name="TSelf"></typeparam>
public abstract class ServiceBase<TSelf>
{
    protected readonly ILogger logger;
    protected readonly IDiaSessionContext session;
    protected ServiceBase(ILogger<TSelf> logger, IDiaSessionContext session)
    {
        this.logger = logger;
        this.session = session;
    }
}

/// <summary>
/// 실행자 노출을 위한 인터페이스
/// </summary>
/// <typeparam name="TRequest"></typeparam>
/// <typeparam name="TResult"></typeparam>
public interface IDiaExecuteServiceBase<TRequest, TResult>
{
    Task<TResult> ExecuteAsync(TRequest request, CancellationToken ct = default);
}

/// <summary>
/// DOCUMENT INTELLIGENCE AGENT 서비스
/// </summary>
/// <typeparam name="TSelf"></typeparam>
/// <typeparam name="TDbContext"></typeparam>
public abstract class DiaServiceBase<TSelf, TDbContext> : ServiceBase<TSelf>
    where TDbContext: DbContext
{
    protected readonly TDbContext dbContext;
    protected DiaServiceBase(ILogger<TSelf> logger, IDiaSessionContext session, TDbContext dbContext) : base(
        logger, session)
    {
        this.dbContext = dbContext;
    }
}

/// <summary>
/// 파이프라인을 위한 DOCUMENT INTELLIGENCE AGENT 서비스
/// TODO: 파이프라인을 실제로 구성하지는 않을 예정임.
/// </summary>
/// <typeparam name="TSelf">구현서비스 class</typeparam>
/// <typeparam name="TDbContext">사용할 dbcontext</typeparam>
/// <typeparam name="TRequest">요청</typeparam>
/// <typeparam name="TResult">결과</typeparam>
public abstract class DiaExecuteServiceBase<TSelf, TDbContext, TRequest, TResult> : DiaServiceBase<TSelf, TDbContext>, IDiaExecuteServiceBase<TRequest, TResult>
    where TSelf : class
    where TDbContext : DbContext
{
    protected DiaExecuteServiceBase(ILogger<TSelf> logger, IDiaSessionContext session, TDbContext dbContext) : base(logger, session, dbContext)
    {
    }

    public virtual Task<bool> PreExecuteAsync(TRequest request, CancellationToken ct = default)
    {
        return Task.FromResult(true);
    }
        
    public abstract Task<TResult> ExecuteAsync(TRequest request, CancellationToken ct = default);
}