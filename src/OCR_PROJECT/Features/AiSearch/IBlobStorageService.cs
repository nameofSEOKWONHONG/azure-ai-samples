using Document.Intelligence.Agent.Features.Chat.Models;

namespace Document.Intelligence.Agent.Features.AiSearch;

public interface IBlobStorageService
{
    Task UploadFileAsync(BinaryData binary, BlobStorageRequest request);
    Task DeleteAsync(string uploadedPath);

    Task<Stream> DownloadStreamAsync(string uploadedPath);
    Uri GenerateBlobSasUri(string uploadedPath);
}