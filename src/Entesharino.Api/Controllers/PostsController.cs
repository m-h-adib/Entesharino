using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Entesharino.Api.Authorization;
using Entesharino.Application.Common.Interfaces;
using Entesharino.Application.Features.Posts.Models;
using Entesharino.Domain.Constants;

namespace Entesharino.Api.Controllers;

[ApiController]
[Route("api/posts")]
[Authorize]
public sealed class PostsController : ControllerBase
{
    private readonly IPostService _postService;

    public PostsController(IPostService postService)
    {
        _postService = postService;
    }

    [HttpGet]
    [HasPermission(PermissionCodes.PostsView)]
    public async Task<IActionResult> GetList(
        [FromQuery] PostListRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _postService.GetListAsync(request, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("{id:long}")]
    [HasPermission(PermissionCodes.PostsView)]
    public async Task<IActionResult> GetById(
        long id,
        CancellationToken cancellationToken)
    {
        var result = await _postService.GetByIdAsync(id, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost]
    [HasPermission(PermissionCodes.PostsCreate)]
    public async Task<IActionResult> Create(
        CreatePostRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _postService.CreateAsync(request, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("{id:long}")]
    [HasPermission(PermissionCodes.PostsManage)]
    public async Task<IActionResult> Update(
        long id,
        UpdatePostRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _postService.UpdateAsync(id, request, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("{id:long}/media")]
    [HasPermission(PermissionCodes.PostsManage)]
    [RequestSizeLimit(50L * 1024 * 1024)]
    public async Task<IActionResult> UploadMedia(
        long id,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        var result = await _postService.UploadMediaAsync(id, file, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpDelete("{id:long}/media/{mediaId:long}")]
    [HasPermission(PermissionCodes.PostsManage)]
    public async Task<IActionResult> DeleteMedia(
        long id,
        long mediaId,
        CancellationToken cancellationToken)
    {
        var result = await _postService.DeleteMediaAsync(id, mediaId, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpDelete("{id:long}")]
    [HasPermission(PermissionCodes.PostsManage)]
    public async Task<IActionResult> Delete(
        long id,
        CancellationToken cancellationToken)
    {
        var result = await _postService.DeleteAsync(id, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("{id:long}/publish")]
    [HasPermission(PermissionCodes.PostsManage)]
    public async Task<IActionResult> Publish(
        long id,
        CancellationToken cancellationToken)
    {
        var result = await _postService.PublishAsync(id, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("{id:long}/schedule")]
    [HasPermission(PermissionCodes.PostsManage)]
    public async Task<IActionResult> Schedule(
        long id,
        SchedulePostRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _postService.ScheduleAsync(id, request, cancellationToken);
        return StatusCode(result.StatusCode, result);
    }
}
