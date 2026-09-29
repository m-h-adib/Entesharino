using Microsoft.AspNetCore.Authorization;

namespace Entesharino.Api.Authorization;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class HasPermissionAttribute : AuthorizeAttribute
{
    public HasPermissionAttribute(string permission)
    {
        Policy = $"{PermissionPolicyProvider.Prefix}{permission}";
    }
}
