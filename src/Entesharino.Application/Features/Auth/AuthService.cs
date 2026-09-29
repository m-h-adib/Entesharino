using Microsoft.EntityFrameworkCore;
using Entesharino.Application.Common.Interfaces;
using Entesharino.Application.Common.Models;
using Entesharino.Application.Features.Auth.Models;
using Entesharino.Domain.Entities;

namespace Entesharino.Application.Features.Auth;

public sealed class AuthService : IAuthService
{
    private readonly IDatabaseContext _database;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;

    public AuthService(
        IDatabaseContext database,
        IPasswordHasher passwordHasher,
        ITokenService tokenService)
    {
        _database = database;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
    }

    public async Task<ResultDto<AuthResponse>> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        var username = request.Username.Trim();
        var email = request.Email.Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(username) ||
            string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            return ResultDto<AuthResponse>.Fail(
                "اطلاعات ثبت‌نام کامل نیست.",
                400);
        }

        var exists = await _database.Users.AnyAsync(
            x => x.Username == username || x.Email == email,
            cancellationToken);

        if (exists)
        {
            return ResultDto<AuthResponse>.Fail(
                "نام کاربری یا ایمیل قبلاً ثبت شده است.",
                409);
        }

        var user = new User
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Username = username,
            Email = email,
            PasswordHash = _passwordHasher.Hash(request.Password)
        };

        _database.Users.Add(user);
        await _database.SaveChangesAsync(cancellationToken);

        var token = _tokenService.CreateAccessToken(user);

        return ResultDto<AuthResponse>.Ok(
            new AuthResponse
            {
                UserId = user.Id,
                AccessToken = token
            },
            "ثبت‌نام با موفقیت انجام شد.",
            201);
    }

    public async Task<ResultDto<AuthResponse>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        var value = request.UsernameOrEmail.Trim();
        var email = value.ToLowerInvariant();

        var user = await _database.Users.FirstOrDefaultAsync(
            x => x.Username == value || x.Email == email,
            cancellationToken);

        if (user is null || user.IsRemoved || !user.IsActive)
        {
            return ResultDto<AuthResponse>.Fail(
                "نام کاربری یا رمز عبور صحیح نیست.",
                401);
        }

        if (!_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            return ResultDto<AuthResponse>.Fail(
                "نام کاربری یا رمز عبور صحیح نیست.",
                401);
        }

        user.LastLoginAt = DateTime.UtcNow;
        await _database.SaveChangesAsync(cancellationToken);

        var token = _tokenService.CreateAccessToken(user);

        return ResultDto<AuthResponse>.Ok(
            new AuthResponse
            {
                UserId = user.Id,
                AccessToken = token
            },
            "ورود با موفقیت انجام شد.",
            200);
    }
}
