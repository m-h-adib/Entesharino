using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Entesharino.Application.Common.Interfaces;
using Entesharino.Application.Features.Auth;
using Entesharino.Application.Features.Channels;
using Entesharino.Application.Features.Roles;
using Entesharino.Application.Features.Users;
using Entesharino.Infrastructure.Persistence;
using Entesharino.Infrastructure.Security;

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

        services.AddScoped<IDatabaseContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IChannelService, ChannelService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<ISecretProtector, DataProtectionSecretProtector>();
        services.AddDataProtection();
        services.AddHttpContextAccessor();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<ITokenService, JwtTokenService>();

        return services;
    }
}