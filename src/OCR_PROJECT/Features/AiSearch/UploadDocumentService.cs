using Azure.AI.DocumentIntelligence;
using Document.Intelligence.Agent.Entities;
using Document.Intelligence.Agent.Infrastructure.Data;
using Document.Intelligence.Agent.Infrastructure.Session;
using Microsoft.Extensions.Logging;

namespace Document.Intelligence.Agent.Features.AiSearch;

public interface IUploadDocumentService : IDiaExecuteServiceBase<string, bool>;

/// <summary>
/// 첨부 파일 또는 blob storage의 파일을 AI SEARCH INDEX로 업로드 함. 로컬 파일일 경우 blob storage에 업로드 후 AI SEARCH INDEX 업로드를 진행. 유사 파일을 링크로 열 수 있도록 파일은 모두 blob으로 업로드 한다.
/// </summary>
public class UploadDocumentService: DiaExecuteServiceBase<UploadDocumentService, DiaDbContext, string, bool>, IUploadDocumentService
{
    private readonly DocumentIntelligenceClient _documentIntelligenceClient;

    public UploadDocumentService(ILogger<UploadDocumentService> logger, IDiaSessionContext session, DiaDbContext dbContext,
        DocumentIntelligenceClient documentIntelligenceClient) : base(logger, session, dbContext)
        
    {
        _documentIntelligenceClient = documentIntelligenceClient;
    }

    public override async Task<Results<bool>> ExecuteAsync(string request)
    {
        var isLocal = Uri.TryCreate(request, UriKind.RelativeOrAbsolute, out Uri address);
        if (isLocal)
        {
            //로컬 파일 업로드
            var uri = await UploadLocal(request);
            address = new Uri(uri);
        }
        
        //blob 파일 다운로드
        
        //문자 추출
        
        //index upload

        return await Results<bool>.SuccessAsync(true);
    }

    /// <summary>
    /// 로컬 파일을 업로드하고 blob 경로를 반환.
    /// TODO: 이미 파일이 있는지 확인하거나 업데이트를 할지는 확인 필요함.
    /// </summary>
    /// <param name="localPath"></param>
    /// <returns></returns>
    private async Task<string> UploadLocal(string localPath)
    {
        await Task.Delay(1);
        return string.Empty;
    }
}