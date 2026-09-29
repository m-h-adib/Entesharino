using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Entesharino.Domain.Entities;

namespace Entesharino.Infrastructure.Persistence.Configurations;

public class PostMediaDeliveryConfiguration : IEntityTypeConfiguration<PostMediaDelivery>
{
    public void Configure(EntityTypeBuilder<PostMediaDelivery> builder)
    {
        builder.ToTable("PostMediaDeliveries");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ExternalMessageId).HasMaxLength(300);
        builder.Property(x => x.ErrorMessage).HasMaxLength(2000);

        builder.HasIndex(x => new { x.PostChannelId, x.PostMediaId }).IsUnique();
        builder.HasIndex(x => x.Status);

        builder.HasOne(x => x.PostChannel)
            .WithMany()
            .HasForeignKey(x => x.PostChannelId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.PostMedia)
            .WithMany()
            .HasForeignKey(x => x.PostMediaId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
