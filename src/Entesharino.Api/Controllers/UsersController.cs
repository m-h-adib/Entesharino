using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Entesharino.Api.Authorization;
using Entesharino.Application.Common.Interfaces;
using Entesharino.Application.Features.Users.Models;
using Entesharino.Domain.Constants;

namespace Entesharino.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public sealed class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    [HasPermission(PermissionCodes.UsersView)]
    public async Task<IActionResult> GetList(
        [FromQuery] UserListRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _userService.GetListAsync(request, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("{id:long}")]
    [HasPermission(PermissionCodes.UsersView)]
    public async Task<IActionResult> GetById(
        long id,
        CancellationToken cancellationToken)
    {
        var result = await _userService.GetByIdAsync(id, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost]
    [HasPermission(PermissionCodes.UsersManage)]
    public async Task<IActionResult> Create(
        CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _userService.CreateAsync(request, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("{id:long}")]
    [HasPermission(PermissionCodes.UsersManage)]
    public async Task<IActionResult> Update(
        long id,
        UpdateUserRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _userService.UpdateAsync(id, request, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPatch("{id:long}/active")]
    [HasPermission(PermissionCodes.UsersManage)]
    public async Task<IActionResult> SetActive(
        long id,
        SetUserActiveRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _userService.SetActiveAsync(id, request.IsActive, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("{id:long}/role")]
    [HasPermission(PermissionCodes.UsersManage)]
    public async Task<IActionResult> AssignRole(
        long id,
        AssignRoleRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _userService.AssignRoleAsync(id, request.RoleId, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }
}
