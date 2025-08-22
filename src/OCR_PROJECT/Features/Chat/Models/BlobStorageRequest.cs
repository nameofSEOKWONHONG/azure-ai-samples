namespace Document.Intelligence.Agent.Features.Chat.Models;

public class BlobStorageRequest
{
    /// <summary>
    /// DB 키
    /// </summary>
    public string Id { get; set; }
    /// <summary>
    /// 파일명 (파일명만)
    /// </summary>
    public string FileName { get; set; }
    /// <summary>
    /// 확장자
    /// </summary>
    public string Extension { get; set; }
    /// <summary>
    /// AZURE BLOB에 업로드된 전체 PATH
    /// </summary>
    public string UploadPath { get; set; }
    /// <summary>
    /// content type
    /// </summary>
    public string ContentType { get; set; }
    /// <summary>
    /// 크기
    /// </summary>
    public long Size { get; set; }
}