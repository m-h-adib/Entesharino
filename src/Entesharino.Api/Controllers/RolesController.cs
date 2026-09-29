using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Entesharino.Api.Authorization;
using Entesharino.Application.Common.Interfaces;
using Entesharino.Application.Features.Roles.Models;
using Entesharino.Domain.Constants;

namespace Entesharino.Api.Controllers;

[ApiController]
[Route("api/roles")]
[Authorize]
public sealed class RolesController : ControllerBase
{
    private readonly IRoleService _roleService;

    public RolesController(IRoleService roleService) => _roleService = roleService;

    [HttpGet]
    [HasPermission(PermissionCodes.RolesView)]
    public async Task<IActionResult> GetList(CancellationToken cancellationToken)
    {
        var result = await _roleService.GetListAsync(cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("{id:long}")]
    [HasPermission(PermissionCodes.RolesView)]
    public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
    {
        var result = await _roleService.GetByIdAsync(id, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("permissions")]
    [HasPermission(PermissionCodes.RolesView)]
    public async Task<IActionResult> GetPermissions(CancellationToken cancellationToken)
    {
        var result = await _roleService.GetPermissionsAsync(cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost]
    [HasPermission(PermissionCodes.RolesManage)]
    public async Task<IActionResult> Create(CreateRoleRequest request, CancellationToken cancellationToken)
    {
        var result = await _roleService.CreateAsync(request, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("{id:long}")]
    [HasPermission(PermissionCodes.RolesManage)]
    public async Task<IActionResult> Update(long id, UpdateRoleRequest request, CancellationToken cancellationToken)
    {
        var result = await _roleService.UpdateAsync(id, request, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }
 
    [HttpPatch("{id:long}/active")]
    [HasPermission(PermissionCodes.RolesManage)]
    public async Task<IActionResult> SetActive(
        long id,
        SetRoleActiveRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _roleService.SetActiveAsync(id, request.IsActive, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpDelete("{id:long}")]
    [HasPermission(PermissionCodes.RolesManage)]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
    {
        var result = await _roleService.DeleteAsync(id, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("{id:long}/permissions")]
    [HasPermission(PermissionCodes.RolesManage)]
    public async Task<IActionResult> SetPermissions(long id, SetRolePermissionsRequest request, CancellationToken cancellationToken)
    {
        var result = await _roleService.SetPermissionsAsync(id, request, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }
}