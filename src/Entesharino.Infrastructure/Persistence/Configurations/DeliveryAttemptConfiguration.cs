using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Entesharino.Domain.Entities;

namespace Entesharino.Infrastructure.Persistence.Configurations;

public class DeliveryAttemptConfiguration : IEntityTypeConfiguration<DeliveryAttempt>
{
    public void Configure(EntityTypeBuilder<DeliveryAttempt> builder)
    {
        builder.ToTable("DeliveryAttempts");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ErrorMessage).HasMaxLength(2000);
        builder.HasIndex(x => x.PostChannelId);
        builder.HasIndex(x => new { x.PostChannelId, x.AttemptNumber }).IsUnique();
        builder.HasOne(x => x.PostChannel).WithMany(x => x.DeliveryAttempts).HasForeignKey(x => x.PostChannelId).OnDelete(DeleteBehavior.Cascade);
    }
}