using Document.Intelligence.Agent.Entities;
using Document.Intelligence.Agent.Features.Topic.Models;
using Document.Intelligence.Agent.Infrastructure.Session;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Document.Intelligence.Agent.MessageQueue.Services;

public interface ISaveMqJobService : IDiaExecuteServiceBase<TopicMetadataProcessItem, bool>;

public class SaveMqJobService: DiaExecuteServiceBase<SaveMqJobService, DiaDbContext, TopicMetadataProcessItem, bool>, ISaveMqJobService
{
    public SaveMqJobService(ILogger<SaveMqJobService> logger, IDiaSessionContext session, DiaDbContext dbContext) : base(logger, session, dbContext)
    {
    }

    public override async Task<bool> ExecuteAsync(TopicMetadataProcessItem request, CancellationToken ct = default)
    {
        //TODO: WRITE LOGIC
        // 0. 파일의 기록이 변경되지 않았을 경우(ModifyDate)는 갱신하지 않는다. - 갱신 처리에 대한 부분 논리가 있어야 함.
        // 1. 파일 다운로드
        // 2. drm 해제
        // 3. 문자 추출
        // 4. indexing 형태에 따라 가공
        // 5. index 업로드
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