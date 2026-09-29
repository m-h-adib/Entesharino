using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Entesharino.Domain.Entities;

namespace Entesharino.Infrastructure.Persistence.Configurations;

public class PostConfiguration : IEntityTypeConfiguration<Post>
{
    public void Configure(EntityTypeBuilder<Post> builder)
    {
        builder.ToTable("Posts");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).HasMaxLength(300).IsRequired();
        builder.Property(x => x.Content).HasColumnType("nvarchar(max)");
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => new { x.UserId, x.CreatedAt });
        builder.HasOne(x => x.User).WithMany(x => x.Posts).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
    }
}