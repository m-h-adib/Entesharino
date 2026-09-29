namespace Entesharino.Application.Common.Interfaces;

public interface IMediaStorage
{
    Task<MediaStorageResult> SaveAsync(
        Stream content,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        string fileUrl,
        CancellationToken cancellationToken = default);
}

public sealed record MediaStorageResult(
    string FileName,
    string FileUrl,
    long FileSize,
    string ContentType);
