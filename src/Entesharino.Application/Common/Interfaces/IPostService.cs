using Entesharino.Application.Common.Models;
using Entesharino.Application.Features.Posts.Models;
using Microsoft.AspNetCore.Http;

namespace Entesharino.Application.Common.Interfaces;

public interface IPostService
{
    Task<ResultOfList<PostListItemDto>> GetListAsync(
        PostListRequest request,
        CancellationToken cancellationToken = default);

    Task<ResultDto<PostDetailsDto>> GetByIdAsync(
        long id,
        CancellationToken cancellationToken = default);

    Task<ResultDto<PostMediaDeliveryReportDto>> GetMediaDeliveryReportAsync(
        long id,
        CancellationToken cancellationToken = default);

    Task<ResultDto<PostDetailsDto>> CreateAsync(
        CreatePostRequest request,
        CancellationToken cancellationToken = default);

    Task<ResultDto<PostDetailsDto>> UpdateAsync(
        long id,
        UpdatePostRequest request,
        CancellationToken cancellationToken = default);

    Task<ResultDto<PostMediaDto>> UploadMediaAsync(
        long postId,
        IFormFile file,
        CancellationToken cancellationToken = default);

    Task<ResultDto> DeleteMediaAsync(
        long postId,
        long mediaId,
        CancellationToken cancellationToken = default);

    Task<ResultDto> DeleteAsync(
        long id,
        CancellationToken cancellationToken = default);

    Task<ResultDto> PublishAsync(
        long id,
        CancellationToken cancellationToken = default);

    Task<ResultDto> ScheduleAsync(
        long id,
        SchedulePostRequest request,
        CancellationToken cancellationToken = default);

    Task<ResultDto> ExecuteScheduledAsync(
        long id,
        CancellationToken cancellationToken = default);
}
