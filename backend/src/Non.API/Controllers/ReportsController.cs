using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Non.Application.DTOs.Reports.Request;
using Non.Application.Interfaces.Services;
using Non.Domain.Constants;

namespace Non.API.Controllers;

[Route("api/[controller]")]
[Authorize(Roles = RoleNames.Admin)]
public class ReportsController : ApiControllerBase
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService)
    {
        _reportService = reportService;
    }

    [HttpGet("production")]
    public async Task<IActionResult> GetProduction([FromQuery] ProductionReportRequest request, CancellationToken ct)
    {
        return ToActionResult(await _reportService.GetProductionAsync(request, ct));
    }

    [HttpGet("sales")]
    public async Task<IActionResult> GetSales([FromQuery] SalesReportRequest request, CancellationToken ct)
    {
        return ToActionResult(await _reportService.GetSalesAsync(request, ct));
    }
}