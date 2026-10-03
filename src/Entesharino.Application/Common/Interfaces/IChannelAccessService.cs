namespace Entesharino.Application.Common.Interfaces;

public interface IChannelAccessService
{
    Task<bool> HasAccessAsync(long channelId, long userId, CancellationToken cancellationToken = default);
    Task<bool> IsAdminAsync(long userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<long>> GetAccessibleChannelIdsAsync(long userId, CancellationToken cancellationToken = default);
}
