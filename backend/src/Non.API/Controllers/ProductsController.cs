using Microsoft.AspNetCore.Mvc;
using Non.Application.DTOs.Products.Request;
using Non.Application.Interfaces.Services;

namespace Non.API.Controllers;

[Route("api/[controller]")]
public class ProductsController : ApiControllerBase
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }

    [HttpGet]
    public async Task<IActionResult> GetActive(CancellationToken ct)
    {
        return ToActionResult(await _productService.GetActiveAsync(ct));
    }

    [HttpGet("manage")]
    public async Task<IActionResult> GetPaged([FromQuery] ProductQueryRequest request, CancellationToken ct)
    {
        return ToActionResult(await _productService.GetPagedAsync(request, ct));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        return ToActionResult(await _productService.GetByIdAsync(id, ct));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProductRequest request, CancellationToken ct)
    {
        var result = await _productService.CreateAsync(request, ct);

        if (result.IsFailure)
            return ToErrorResult(result);

        return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProductRequest request, CancellationToken ct)
    {
        return ToActionResult(await _productService.UpdateAsync(id, request, ct));
    }
}