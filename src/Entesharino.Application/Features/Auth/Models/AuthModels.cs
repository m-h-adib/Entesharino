namespace Entesharino.Application.Features.Auth.Models;

public sealed class RegisterRequest
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public sealed class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public sealed class AuthResponse
{
    public long UserId { get; init; }
    public string AccessToken { get; init; } = string.Empty;
}
