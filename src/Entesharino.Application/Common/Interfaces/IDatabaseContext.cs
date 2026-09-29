using Microsoft.EntityFrameworkCore;
using Entesharino.Domain.Entities;

namespace Entesharino.Application.Common.Interfaces;

public interface IDatabaseContext
{
    DbSet<User> Users { get; }
    DbSet<Role> Roles { get; }
    DbSet<Permission> Permissions { get; }
    DbSet<UserRole> UserRoles { get; }
    DbSet<RolePermission> RolePermissions { get; }
    DbSet<Channel> Channels { get; }
    DbSet<ChannelConnection> ChannelConnections { get; }
    DbSet<Post> Posts { get; }
    DbSet<PostMedia> PostMedia { get; }
    DbSet<PostMediaDelivery> PostMediaDeliveries { get; }
    DbSet<PostChannel> PostChannels { get; }
    DbSet<DeliveryAttempt> DeliveryAttempts { get; }
    DbSet<PostSchedule> PostSchedules { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}