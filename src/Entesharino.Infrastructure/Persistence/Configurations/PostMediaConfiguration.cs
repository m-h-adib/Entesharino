using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Entesharino.Domain.Entities;

namespace Entesharino.Infrastructure.Persistence.Configurations;

public class PostMediaConfiguration : IEntityTypeConfiguration<PostMedia>
{
    public void Configure(EntityTypeBuilder<PostMedia> builder)
    {
        builder.ToTable("PostMedia");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.FileName).HasMaxLength(300).IsRequired();
        builder.Property(x => x.FileUrl).HasMaxLength(2000).IsRequired();
        builder.HasIndex(x => x.PostId);
        builder.HasOne(x => x.Post).WithMany(x => x.Media).HasForeignKey(x => x.PostId).OnDelete(DeleteBehavior.Cascade);
    }
}