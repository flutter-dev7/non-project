using Non.Application.Common;
using Non.Application.Interfaces.Services;

namespace Non.Infrastructure.Services;

public class LocalImageStorage : IImageStorage
{
    private const long MaxBytes = 5 * 1024 * 1024;

    private static readonly Dictionary<string, string> Extensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = ".jpg",
        ["image/png"] = ".png",
        ["image/webp"] = ".webp"
    };

    private readonly string _rootPath;

    public LocalImageStorage(string rootPath)
    {
        _rootPath = rootPath;
    }

    public async Task<Result<string>> SaveProductImageAsync(
        Stream content, string contentType, long length, CancellationToken ct = default)
    {
        if (length <= 0)
            return Result<string>.Failure("Файл пустой.");

        if (length > MaxBytes)
            return Result<string>.Failure("Размер фото не должен превышать 5 МБ.");

        if (!Extensions.TryGetValue(contentType, out var extension))
            return Result<string>.Failure("Допустимые форматы: JPG, PNG, WEBP.");

        var folder = Path.Combine(_rootPath, "products");
        Directory.CreateDirectory(folder);

        var fileName = $"{Guid.NewGuid():N}{extension}";

        await using var file = File.Create(Path.Combine(folder, fileName));
        await content.CopyToAsync(file, ct);

        return Result<string>.Success($"/uploads/products/{fileName}");
    }
}