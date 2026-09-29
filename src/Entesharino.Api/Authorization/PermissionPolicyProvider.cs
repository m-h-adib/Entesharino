using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Entesharino.Api.Authorization;

public sealed class PermissionPolicyProvider : DefaultAuthorizationPolicyProvider
{
    public const string Prefix = "Permission:";

    public PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
        : base(options)
    {
    }

    public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (!policyName.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
            return await base.GetPolicyAsync(policyName);

        var permission = policyName[Prefix.Length..];

        if (string.IsNullOrWhiteSpace(permission))
            return null;

        return new AuthorizationPolicyBuilder()
            .AddRequirements(new PermissionRequirement(permission))
            .Build();
    }
}
