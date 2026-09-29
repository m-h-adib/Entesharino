using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Entesharino.Api.Authorization;
using Entesharino.Application.Common.Interfaces;
using Entesharino.Application.Features.Channels.Models;
using Entesharino.Domain.Constants;

namespace Entesharino.Api.Controllers;

[ApiController]
[Route("api/channels")]
[Authorize]
public sealed class ChannelsController : ControllerBase
{
    private readonly IChannelService _channelService;

    public ChannelsController(IChannelService channelService)
    {
        _channelService = channelService;
    }

    [HttpGet]
    [HasPermission(PermissionCodes.ChannelsView)]
    public async Task<IActionResult> GetList(
        [FromQuery] ChannelListRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _channelService.GetListAsync(request, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("{id:long}")]
    [HasPermission(PermissionCodes.ChannelsView)]
    public async Task<IActionResult> GetById(
        long id,
        CancellationToken cancellationToken)
    {
        var result = await _channelService.GetByIdAsync(id, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost]
    [HasPermission(PermissionCodes.ChannelsManage)]
    public async Task<IActionResult> Create(
        CreateChannelRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _channelService.CreateAsync(request, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("{id:long}")]
    [HasPermission(PermissionCodes.ChannelsManage)]
    public async Task<IActionResult> Update(
        long id,
        UpdateChannelRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _channelService.UpdateAsync(id, request, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("{id:long}/connection")]
    [HasPermission(PermissionCodes.ChannelsManage)]
    public async Task<IActionResult> UpdateConnection(
        long id,
        UpdateChannelConnectionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _channelService.UpdateConnectionAsync(id, request, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPatch("{id:long}/active")]
    [HasPermission(PermissionCodes.ChannelsManage)]
    public async Task<IActionResult> SetActive(
        long id,
        [FromQuery] bool isActive,
        CancellationToken cancellationToken)
    {
        var result = await _channelService.SetActiveAsync(id, isActive, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("{id:long}/test-connection")]
    [HasPermission(PermissionCodes.ChannelsManage)]
    public async Task<IActionResult> TestConnection(
        long id,
        CancellationToken cancellationToken)
    {
        var result = await _channelService.TestConnectionAsync(id, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpDelete("{id:long}")]
    [HasPermission(PermissionCodes.ChannelsManage)]
    public async Task<IActionResult> Delete(
        long id,
        CancellationToken cancellationToken)
    {
        var result = await _channelService.DeleteAsync(id, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }
}
