using Document.Intelligence.Agent.Features.AiSearch;
using Document.Intelligence.Agent.Features.Chat.Models;
using Document.Intelligence.Agent.Features.Drm.M365;
using Document.Intelligence.Agent.Infrastructure.Session;
using eXtensionSharp;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Document.Intelligence.Agent.Features.Receipt;

public interface IReceiptService
{
    Task<string> SaveReceiptAnalysisAsync(IFormFile file);
    Task DeleteReceiptAnalysisAsync(string id);
}

public class ReceiptService : ServiceBase<ReceiptService>, IReceiptService
{
    private readonly IReceiptExtractService _receiptExtractService;
    private readonly IReceiptAiSearchService _receiptAiSearchService;
    private readonly IBlobStorageService _blobStorageService;
    private readonly IDrmHandler _drmHandler;

    public ReceiptService(ILogger<ReceiptService> logger, IDiaSessionContext sessionContext,
        IReceiptExtractService receiptExtractService,
        IReceiptAiSearchService receiptAiSearchService,
        IBlobStorageService blobStorageService,
        IDrmHandler drmHandler) : base(logger, sessionContext)
    {
        _receiptExtractService = receiptExtractService;
        _receiptAiSearchService = receiptAiSearchService;
        _blobStorageService = blobStorageService;
        _drmHandler = drmHandler;
    }

    /// <summary>
    /// 영수증 추출 및 blob 업로드, db 기록
    /// </summary>
    /// <returns>파일 정보 PK (GUID:string)</returns>
    public async Task<string> SaveReceiptAnalysisAsync(IFormFile file)
    {
        if (file.xIsEmpty()) throw new ArgumentException("File is empty");

        // 1) 안전한 파일명/경로 생성 (원본 파일명은 로그 등에서만 사용)
        var originalName = Path.GetFileName(file.FileName); // 디렉터리 제거
        var safeName = string.Concat(
            Path.GetFileNameWithoutExtension(originalName)
                .Replace(':','_').Replace('/','_').Replace('\\','_'),
            Path.GetExtension(originalName)
        );

        var tmpIn  = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}_{safeName}");
        var tmpOut = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}_{safeName}");
        
        var id = Guid.NewGuid().ToString();
        var extension = safeName.xGetExtension();
        var contentType = string.IsNullOrWhiteSpace(file.ContentType)
            ? "application/octet-stream"
            : file.ContentType;

        try
        {
            await using (var fs = new FileStream(
                             tmpIn,
                             new FileStreamOptions
                             {
                                 Mode = FileMode.CreateNew,
                                 Access = FileAccess.Write,
                                 Share = FileShare.None,
                                 Options = FileOptions.Asynchronous | FileOptions.SequentialScan,
                                 PreallocationSize = file.Length > 0 ? file.Length : 0
                             }))
            {
                await file.CopyToAsync(fs);
            }

            // 3) DRM 처리 (출력 경로는 미리 준비한 tmpOut 사용)
            var drm = await _drmHandler.ExecuteAsync(tmpIn, tmpOut);
            if (!drm.IsSuccess)
                throw new InvalidOperationException(drm.Message);

            var finalPath = drm.FilePath ?? tmpOut;
            await using var read = new FileStream(
                             finalPath,
                             new FileStreamOptions
                             {
                                 Mode = FileMode.Open,
                                 Access = FileAccess.Read,
                                 Share = FileShare.Read,
                                 Options = FileOptions.Asynchronous | FileOptions.SequentialScan
                             });
            
            var binary = await BinaryData.FromStreamAsync(read);
            read.Position = 0;
            
            // 영수증 정보 추출
            var extract = await _receiptExtractService.ExtractReceiptAsync(binary);
            
            // upload ai search
            var vector = await _receiptAiSearchService.UploadReceiptDocument(extract);
            
            // db 등록
        
            // 원본 리소스(OCR_RECEIPT_DOC)에 해당 하는 DB에 등록
        
            // 파일 DB 등록

            // 등록 키 반환
            
            // 파일 업로드
            var request = new BlobStorageRequest()
            {
                Id = id,
                UploadPath = $"receipts/{safeName}",
                FileName = safeName,
                ContentType = contentType,
                Size = file.Length,
                Extension = extension,
            };
            await _blobStorageService.UploadFileAsync(binary, request);
        }
        finally
        {
            TryDelete(tmpIn);
            TryDelete(tmpOut);
        }

        return id;
    }
    
    private void TryDelete(string path)
    {
        try { if (System.IO.File.Exists(path)) System.IO.File.Delete(path); } catch { /* swallow */ }
    }

    /// <summary>
    /// 영수증 삭제 및 blob, ai search 삭제, db 기록 삭제
    /// </summary>
    /// <param name="id"></param>
    public Task DeleteReceiptAnalysisAsync(string id)
    {
        //db에서 id 찾기
        
        //파일 정보 찾기
        
        //blob 파일 삭제
        
        //ai search index 삭제

        return Task.CompletedTask;
    }
}