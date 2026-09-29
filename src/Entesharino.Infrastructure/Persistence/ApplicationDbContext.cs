using Microsoft.EntityFrameworkCore;
using Entesharino.Application.Common.Interfaces;
using Entesharino.Domain.Entities;

namespace Entesharino.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext, IDatabaseContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<Channel> Channels => Set<Channel>();
    public DbSet<ChannelConnection> ChannelConnections => Set<ChannelConnection>();
    public DbSet<Post> Posts => Set<Post>();
    public DbSet<PostMedia> PostMedia => Set<PostMedia>();
    public DbSet<PostMediaDelivery> PostMediaDeliveries => Set<PostMediaDelivery>();
    public DbSet<PostChannel> PostChannels => Set<PostChannel>();
    public DbSet<DeliveryAttempt> DeliveryAttempts => Set<DeliveryAttempt>();
    public DbSet<PostSchedule> PostSchedules => Set<PostSchedule>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<Entesharino.Domain.Common.BaseEntity>())
        {
            if (entry.State == EntityState.Added)
                entry.Entity.CreatedAt = DateTime.UtcNow;
            else if (entry.State == EntityState.Modified)
                entry.Entity.UpdatedAt = DateTime.UtcNow;
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}