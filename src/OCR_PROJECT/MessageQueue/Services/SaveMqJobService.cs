using Document.Intelligence.Agent.Entities;
using Document.Intelligence.Agent.Features.Doc;
using Document.Intelligence.Agent.Features.Topic.Models;
using Document.Intelligence.Agent.Infrastructure.Session;
using eXtensionSharp;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Document.Intelligence.Agent.MessageQueue.Services;

public interface ISaveMqJobService : IDiaExecuteServiceBase<TopicMetadataProcessItem, bool>;

public class SaveMqJobService: DiaExecuteServiceBase<SaveMqJobService, DiaDbContext, TopicMetadataProcessItem, bool>, ISaveMqJobService
{
    private readonly IDocumentAnalysisService _documentAnalysisService;

    public SaveMqJobService(ILogger<SaveMqJobService> logger, IDiaSessionContext session, DiaDbContext dbContext, 
        IDocumentAnalysisService documentAnalysisService) : base(logger, session, dbContext)
    {
        _documentAnalysisService = documentAnalysisService;
    }

    public override async Task<bool> ExecuteAsync(TopicMetadataProcessItem request, CancellationToken ct = default)
    {
        //TODO: WRITE LOGIC
        // 0. 파일의 기록이 변경되지 않았을 경우(ModifyDate)는 갱신하지 않는다. - 갱신 처리에 대한 부분 논리가 있어야 함.
        // 1. 파일 다운로드
        var file = $"{DateTime.Now:yyyyMMddHHmmss}_{request.Path.xGetFileName()}";
        // 2. drm 해제
        // 3. 문자 추출
        // 4. indexing 형태에 따라 가공
        var analysisResult = await _documentAnalysisService.ExecuteAsync(file, ct);
        // 5. index 업로드
        var objs = analysisResult.Select(m => new
        {
            chunk_id = m.ChunkId,
            doc_id = m.DocId,
            source_file_type = m.SourceFileType,
            source_file_path = m.SourceFilePath,
            source_file_name = m.SourceFileName,
            page = m.Page,
            content = m.Content,
            content_vector = m.ContentVector,
        });
        // 6. DB에 index id 기록
        
        // 전체 파일 조회부터 시작
        
        // var exists = await dbContext.TopicJobs.FirstAsync(m => m.Id == request.JobId && m.TopicId == request.TopicId, cancellationToken: ct);
        // exists.ProcessedFiles += 1;
        // dbContext.TopicJobs.Update(exists);
        // await dbContext.SaveChangesAsync(ct);

        await Task.Delay(100, ct);
        return true;
    }
}