using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Entesharino.Domain.Entities;

namespace Entesharino.Infrastructure.Persistence.Configurations;

public sealed class ChannelUserAccessConfiguration : IEntityTypeConfiguration<ChannelUserAccess>
{
    public void Configure(EntityTypeBuilder<ChannelUserAccess> builder)
    {
        builder.ToTable("ChannelUserAccesses");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.ChannelId, x.UserId }).IsUnique();
        builder.HasIndex(x => x.UserId);
        builder.HasOne(x => x.Channel)
            .WithMany(x => x.UserAccesses)
            .HasForeignKey(x => x.ChannelId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.User)
            .WithMany(x => x.ChannelAccesses)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
