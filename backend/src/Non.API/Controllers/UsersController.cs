using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Non.Application.DTOs.Users.Request;
using Non.Application.Interfaces.Services;
using Non.Domain.Constants;

namespace Non.API.Controllers;

[Route("api/[controller]")]
[Authorize(Roles = RoleNames.Admin)]
public class UsersController : ApiControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    public async Task<IActionResult> GetPaged([FromQuery] UserQueryRequest request, CancellationToken ct)
    {
        return ToActionResult(await _userService.GetPagedAsync(request, ct));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        return ToActionResult(await _userService.GetByIdAsync(id, ct));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request, CancellationToken ct)
    {
        var result = await _userService.CreateAsync(request, ct);

        if (result.IsFailure)
            return ToErrorResult(result);

        return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result);
        
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUserRequest request, CancellationToken ct)
    {
        return ToActionResult(await _userService.UpdateAsync(id, request, CurrentUserId, ct));
    }
}