using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Non.Application.Interfaces.Services;

namespace Non.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class UploadsController : ControllerBase
{
    private readonly IImageStorage _imageStorage;

    public UploadsController(IImageStorage imageStorage)
    {
        _imageStorage = imageStorage;
    }

    [HttpPost("product-image")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> UploadProductImage(IFormFile file, CancellationToken ct)
    {
        await using var stream = file.OpenReadStream();

        var result = await _imageStorage.SaveProductImageAsync(stream, file.ContentType, file.Length, ct);

        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }
}