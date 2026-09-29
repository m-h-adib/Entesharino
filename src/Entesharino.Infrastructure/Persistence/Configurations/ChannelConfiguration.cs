using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Entesharino.Domain.Entities;

namespace Entesharino.Infrastructure.Persistence.Configurations;

public class ChannelConfiguration : IEntityTypeConfiguration<Channel>
{
    public void Configure(EntityTypeBuilder<Channel> builder)
    {
        builder.ToTable("Channels");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Identifier).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(1000);
        builder.HasIndex(x => new { x.UserId, x.Platform });
        builder.HasIndex(x => new { x.Platform, x.Identifier }).IsUnique();
        builder.HasIndex(x => x.IsActive);
        builder.HasOne(x => x.User).WithMany(x => x.Channels).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Connection).WithOne(x => x.Channel).HasForeignKey<ChannelConnection>(x => x.ChannelId).OnDelete(DeleteBehavior.Cascade);
    }
}