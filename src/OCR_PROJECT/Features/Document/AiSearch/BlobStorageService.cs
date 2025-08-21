using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Document.Intelligence.Agent.Entities;
using Document.Intelligence.Agent.Features.Document.Models;
using Document.Intelligence.Agent.Infrastructure.Session;
using eXtensionSharp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Document.Intelligence.Agent.Features.Document.AiSearch;

/// <summary>
/// blob 파일 관리 서비스
/// </summary>
public class BlobStorageService : DiaServiceBase<BlobStorageService, DiaDbContext>, IBlobStorageService
{
    private readonly IConfiguration _configuration;
    private readonly BlobServiceClient _client;
    private readonly BlobContainerClient _container;

    public BlobStorageService(ILogger<BlobStorageService> logger, IDiaSessionContext session, DiaDbContext dbContext,
        IConfiguration configuration) : base(logger, session, dbContext)
    {
        _client = new BlobServiceClient(configuration["OCR:AZURE_BLOB_STORAGE_CONNECTION"].xValue<string>());
        _container = _client.GetBlobContainerClient(configuration["OCR:AZURE_BLOB_STORAGE_CONTAINER"].xValue<string>());
        
        // Blob Container가 생성되어 있다는 전제로 함.
        //_blobContainerClient.CreateIfNotExists(PublicAccessType.None);
    }

    /// <summary>
    /// 파일 업로드
    /// </summary>
    /// <param name="binary"></param>
    /// <param name="request"></param>
    public async Task UploadFileAsync(BinaryData binary, BlobStorageRequest request)
    {
        var blob = _container.GetBlobClient($"{request.UploadPath}");
        var options = new BlobUploadOptions();
        if(request.ContentType.xIsNotEmpty())
            options.HttpHeaders = new BlobHttpHeaders { ContentType = request.ContentType };

        await blob.UploadAsync(binary, options);
    }

    public async Task DeleteAsync(string uploadedPath)
    {
        var blob = _container.GetBlobClient(uploadedPath);
        await blob.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots);
    }
    
    public async Task<Stream> DownloadStreamAsync(string uploadedPath)
    {
        var blob = _container.GetBlobClient(uploadedPath);
        var ms = new MemoryStream();
        await blob.DownloadToAsync(ms);
        ms.Position = 0;
        return ms;
    }

    /// <summary>
    /// 프리사이트 URL 조회 (1시간, 읽기 전용)
    /// </summary>
    /// <param name="uploadedPath"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public Uri GenerateBlobSasUri(string uploadedPath)
    {
        var blob = _container.GetBlobClient(uploadedPath);
        
        if (!_client.CanGenerateAccountSasUri)
            throw new Exception("Client does not have credentials to generate SAS.");

        var sasBuilder = new BlobSasBuilder()
        {
            BlobContainerName = _container.Name,
            BlobName = uploadedPath,
            Resource = "b", // b is blob
            StartsOn = DateTimeOffset.UtcNow,
            ExpiresOn = DateTimeOffset.UtcNow.AddHours(1)
        };
        
        sasBuilder.SetPermissions(BlobAccountSasPermissions.Read);

        return blob.GenerateSasUri(sasBuilder);
    }
}