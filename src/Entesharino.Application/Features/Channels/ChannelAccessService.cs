using Microsoft.EntityFrameworkCore;
using Entesharino.Application.Common.Interfaces;
using Entesharino.Domain.Constants;

namespace Entesharino.Application.Features.Channels;

public sealed class ChannelAccessService : IChannelAccessService
{
    private readonly IDatabaseContext _database;

    public ChannelAccessService(IDatabaseContext database) => _database = database;

    public Task<bool> IsAdminAsync(long userId, CancellationToken cancellationToken = default) =>
        _database.UserRoles.AnyAsync(x =>
            x.UserId == userId &&
            x.Role.Name == RoleNames.Admin &&
            !x.Role.IsRemoved &&
            x.Role.IsActive,
            cancellationToken);

    public async Task<bool> HasAccessAsync(long channelId, long userId, CancellationToken cancellationToken = default)
    {
        if (await IsAdminAsync(userId, cancellationToken))
            return await _database.Channels.AnyAsync(x => x.Id == channelId && !x.IsRemoved && x.IsActive, cancellationToken);

        return await _database.Channels.AnyAsync(x =>
            x.Id == channelId && !x.IsRemoved && x.IsActive &&
            (x.UserId == userId || x.UserAccesses.Any(a => a.UserId == userId && !a.IsRemoved && a.IsActive)),
            cancellationToken);
    }

    public async Task<IReadOnlyList<long>> GetAccessibleChannelIdsAsync(long userId, CancellationToken cancellationToken = default)
    {
        if (await IsAdminAsync(userId, cancellationToken))
            return await _database.Channels.Where(x => !x.IsRemoved && x.IsActive).Select(x => x.Id).ToListAsync(cancellationToken);

        return await _database.Channels
            .Where(x => !x.IsRemoved && x.IsActive &&
                (x.UserId == userId || x.UserAccesses.Any(a => a.UserId == userId && !a.IsRemoved && a.IsActive)))
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);
    }
}
