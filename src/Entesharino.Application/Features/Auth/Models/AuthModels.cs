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
    public string UsernameOrEmail { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public sealed class AuthResult
{
    public bool Succeeded { get; init; }
    public string? Message { get; init; }
    public long? UserId { get; init; }
    public string? AccessToken { get; init; }

    public static AuthResult Success(long userId, string accessToken) =>
        new() { Succeeded = true, UserId = userId, AccessToken = accessToken };

    public static AuthResult Failure(string message) =>
        new() { Succeeded = false, Message = message };
}
