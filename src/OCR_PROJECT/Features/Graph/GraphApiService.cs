using Document.Intelligence.Agent.Entities;
using Document.Intelligence.Agent.Infrastructure.Session;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.Models;

namespace Document.Intelligence.Agent.Features.Graph;

public interface IGraphApiService
{
    Task<IEnumerable<Site>> FindSite();
}

public class GraphApiService: ServiceBase<GraphApiService>, IGraphApiService
{
    private readonly DiaDbContext _dbContext;
    private readonly GraphServiceClient _client;

    public GraphApiService(ILogger<GraphApiService> logger, IDiaSessionContext session,
        DiaDbContext dbContext,
        GraphServiceClient client) : base(logger, session)
    {
        _dbContext = dbContext;
        _client = client;
    }

    public async Task<IEnumerable<Site>> FindSite()
    {
        // var res = await _client.Users.GetAsync(r => {
        //     r.Headers.Add("ConsistencyLevel", "eventual"); // ★ 필수
        //     r.QueryParameters.Count = true;                // $count=true
        //     r.QueryParameters.Top = 1;                     // 페이로드 최소화
        //     // r.QueryParameters.Filter = "...";           // 필터가 있다면 여기에
        // });
        
        var result = await this._client.Sites.GetAsync(m =>
        {
            m.QueryParameters.Search = "gowitco";
            m.QueryParameters.Top = 50;
            m.QueryParameters.Select = new[] { "id","name","displayName","webUrl","siteCollection" };
        });
        var selectedSite = result.Value.First(m => m.DisplayName == "CS팀");
        var drives = await _client.Sites[selectedSite.Id].Drives.GetAsync(r =>
        {
            r.QueryParameters.Select = ["id", "name", "driveType", "webUrl"];
            r.QueryParameters.Top = 50;
        });
        foreach (var d in drives!.Value!.Where(d => 
                     string.Equals(d.DriveType, "documentLibrary", StringComparison.OrdinalIgnoreCase)))
        {
            Console.WriteLine($"[LIB] {d.Name} | {d.WebUrl} | {d.Id}");
        }
        return result.Value;
    }
    
    
}