using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Non.Application.DTOs.Payments.Request;
using Non.Application.Interfaces.Services;
using Non.Domain.Constants;

namespace Non.API.Controllers;

[Route("api/[controller]")]
[Authorize]
public class PaymentsController : ApiControllerBase
{
    private readonly IPaymentService _paymentService;

    public PaymentsController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    [HttpGet]
    [Authorize(Roles = RoleNames.AdminOrShop)]
    public async Task<IActionResult> GetPaged([FromQuery] PaymentQueryRequest request, CancellationToken ct)
    {
        return ToActionResult(await _paymentService.GetPagedAsync(request, Caller, ct));
    }

    [HttpPost]
    [Authorize(Roles = RoleNames.AdminOrCourier)]
    public async Task<IActionResult> Create([FromBody] CreatePaymentRequest request, CancellationToken ct)
    {
        return ToActionResult(await _paymentService.CreateAsync(request, Caller, ct));
    }

    [HttpPost("{id:guid}/reverse")]
    [Authorize(Roles = RoleNames.Admin)]
    public async Task<IActionResult> Reverse(Guid id, [FromBody] ReversePaymentRequest request, CancellationToken ct)
    {
        return ToActionResult(await _paymentService.ReverseAsync(id, request, Caller, ct));
    }
}