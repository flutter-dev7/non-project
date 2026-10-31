using Non.Application.Common;

namespace Non.Application.Interfaces.Services;

public interface IImageStorage
{
    Task<Result<string>> SaveProductImageAsync(
        Stream content, string contentType, long length, CancellationToken ct = default);
}