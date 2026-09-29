namespace Entesharino.Application.Common.Interfaces;

public interface IPostDeliveryRetryScheduler
{
    Task ScheduleAsync(
        long postChannelId,
        int retryNumber,
        CancellationToken cancellationToken = default);

    Task RemoveAsync(
        long postChannelId,
        CancellationToken cancellationToken = default);
}
