namespace Entesharino.Application.Features.Users.Models;

public class UserListItemDto
{
    public long Id { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}".Trim();
    public string Username { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public DateTime? LastLoginAt { get; init; }
    public IReadOnlyList<string> Roles { get; init; } = [];
}

public sealed class UserDetailsDto : UserListItemDto
{
    public DateTime CreatedAt { get; init; }
}

public sealed class CreateUserRequest
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public long? RoleId { get; set; }
}

public sealed class UpdateUserRequest
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public sealed class SetUserActiveRequest
{
    public bool IsActive { get; set; }
}

public sealed class AssignRoleRequest
{
    public long RoleId { get; set; }
}

public sealed class ResetUserPasswordRequest
{
    public string Password { get; set; } = string.Empty;
}

public sealed class UserListRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Search { get; set; }
}