using Entesharino.Application.Common.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace Entesharino.Infrastructure.Storage;

public sealed class LocalMediaStorage : IMediaStorage
{
    private readonly IWebHostEnvironment _environment;
    private readonly IConfiguration _configuration;

    public LocalMediaStorage(
        IWebHostEnvironment environment,
        IConfiguration configuration)
    {
        _environment = environment;
        _configuration = configuration;
    }

    public async Task<MediaStorageResult> SaveAsync(
        Stream content,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        if (content is null)
            throw new ArgumentNullException(nameof(content));

        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("نام فایل الزامی است.", nameof(fileName));

        var root = _configuration["Storage:MediaRoot"];

        var physicalRoot = string.IsNullOrWhiteSpace(root)
            ? Path.Combine(_environment.WebRootPath ?? _environment.ContentRootPath, "uploads", "media")
            : Path.IsPathRooted(root)
                ? root
                : Path.Combine(_environment.ContentRootPath, root);

        var now = DateTime.UtcNow;
        var relativeDirectory = Path.Combine(
            now.ToString("yyyy"),
            now.ToString("MM"),
            now.ToString("dd"));

        var directory = Path.Combine(physicalRoot, relativeDirectory);
        Directory.CreateDirectory(directory);

        var extension = Path.GetExtension(fileName);
        var storedFileName = $"{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(directory, storedFileName);

        await using (var output = new FileStream(
            fullPath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 64 * 1024,
            useAsync: true))
        {
            await content.CopyToAsync(output, cancellationToken);
        }

        var publicRoot = _configuration["Storage:MediaPublicPath"]?.Trim('/');
        if (string.IsNullOrWhiteSpace(publicRoot))
            publicRoot = "uploads/media";

        var fileUrl = $"/{publicRoot}/{now:yyyy}/{now:MM}/{now:dd}/{storedFileName}";

        return new MediaStorageResult(
            Path.GetFileName(fileName),
            fileUrl,
            new FileInfo(fullPath).Length,
            contentType);
    }

    public Task<Stream> OpenReadAsync(
        string fileUrl,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(fileUrl))
            throw new ArgumentException("آدرس فایل الزامی است.", nameof(fileUrl));

        var publicRoot = _configuration["Storage:MediaPublicPath"]?.Trim('/');
        if (string.IsNullOrWhiteSpace(publicRoot))
            publicRoot = "uploads/media";

        var prefix = "/" + publicRoot + "/";
        if (!fileUrl.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("آدرس فایل رسانه معتبر نیست.");

        var relativePath = fileUrl[prefix.Length..]
            .Replace('/', Path.DirectorySeparatorChar);

        var root = _configuration["Storage:MediaRoot"];

        var physicalRoot = string.IsNullOrWhiteSpace(root)
            ? Path.Combine(_environment.WebRootPath ?? _environment.ContentRootPath, "uploads", "media")
            : Path.IsPathRooted(root)
                ? root
                : Path.Combine(_environment.ContentRootPath, root);

        var fullPath = Path.GetFullPath(Path.Combine(physicalRoot, relativePath));

        var normalizedRoot = Path.GetFullPath(physicalRoot)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;

        if (!fullPath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("مسیر فایل رسانه معتبر نیست.");

        if (!File.Exists(fullPath))
            throw new FileNotFoundException("فایل رسانه پیدا نشد.", fullPath);

        Stream stream = new FileStream(
            fullPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 64 * 1024,
            useAsync: true);

        return Task.FromResult(stream);
    }

    public Task DeleteAsync(
        string fileUrl,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(fileUrl))
            return Task.CompletedTask;

        var publicRoot = _configuration["Storage:MediaPublicPath"]?.Trim('/');
        if (string.IsNullOrWhiteSpace(publicRoot))
            publicRoot = "uploads/media";

        var prefix = "/" + publicRoot + "/";
        if (!fileUrl.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return Task.CompletedTask;

        var relativePath = fileUrl[prefix.Length..]
            .Replace('/', Path.DirectorySeparatorChar);

        var root = _configuration["Storage:MediaRoot"];

        var physicalRoot = string.IsNullOrWhiteSpace(root)
            ? Path.Combine(_environment.WebRootPath ?? _environment.ContentRootPath, "uploads", "media")
            : Path.IsPathRooted(root)
                ? root
                : Path.Combine(_environment.ContentRootPath, root);

        var fullPath = Path.GetFullPath(Path.Combine(physicalRoot, relativePath));

        var normalizedRoot = Path.GetFullPath(physicalRoot)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;

        if (!fullPath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase))
            return Task.CompletedTask;

        if (File.Exists(fullPath))
            File.Delete(fullPath);

        return Task.CompletedTask;
    }
}
