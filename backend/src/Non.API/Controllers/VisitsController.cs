using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Non.Application.DTOs.Visits.Request;
using Non.Application.Interfaces.Services;
using Non.Domain.Constants;

namespace Non.API.Controllers;

[Route("api/[controller]")]
[Authorize(Roles = RoleNames.AdminOrAgent)]
public class VisitsController : ApiControllerBase
{
    private readonly IVisitService _visitService;

    public VisitsController(IVisitService visitService)
    {
        _visitService = visitService;
    }

    [HttpGet]
    public async Task<IActionResult> GetPaged([FromQuery] VisitQueryRequest request, CancellationToken ct)
    {
        return ToActionResult(await _visitService.GetPagedAsync(request, Caller, ct));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        return ToActionResult(await _visitService.GetByIdAsync(id, Caller, ct));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateVisitRequest request, CancellationToken ct)
    {
        return ToActionResult(await _visitService.CreateAsync(request, Caller, ct));
    }
}