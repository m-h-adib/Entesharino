using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Entesharino.Application.Common.Interfaces;
using Entesharino.Domain.Entities;

namespace Entesharino.Infrastructure.Security;

public sealed class JwtTokenService : ITokenService
{
    private const string PermissionClaim = "permission";

    private readonly IConfiguration _configuration;
    private readonly IDatabaseContext _database;

    public JwtTokenService(
        IConfiguration configuration,
        IDatabaseContext database)
    {
        _configuration = configuration;
        _database = database;
    }

    public async Task<string> CreateAccessTokenAsync(
        User user,
        CancellationToken cancellationToken = default)
    {
        var key = _configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("Jwt:Key is not configured.");

        var issuer = _configuration["Jwt:Issuer"];
        var audience = _configuration["Jwt:Audience"];

        var roles = await _database.UserRoles
            .Where(x => x.UserId == user.Id)
            .Where(x => x.Role.IsActive && !x.Role.IsRemoved)
            .Select(x => x.Role.Name)
            .Distinct()
            .ToListAsync(cancellationToken);

        var permissions = await _database.UserRoles
            .Where(x => x.UserId == user.Id)
            .Where(x => x.Role.IsActive && !x.Role.IsRemoved)
            .SelectMany(x => x.Role.RolePermissions)
            .Where(x => x.Permission.IsActive && !x.Permission.IsRemoved)
            .Select(x => x.Permission.Code)
            .Distinct()
            .ToListAsync(cancellationToken);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Email, user.Email)
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
        claims.AddRange(permissions.Select(permission => new Claim(PermissionClaim, permission)));

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer,
            audience,
            claims,
            expires: DateTime.UtcNow.AddHours(2),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
