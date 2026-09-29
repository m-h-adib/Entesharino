using Entesharino.Domain.Enums;

namespace Entesharino.Application.Common.Interfaces;

public interface IPostScheduler
{
    Task ScheduleAsync(
        long postId,
        ScheduleType scheduleType,
        DateTime? scheduledAt,
        string? cronExpression,
        string? timeZone,
        CancellationToken cancellationToken = default);

    Task RemoveAsync(
        long postId,
        CancellationToken cancellationToken = default);
}
