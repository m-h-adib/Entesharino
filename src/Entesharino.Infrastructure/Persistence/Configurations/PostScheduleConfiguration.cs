using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Entesharino.Domain.Entities;

namespace Entesharino.Infrastructure.Persistence.Configurations;

public class PostScheduleConfiguration : IEntityTypeConfiguration<PostSchedule>
{
    public void Configure(EntityTypeBuilder<PostSchedule> builder)
    {
        builder.ToTable("PostSchedules");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CronExpression).HasMaxLength(200);
        builder.Property(x => x.TimeZone).HasMaxLength(100);
        builder.HasIndex(x => x.NextRunAt);
        builder.HasIndex(x => new { x.ScheduleType, x.IsCompleted });
        builder.HasOne(x => x.Post).WithOne(x => x.Schedule).HasForeignKey<PostSchedule>(x => x.PostId).OnDelete(DeleteBehavior.Cascade);
    }
}