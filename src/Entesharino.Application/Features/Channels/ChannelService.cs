using Microsoft.EntityFrameworkCore;
using Entesharino.Application.Common.Interfaces;
using Entesharino.Application.Common.Models;
using Entesharino.Application.Features.Channels.Models;
using Entesharino.Domain.Entities;

namespace Entesharino.Application.Features.Channels;

public sealed class ChannelService : IChannelService
{
    private const int MaxPageSize = 100;

    private readonly IDatabaseContext _database;
    private readonly ICurrentUserService _currentUser;
    private readonly ISecretProtector _secretProtector;

    public ChannelService(
        IDatabaseContext database,
        ICurrentUserService currentUser,
        ISecretProtector secretProtector)
    {
        _database = database;
        _currentUser = currentUser;
        _secretProtector = secretProtector;
    }

    public async Task<ResultOfList<ChannelListItemDto>> GetListAsync(
        ChannelListRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId;
        if (!userId.HasValue)
            return ResultOfList<ChannelListItemDto>.Fail("کاربر جاری شناسایی نشد.", 401);

        var page = request.Page;
        var pageSize = Math.Min(request.PageSize, MaxPageSize);
        var search = request.Search?.Trim();

        var query = _database.Channels
            .AsNoTracking()
            .Where(x => x.UserId == userId.Value && !x.IsRemoved);

        if (request.Platform.HasValue)
            query = query.Where(x => x.Platform == request.Platform.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(x =>
                x.Name.Contains(search) ||
                x.Identifier.Contains(search));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var channels = await query
            .OrderByDescending(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new ChannelListItemDto
            {
                Id = x.Id,
                Platform = x.Platform,
                Name = x.Name,
                Identifier = x.Identifier,
                Description = x.Description,
                IsActive = x.IsActive,
                IsConnected = x.IsConnected,
                LastConnectionCheckAt = x.LastConnectionCheckAt,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return ResultOfList<ChannelListItemDto>.Ok(channels, totalCount);
    }

    public async Task<ResultDto<ChannelDetailsDto>> GetByIdAsync(
        long id,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId;
        if (!userId.HasValue)
            return ResultDto<ChannelDetailsDto>.Fail("کاربر جاری شناسایی نشد.", 401);

        var channel = await _database.Channels
            .AsNoTracking()
            .Where(x => x.Id == id && x.UserId == userId.Value && !x.IsRemoved)
            .Select(x => new ChannelDetailsDto
            {
                Id = x.Id,
                Platform = x.Platform,
                Name = x.Name,
                Identifier = x.Identifier,
                Description = x.Description,
                IsActive = x.IsActive,
                IsConnected = x.IsConnected,
                LastConnectionCheckAt = x.LastConnectionCheckAt,
                CreatedAt = x.CreatedAt
            })
            .SingleOrDefaultAsync(cancellationToken);

        return channel is null
            ? ResultDto<ChannelDetailsDto>.Fail("کانال موردنظر پیدا نشد.", 404)
            : ResultDto<ChannelDetailsDto>.Ok(channel);
    }

    public async Task<ResultDto<ChannelDetailsDto>> CreateAsync(
        CreateChannelRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId;
        if (!userId.HasValue)
            return ResultDto<ChannelDetailsDto>.Fail("کاربر جاری شناسایی نشد.", 401);

        var identifier = request.Identifier.Trim();

        var exists = await _database.Channels.AnyAsync(
            x => x.Platform == request.Platform &&
                 x.Identifier == identifier &&
                 !x.IsRemoved,
            cancellationToken);

        if (exists)
            return ResultDto<ChannelDetailsDto>.Fail(
                "این کانال برای این پلتفرم قبلاً ثبت شده است.",
                409);

        var channel = new Channel
        {
            UserId = userId.Value,
            Platform = request.Platform,
            Name = request.Name.Trim(),
            Identifier = identifier,
            Description = string.IsNullOrWhiteSpace(request.Description)
                ? null
                : request.Description.Trim(),
            IsConnected = false
        };

        channel.Connection = new ChannelConnection
        {
            EncryptedAccessToken = _secretProtector.Protect(request.AccessToken),
            EncryptedRefreshToken = string.IsNullOrWhiteSpace(request.RefreshToken)
                ? null
                : _secretProtector.Protect(request.RefreshToken),
            TokenExpiresAt = request.TokenExpiresAt
        };

        _database.Channels.Add(channel);
        await _database.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(channel.Id, cancellationToken);
    }

    public async Task<ResultDto<ChannelDetailsDto>> UpdateAsync(
        long id,
        UpdateChannelRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId;
        if (!userId.HasValue)
            return ResultDto<ChannelDetailsDto>.Fail("کاربر جاری شناسایی نشد.", 401);

        var channel = await _database.Channels
            .SingleOrDefaultAsync(
                x => x.Id == id && x.UserId == userId.Value && !x.IsRemoved,
                cancellationToken);

        if (channel is null)
            return ResultDto<ChannelDetailsDto>.Fail("کانال موردنظر پیدا نشد.", 404);

        var identifier = request.Identifier.Trim();

        var duplicate = await _database.Channels.AnyAsync(
            x => x.Id != id &&
                 x.Platform == channel.Platform &&
                 x.Identifier == identifier &&
                 !x.IsRemoved,
            cancellationToken);

        if (duplicate)
            return ResultDto<ChannelDetailsDto>.Fail(
                "کانال دیگری با این شناسه برای این پلتفرم وجود دارد.",
                409);

        channel.Name = request.Name.Trim();
        channel.Identifier = identifier;
        channel.Description = string.IsNullOrWhiteSpace(request.Description)
            ? null
            : request.Description.Trim();

        await _database.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<ResultDto> UpdateConnectionAsync(
        long id,
        UpdateChannelConnectionRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId;
        if (!userId.HasValue)
            return ResultDto.Fail("کاربر جاری شناسایی نشد.", 401);

        var channel = await _database.Channels
            .Include(x => x.Connection)
            .SingleOrDefaultAsync(
                x => x.Id == id && x.UserId == userId.Value && !x.IsRemoved,
                cancellationToken);

        if (channel is null)
            return ResultDto.Fail("کانال موردنظر پیدا نشد.", 404);

        if (channel.Connection is null)
        {
            channel.Connection = new ChannelConnection
            {
                ChannelId = channel.Id
            };
        }

        channel.Connection.EncryptedAccessToken =
            _secretProtector.Protect(request.AccessToken);

        channel.Connection.EncryptedRefreshToken =
            string.IsNullOrWhiteSpace(request.RefreshToken)
                ? null
                : _secretProtector.Protect(request.RefreshToken);

        channel.Connection.TokenExpiresAt = request.TokenExpiresAt;
        channel.Connection.LastValidatedAt = null;
        channel.IsConnected = false;
        channel.LastConnectionCheckAt = null;

        await _database.SaveChangesAsync(cancellationToken);

        return ResultDto.Ok("اطلاعات اتصال کانال با موفقیت به‌روزرسانی شد.");
    }

    public async Task<ResultDto> SetActiveAsync(
        long id,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId;
        if (!userId.HasValue)
            return ResultDto.Fail("کاربر جاری شناسایی نشد.", 401);

        var channel = await _database.Channels
            .SingleOrDefaultAsync(
                x => x.Id == id && x.UserId == userId.Value && !x.IsRemoved,
                cancellationToken);

        if (channel is null)
            return ResultDto.Fail("کانال موردنظر پیدا نشد.", 404);

        channel.IsActive = isActive;
        await _database.SaveChangesAsync(cancellationToken);

        return ResultDto.Ok(isActive ? "کانال فعال شد." : "کانال غیرفعال شد.");
    }

    public async Task<ResultDto> DeleteAsync(
        long id,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId;
        if (!userId.HasValue)
            return ResultDto.Fail("کاربر جاری شناسایی نشد.", 401);

        var channel = await _database.Channels
            .SingleOrDefaultAsync(
                x => x.Id == id && x.UserId == userId.Value && !x.IsRemoved,
                cancellationToken);

        if (channel is null)
            return ResultDto.Fail("کانال موردنظر پیدا نشد.", 404);

        channel.IsRemoved = true;
        channel.IsActive = false;
        channel.IsConnected = false;

        await _database.SaveChangesAsync(cancellationToken);

        return ResultDto.Ok("کانال با موفقیت حذف شد.");
    }
}
