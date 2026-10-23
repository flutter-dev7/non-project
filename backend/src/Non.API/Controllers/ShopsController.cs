using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Non.Application.DTOs.Shops.Request;
using Non.Application.Interfaces.Services;
using Non.Domain.Constants;

namespace Non.API.Controllers;

[Route("api/[controller]")]
[Authorize]
public class ShopsController : ApiControllerBase
{
    private readonly IShopService _shopService;

    public ShopsController(IShopService shopService)
    {
        _shopService = shopService;
    }

    [HttpGet]
    [Authorize(Roles = RoleNames.AdminOrAgent)]
    public async Task<IActionResult> GetPaged([FromQuery] ShopQueryRequest request, CancellationToken ct)
    {
        return ToActionResult(await _shopService.GetPagedAsync(request, ct));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = RoleNames.AdminOrAgent)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        return ToActionResult(await _shopService.GetByIdAsync(id, ct));
    }

    [HttpPost]
    [Authorize(Roles = RoleNames.AdminOrAgent)]
    public async Task<IActionResult> Create([FromBody] CreateShopRequest request, CancellationToken ct)
    {
        var result = await _shopService.CreateAsync(request, ct);

        if (result.IsFailure)
            return ToErrorResult(result);

        return CreatedAtAction(nameof(GetMy), result);
        
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = RoleNames.AdminOrAgent)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateShopRequest request, CancellationToken ct)
    {
        return ToActionResult(await _shopService.UpdateAsync(id, request, ct));
    }

    [HttpPut("{id:guid}/owner")]
    [Authorize(Roles = RoleNames.AdminOrAgent)]
    public async Task<IActionResult> SetOwner(Guid id, [FromBody] SetOwnerRequest request, CancellationToken ct)
    {
        return ToActionResult(await _shopService.SetOwnerAsync(id, request, ct));
    }

    [HttpPut("{id:guid}/debt-limit")]
    [Authorize(Roles = RoleNames.Admin)]
    public async Task<IActionResult> SetDebtLimit(Guid id, [FromBody] SetDebtLimitRequest request, CancellationToken ct)
    {
        return ToActionResult(await _shopService.SetDebtLimitAsync(id, request, ct));
    }

    [HttpGet("my")]
    [Authorize(Roles = RoleNames.Shop)]
    public async Task<IActionResult> GetMy(CancellationToken ct)
    {
        return ToActionResult(await _shopService.GetMyAsync(CurrentUserId, ct));
    }

    [HttpPost("my")]
    [Authorize(Roles = RoleNames.Shop)]
    public async Task<IActionResult> CreateMy([FromBody] CreateShopRequest request, CancellationToken ct)
    {
        var result = await _shopService.CreateMyAsync(CurrentUserId, request, ct);

        if (result.IsFailure)
            return ToErrorResult(result);

        return CreatedAtAction(nameof(GetMy), result);
    }
}