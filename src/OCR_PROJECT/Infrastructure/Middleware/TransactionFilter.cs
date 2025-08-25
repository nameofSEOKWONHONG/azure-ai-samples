using Document.Intelligence.Agent.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Document.Intelligence.Agent.Infrastructure.Middleware;

public class TransactionFilter : IEndpointFilter
{
    public async ValueTask<object> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var db = context.HttpContext.RequestServices.GetRequiredService<DiaDbContext>();
        await using var tx = await db.Database.BeginTransactionAsync();

        try
        {
            // 엔드포인트 실행
            var result = await next(context);
            
            await tx.CommitAsync();
            return result;
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }
}