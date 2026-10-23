using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Non.Application.DTOs.Orders.Request;
using Non.Application.Interfaces.Services;
using Non.Domain.Constants;

namespace Non.API.Controllers;

[Route("api/[controller]")]
[Authorize]
public class OrdersController : ApiControllerBase
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    [HttpGet]
    public async Task<IActionResult> GetPaged([FromQuery] OrderQueryRequest request, CancellationToken ct)
    {
        return ToActionResult(await _orderService.GetPagedAsync(request, Caller, ct));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        return ToActionResult(await _orderService.GetByIdAsync(id, Caller, ct));
    }

    [HttpPost]
    [Authorize(Roles = RoleNames.OrderCreators)]
    public async Task<IActionResult> Create([FromBody] CreateOrderRequest request, CancellationToken ct)
    {
        var result = await _orderService.CreateAsync(request, Caller, ct);

        if (result.IsFailure)
            return ToErrorResult(result);

        return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result);
    }

    [HttpPut("{id:guid}/status")]
    [Authorize(Roles = RoleNames.AdminOrCourier)]
    public async Task<IActionResult> ChangeStatus(
        Guid id, [FromBody] ChangeOrderStatusRequest request, CancellationToken ct)
    {
        return ToActionResult(await _orderService.ChangeStatusAsync(id, request, Caller, ct));
    }

    [HttpPost("{id:guid}/cancel")]
    [Authorize(Roles = RoleNames.OrderCreators)]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
    {
        return ToActionResult(await _orderService.CancelAsync(id, Caller, ct));
    }

    [HttpPut("{id:guid}/courier")]
    [Authorize(Roles = RoleNames.Admin)]
    public async Task<IActionResult> AssignCourier(
        Guid id, [FromBody] AssignCourierRequest request, CancellationToken ct)
    {
        return ToActionResult(await _orderService.AssignCourierAsync(id, request, ct));
    } 
}