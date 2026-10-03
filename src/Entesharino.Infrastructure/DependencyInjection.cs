using Microsoft.EntityFrameworkCore;
using Hangfire;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Entesharino.Application.Common.Interfaces;
using Entesharino.Application.Features.Auth;
using Entesharino.Application.Features.Channels;
using Entesharino.Application.Features.Posts;
using Entesharino.Application.Features.Reports;
using Entesharino.Infrastructure.Messaging;
using Entesharino.Application.Features.Roles;
using Entesharino.Application.Features.Users;
using Entesharino.Infrastructure.Persistence;
using Entesharino.Infrastructure.Security;
using Entesharino.Infrastructure.Scheduling;
using Entesharino.Infrastructure.Storage;

namespace Entesharino.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection is not configured.");

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddHangfire(config =>
            config
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UseSqlServerStorage(connectionString));

        services.AddHangfireServer();

        services.AddScoped<IDatabaseContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IChannelService, ChannelService>();
        services.AddScoped<IPostService, PostService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<IPostDeliveryService, PostDeliveryService>();
        services.AddScoped<IPostDeliveryRetryScheduler, HangfireDeliveryRetryScheduler>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<ISecretProtector, ChannelSecretProtector>();

        services.AddScoped<IMessageSenderFactory, MessageSenderFactory>();
        services.AddScoped<IPostScheduler, HangfirePostScheduler>();
        services.AddScoped<IMediaStorage, LocalMediaStorage>();

        services.AddHttpClient<TelegramMessageSender>(client =>
        {
            client.BaseAddress = new Uri("https://api.telegram.org/");
            client.Timeout = TimeSpan.FromMinutes(10);
        });

        services.AddHttpClient<BaleMessageSender>(client =>
        {
            client.BaseAddress = new Uri("https://tapi.bale.ai/");
            client.Timeout = TimeSpan.FromMinutes(10);
        });

        services.AddHttpClient<EitaaMessageSender>(client =>
        {
            client.BaseAddress = new Uri("https://eitaayar.ir/api/");
            client.Timeout = TimeSpan.FromMinutes(10);
        });

        services.AddHttpClient<RubikaMessageSender>(client =>
        {
            client.BaseAddress = new Uri("https://botapi.rubika.ir/v3/");
            client.Timeout = TimeSpan.FromMinutes(10);
        });

        services.AddScoped<IMessageSender>(sp => sp.GetRequiredService<TelegramMessageSender>());
        services.AddScoped<IMessageSender>(sp => sp.GetRequiredService<BaleMessageSender>());
        services.AddScoped<IMessageSender>(sp => sp.GetRequiredService<EitaaMessageSender>());
        services.AddScoped<IMessageSender>(sp => sp.GetRequiredService<RubikaMessageSender>());

        services.AddHttpContextAccessor();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<ITokenService, JwtTokenService>();

        return services;
    }
}
