using Document.Intelligence.Agent.Features.Document.Models;

namespace Document.Intelligence.Agent.Features.Document.AiSearch;

public interface IBlobStorageService
{
    Task UploadFileAsync(BinaryData binary, BlobStorageRequest request);
    Task DeleteAsync(string uploadedPath);

    Task<Stream> DownloadStreamAsync(string uploadedPath);
    Uri GenerateBlobSasUri(string uploadedPath);
}