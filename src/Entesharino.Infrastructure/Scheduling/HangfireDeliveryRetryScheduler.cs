using Hangfire;
using Entesharino.Application.Common.Interfaces;

namespace Entesharino.Infrastructure.Scheduling;

public sealed class HangfireDeliveryRetryScheduler : IPostDeliveryRetryScheduler
{
    private readonly IBackgroundJobClient _backgroundJobs;

    public HangfireDeliveryRetryScheduler(IBackgroundJobClient backgroundJobs)
    {
        _backgroundJobs = backgroundJobs;
    }

    public Task ScheduleAsync(
        long postChannelId,
        int retryNumber,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var delay = CalculateDelay(retryNumber);

        _backgroundJobs.Schedule<PostDeliveryRetryJob>(
            job => job.ExecuteAsync(postChannelId),
            delay);

        return Task.CompletedTask;
    }

    public Task RemoveAsync(
        long postChannelId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }

    private static TimeSpan CalculateDelay(int retryNumber)
    {
        var exponent = Math.Clamp(retryNumber - 1, 0, 5);
        var seconds = Math.Min(1800, 30 * Math.Pow(2, exponent));
        return TimeSpan.FromSeconds(seconds);
    }
}

public sealed class PostDeliveryRetryJob
{
    private readonly IPostDeliveryService _deliveryService;

    public PostDeliveryRetryJob(IPostDeliveryService deliveryService)
    {
        _deliveryService = deliveryService;
    }

    public Task ExecuteAsync(long postChannelId)
        => _deliveryService.RetryAsync(postChannelId);
}
