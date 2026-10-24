using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Non.Application.DTOs.Settings.Request;
using Non.Application.Interfaces.Services;
using Non.Domain.Constants;

namespace Non.API.Controllers;

[Route("api/[controller]")]
[Authorize(Roles = RoleNames.Admin)]
public class SettingsController : ApiControllerBase
{
    private readonly ISettingService _settingService;

    public SettingsController(ISettingService settingService)
    {
        _settingService = settingService;
    }

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        return ToActionResult(await _settingService.GetAsync(ct));
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdateSettingsRequest request, CancellationToken ct)
    {
        return ToActionResult(await _settingService.UpdateAsync(request, ct));
    }
}