using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Entesharino.Domain.Entities;

namespace Entesharino.Infrastructure.Persistence.Configurations;

public class PostChannelConfiguration : IEntityTypeConfiguration<PostChannel>
{
    public void Configure(EntityTypeBuilder<PostChannel> builder)
    {
        builder.ToTable("PostChannels");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ExternalMessageId).HasMaxLength(300);
        builder.Property(x => x.ErrorMessage).HasMaxLength(2000);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.ScheduledAt);
        builder.HasIndex(x => new { x.PostId, x.ChannelId }).IsUnique();
        builder.HasOne(x => x.Post).WithMany(x => x.Channels).HasForeignKey(x => x.PostId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Channel).WithMany(x => x.PostChannels).HasForeignKey(x => x.ChannelId).OnDelete(DeleteBehavior.Restrict);
    }
}