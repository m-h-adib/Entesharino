using Hangfire;
using Entesharino.Application.Common.Interfaces;
using Entesharino.Domain.Enums;

namespace Entesharino.Infrastructure.Scheduling;

public sealed class HangfirePostScheduler : IPostScheduler
{
    private readonly IRecurringJobManager _recurringJobs;

    public HangfirePostScheduler(IRecurringJobManager recurringJobs)
    {
        _recurringJobs = recurringJobs;
    }

    public Task ScheduleAsync(
        long postId,
        ScheduleType scheduleType,
        DateTime? scheduledAt,
        string? cronExpression,
        string? timeZone,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var recurringJobId = GetRecurringJobId(postId);

        if (scheduleType == ScheduleType.Cron ||
            scheduleType == ScheduleType.Daily ||
            scheduleType == ScheduleType.Weekly ||
            scheduleType == ScheduleType.Monthly)
        {
            var cron = ResolveCron(scheduleType, scheduledAt, cronExpression);

            _recurringJobs.AddOrUpdate<PostExecutionJob>(
                recurringJobId,
                job => job.ExecuteAsync(postId),
                cron,
                new RecurringJobOptions
                {
                    TimeZone = ResolveTimeZone(timeZone)
                });

            return Task.CompletedTask;
        }

        if (!scheduledAt.HasValue)
            throw new InvalidOperationException(
                "برای زمان‌بندی یک‌باره، ScheduledAt الزامی است.");

        BackgroundJob.Schedule<PostExecutionJob>(
            job => job.ExecuteAsync(postId),
            scheduledAt.Value.ToUniversalTime() - DateTime.UtcNow);

        return Task.CompletedTask;
    }

    public Task RemoveAsync(
        long postId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _recurringJobs.RemoveIfExists(GetRecurringJobId(postId));
        return Task.CompletedTask;
    }

    private static string ResolveCron(
        ScheduleType scheduleType,
        DateTime? scheduledAt,
        string? cronExpression)
    {
        if (scheduleType == ScheduleType.Cron)
        {
            if (string.IsNullOrWhiteSpace(cronExpression))
                throw new InvalidOperationException("CronExpression الزامی است.");

            return cronExpression.Trim();
        }

        if (!scheduledAt.HasValue)
            throw new InvalidOperationException(
                "برای زمان‌بندی تکرارشونده، ScheduledAt الزامی است.");

        var minute = scheduledAt.Value.Minute;
        var hour = scheduledAt.Value.Hour;

        return scheduleType switch
        {
            ScheduleType.Daily => Cron.Daily(hour, minute),
            ScheduleType.Weekly => Cron.Weekly(
                scheduledAt.Value.DayOfWeek,
                hour,
                minute),
            ScheduleType.Monthly => Cron.Monthly(
                scheduledAt.Value.Day,
                hour,
                minute),
            _ => throw new InvalidOperationException(
                $"نوع زمان‌بندی '{scheduleType}' پشتیبانی نمی‌شود.")
        };
    }

    private static TimeZoneInfo ResolveTimeZone(string? timeZone)
    {
        if (string.IsNullOrWhiteSpace(timeZone))
            return TimeZoneInfo.Utc;

        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZone.Trim());
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.Utc;
        }
        catch (InvalidTimeZoneException)
        {
            return TimeZoneInfo.Utc;
        }
    }

    private static string GetRecurringJobId(long postId)
        => $"post-schedule:{postId}";
}

public sealed class PostExecutionJob
{
    private readonly IPostService _postService;

    public PostExecutionJob(IPostService postService)
    {
        _postService = postService;
    }

    public async Task ExecuteAsync(long postId)
    {
        await _postService.ExecuteScheduledAsync(postId);
    }
}
