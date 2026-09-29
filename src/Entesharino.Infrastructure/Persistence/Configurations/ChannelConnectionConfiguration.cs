using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Entesharino.Domain.Entities;

namespace Entesharino.Infrastructure.Persistence.Configurations;

public class ChannelConnectionConfiguration : IEntityTypeConfiguration<ChannelConnection>
{
    public void Configure(EntityTypeBuilder<ChannelConnection> builder)
    {
        builder.ToTable("ChannelConnections");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.EncryptedAccessToken).HasMaxLength(4000).IsRequired();
        builder.Property(x => x.EncryptedRefreshToken).HasMaxLength(4000);
        builder.HasIndex(x => x.ChannelId).IsUnique();
    }
}