using Entesharino.Application.Common.Interfaces;
using Entesharino.Application.Features.Posts;
using Entesharino.Domain.Entities;
using Entesharino.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Entesharino.Application.Tests;

public sealed class PostDeliveryServiceTests
{
    [Fact]
    public async Task RetryAsync_WhenMediaWasSentPreviously_DoesNotResendIt()
    {
        await using var database = new TestDatabaseContext();
        var post = CreatePost();
        var telegram = CreateChannel(1, PlatformType.Telegram);
        var bale = CreateChannel(2, PlatformType.Bale);

        post.Channels.Add(new PostChannel
        {
            Id = 101,
            PostId = post.Id,
            ChannelId = telegram.Id,
            Post = post,
            Channel = telegram,
            Status = DeliveryStatus.Failed,
            RetryCount = 1
        });

        post.Channels.Add(new PostChannel
        {
            Id = 102,
            PostId = post.Id,
            ChannelId = bale.Id,
            ChannelId = bale.Id,
            Post = post,
            Channel = bale,
            Status = DeliveryStatus.Pending
        });

        database.Posts.Add(post);
        database.Channels.AddRange(telegram, bale);
        await database.SaveChangesAsync();

        var sender = new RecordingSender();
        sender.FailTelegramVideoOnce = true;

        var factory = new Mock<IMessageSenderFactory>();
        factory.Setup(x => x.Get(PlatformType.Telegram)).Returns(sender);
        factory.Setup(x => x.Get(PlatformType.Bale)).Returns(sender);

        var storage = new Mock<IMediaStorage>();
        storage
            .Setup(x => x.OpenReadAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string _, CancellationToken _) =>
                new MemoryStream(new byte[] { 1, 2, 3 }));

        var secret = new Mock<ISecretProtector>();
        secret.Setup(x => x.Unprotect(It.IsAny<string>())).Returns("token");

        var scheduler = new Mock<IPostDeliveryRetryScheduler>();

        var service = new PostDeliveryService(
            database,
            secret.Object,
            factory.Object,
            scheduler.Object,
            storage.Object);

        var first = await service.RetryAsync(101);
        Assert.Equal(202, first.StatusCode);

        var firstTelegramCalls = sender.Calls
            .Where(x => x.Platform == PlatformType.Telegram)
            .Select(x => x.FileName)
            .ToList();

        Assert.Equal(["image.jpg", "video.mp4"], firstTelegramCalls);

        var second = await service.RetryAsync(101);
        Assert.True(second.Success);

        var telegramCalls = sender.Calls
            .Where(x => x.Platform == PlatformType.Telegram)
            .Select(x => x.FileName)
            .ToList();

        Assert.Equal(
            ["image.jpg", "video.mp4", "video.mp4", "pdf.pdf"],
            telegramCalls);

        var baleResult = await service.RetryAsync(102);
        Assert.True(baleResult.Success);

        var baleCalls = sender.Calls
            .Where(x => x.Platform == PlatformType.Bale)
            .Select(x => x.FileName)
            .ToList();

        Assert.Equal(
            ["image.jpg", "video.mp4", "pdf.pdf"],
            baleCalls);

        var deliveries = await database.PostMediaDeliveries
            .OrderBy(x => x.PostChannelId)
            .ThenBy(x => x.PostMediaId)
            .ToListAsync();

        Assert.Equal(6, deliveries.Count);
        Assert.All(deliveries, x => Assert.Equal(DeliveryStatus.Sent, x.Status));
        Assert.All(deliveries, x => Assert.NotNull(x.SentAt));

        var telegramTarget = await database.PostChannels.SingleAsync(x => x.Id == 101);
        var baleTarget = await database.PostChannels.SingleAsync(x => x.Id == 102);

        Assert.Equal(DeliveryStatus.Sent, telegramTarget.Status);
        Assert.Equal(DeliveryStatus.Sent, baleTarget.Status);

        var refreshedPost = await database.Posts.SingleAsync(x => x.Id == post.Id);
        Assert.Equal(PostStatus.Completed, refreshedPost.Status);
    }

    private static Post CreatePost()
    {
        return new Post
        {
            Id = 10,
            UserId = 1,
            Title = "Retry test",
            Content = "Test content",
            Status = PostStatus.Processing,
            Media =
            [
                new PostMedia
                {
                    Id = 201,
                    PostId = 10,
                    MediaType = MediaType.Image,
                    FileName = "image.jpg",
                    FileUrl = "/media/image.jpg",
                    FileSize = 100
                },
                new PostMedia
                {
                    Id = 202,
                    PostId = 10,
                    MediaType = MediaType.Video,
                    FileName = "video.mp4",
                    FileUrl = "/media/video.mp4",
                    FileSize = 200
                },
                new PostMedia
                {
                    Id = 203,
                    PostId = 10,
                    MediaType = MediaType.Document,
                    FileName = "pdf.pdf",
                    FileUrl = "/media/pdf.pdf",
                    FileSize = 300
                }
            ]
        };
    }

    private static Channel CreateChannel(long id, PlatformType platform)
    {
        var channel = new Channel
        {
            Id = id,
            UserId = 1,
            Platform = platform,
            Name = platform.ToString(),
            Identifier = $"channel-{id}",
            IsActive = true,
            Connection = new ChannelConnection
            {
                Id = id + 1000,
                ChannelId = id,
                EncryptedAccessToken = "encrypted-token"
            }
        };

        return channel;
    }

    private sealed class RecordingSender : IMessageSender
    {
        public PlatformType Platform => PlatformType.Telegram;

        public bool FailTelegramVideoOnce { get; set; }

        public List<Call> Calls { get; } = [];

        public Task<SenderResult> TestConnectionAsync(
            ChannelCredentials credentials,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new SenderResult(true));

        public Task<SenderResult> SendTextAsync(
            ChannelCredentials credentials,
            string text,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new SenderResult(true, "text"));

        public async Task<SenderResult> SendMediaAsync(
            ChannelCredentials credentials,
            MediaMessage media,
            CancellationToken cancellationToken = default)
        {
            Calls.Add(new Call(
                credentials.Identifier,
                PlatformType.Telegram,
                media.FileName));

            if (FailTelegramVideoOnce &&
                credentials.Identifier == "channel-1" &&
                media.FileName == "video.mp4")
            {
                FailTelegramVideoOnce = false;
                return new SenderResult(false, ErrorMessage: "simulated video failure");
            }

            return await Task.FromResult(
                new SenderResult(true, Guid.NewGuid().ToString("N")));
        }
    }

    private sealed record Call(
        string Identifier,
        PlatformType Platform,
        string FileName);
}

internal sealed class TestDatabaseContext : IDatabaseContext, IAsyncDisposable
{
    private readonly TestDbContext _db;

    public TestDatabaseContext()
    {
        _db = new TestDbContext();
    }

    public DbSet<User> Users => _db.Users;
    public DbSet<Role> Roles => _db.Roles;
    public DbSet<Permission> Permissions => _db.Permissions;
    public DbSet<UserRole> UserRoles => _db.UserRoles;
    public DbSet<RolePermission> RolePermissions => _db.RolePermissions;
    public DbSet<Channel> Channels => _db.Channels;
    public DbSet<ChannelConnection> ChannelConnections => _db.ChannelConnections;
    public DbSet<Post> Posts => _db.Posts;
    public DbSet<PostMedia> PostMedia => _db.PostMedia;
    public DbSet<PostMediaDelivery> PostMediaDeliveries => _db.PostMediaDeliveries;
    public DbSet<PostChannel> PostChannels => _db.PostChannels;
    public DbSet<DeliveryAttempt> DeliveryAttempts => _db.DeliveryAttempts;
    public DbSet<PostSchedule> PostSchedules => _db.PostSchedules;

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => _db.SaveChangesAsync(cancellationToken);

    public ValueTask DisposeAsync() => _db.DisposeAsync();
}

internal sealed class TestDbContext : DbContext
{
    public TestDbContext()
        : base(new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options)
    {
    }

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
}
