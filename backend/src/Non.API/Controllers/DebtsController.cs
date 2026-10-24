using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Non.Application.DTOs.Debts.Request;
using Non.Application.Interfaces.Services;
using Non.Domain.Constants;

namespace Non.API.Controllers;

[Route("api/[controller]")]
[Authorize(Roles = RoleNames.Admin)]
public class DebtsController : ApiControllerBase
{
    private readonly IDebtService _debtService;

    public DebtsController(IDebtService debtService)
    {
        _debtService = debtService;
    }

    [HttpGet]
    public async Task<IActionResult> GetDebts([FromQuery] DebtQueryRequest request, CancellationToken ct)
    {
        return ToActionResult(await _debtService.GetDebtsAsync(request, ct));
    }
}