using Microsoft.EntityFrameworkCore;
using Entesharino.Application.Common.Interfaces;
using Entesharino.Application.Common.Models;
using Entesharino.Application.Features.Users.Models;
using Entesharino.Domain.Entities;

namespace Entesharino.Application.Features.Users;

public sealed class UserService : IUserService
{
    private readonly IDatabaseContext _database;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ICurrentUserService _currentUser;

    public UserService(IDatabaseContext database, IPasswordHasher passwordHasher, ICurrentUserService currentUser)
    {
        _database = database;
        _passwordHasher = passwordHasher;
        _currentUser = currentUser;
    }

    public async Task<ResultOfList<UserListItemDto>> GetListAsync(UserListRequest request, CancellationToken cancellationToken = default)
    {
        var page = request.Page;
        var pageSize = request.PageSize;
        var search = request.Search?.Trim();

        var query = _database.Users.AsNoTracking().Where(x => !x.IsRemoved);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(x =>
                x.FirstName.Contains(search) ||
                x.LastName.Contains(search) ||
                x.Username.Contains(search) ||
                x.Email.Contains(search));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var users = await query
            .OrderByDescending(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new UserListItemDto
            {
                Id = x.Id,
                FirstName = x.FirstName,
                LastName = x.LastName,
                Username = x.Username,
                Email = x.Email,
                IsActive = x.IsActive,
                LastLoginAt = x.LastLoginAt,
                Roles = x.UserRoles.Select(ur => ur.Role.Name).ToList()
            })
            .ToListAsync(cancellationToken);

        return ResultOfList<UserListItemDto>.Ok(users, totalCount);
    }

    public async Task<ResultDto<UserDetailsDto>> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var user = await _database.Users.AsNoTracking()
            .Where(x => x.Id == id && !x.IsRemoved)
            .Select(x => new UserDetailsDto
            {
                Id = x.Id,
                FirstName = x.FirstName,
                LastName = x.LastName,
                Username = x.Username,
                Email = x.Email,
                IsActive = x.IsActive,
                LastLoginAt = x.LastLoginAt,
                CreatedAt = x.CreatedAt,
                Roles = x.UserRoles.Select(ur => ur.Role.Name).ToList()
            })
            .SingleOrDefaultAsync(cancellationToken);

        return user is null
            ? ResultDto<UserDetailsDto>.Fail("کاربر پیدا نشد.", 404)
            : ResultDto<UserDetailsDto>.Ok(user);
    }

    public async Task<ResultDto<UserDetailsDto>> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        var username = request.Username.Trim();
        var email = request.Email.Trim().ToLowerInvariant();

        var exists = await _database.Users.AnyAsync(x => x.Username == username || x.Email == email, cancellationToken);
        if (exists)
            return ResultDto<UserDetailsDto>.Fail("نام کاربری یا ایمیل قبلاً ثبت شده است.", 409);

        if (request.RoleId.HasValue)
        {
            var roleExists = await _database.Roles.AnyAsync(
                x => x.Id == request.RoleId.Value && x.IsActive && !x.IsRemoved,
                cancellationToken);

            if (!roleExists)
                return ResultDto<UserDetailsDto>.Fail("نقش انتخاب‌شده پیدا نشد.", 404);
        }

        var user = new User
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Username = username,
            Email = email,
            PasswordHash = _passwordHasher.Hash(request.Password)
        };

        if (request.RoleId.HasValue)
            user.UserRoles.Add(new UserRole { RoleId = request.RoleId.Value });

        _database.Users.Add(user);
        await _database.SaveChangesAsync(cancellationToken);

        var result = await GetByIdAsync(user.Id, cancellationToken);
        result.Message = "کاربر با موفقیت ایجاد شد.";
        result.StatusCode = 201;
        return result;
    }

    public async Task<ResultDto<UserDetailsDto>> UpdateAsync(long id, UpdateUserRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _database.Users.SingleOrDefaultAsync(x => x.Id == id && !x.IsRemoved, cancellationToken);
        if (user is null)
            return ResultDto<UserDetailsDto>.Fail("کاربر پیدا نشد.", 404);

        var email = request.Email.Trim().ToLowerInvariant();
        var emailExists = await _database.Users.AnyAsync(
            x => x.Id != id && x.Email == email && !x.IsRemoved,
            cancellationToken);

        if (emailExists)
            return ResultDto<UserDetailsDto>.Fail("این ایمیل قبلاً ثبت شده است.", 409);

        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.Email = email;

        await _database.SaveChangesAsync(cancellationToken);

        var result = await GetByIdAsync(id, cancellationToken);
        result.Message = "اطلاعات کاربر با موفقیت به‌روزرسانی شد.";
        return result;
    }

    public async Task<ResultDto> SetActiveAsync(long id, bool isActive, CancellationToken cancellationToken = default)
    {
        var user = await _database.Users.SingleOrDefaultAsync(x => x.Id == id && !x.IsRemoved, cancellationToken);
        if (user is null)
            return ResultDto.Fail("کاربر پیدا نشد.", 404);

        user.IsActive = isActive;
        await _database.SaveChangesAsync(cancellationToken);
        return ResultDto.Ok(isActive ? "کاربر فعال شد." : "کاربر غیرفعال شد.");
    }

    public async Task<ResultDto> AssignRoleAsync(long id, long roleId, CancellationToken cancellationToken = default)
    {
        var userExists = await _database.Users.AnyAsync(x => x.Id == id && !x.IsRemoved, cancellationToken);
        if (!userExists)
            return ResultDto.Fail("کاربر پیدا نشد.", 404);

        var roleExists = await _database.Roles.AnyAsync(
            x => x.Id == roleId && x.IsActive && !x.IsRemoved,
            cancellationToken);

        if (!roleExists)
            return ResultDto.Fail("نقش پیدا نشد.", 404);

        var userRole = await _database.UserRoles.SingleOrDefaultAsync(x => x.UserId == id, cancellationToken);

        if (userRole is null)
            _database.UserRoles.Add(new UserRole { UserId = id, RoleId = roleId });
        else
            userRole.RoleId = roleId;

        await _database.SaveChangesAsync(cancellationToken);
        return ResultDto.Ok("نقش کاربر با موفقیت تغییر کرد.");
    }
}


    public async Task<ResultDto> ResetPasswordAsync(long id, string password, CancellationToken cancellationToken = default)
    {
        var user = await _database.Users
            .SingleOrDefaultAsync(x => x.Id == id && !x.IsRemoved, cancellationToken);

        if (user is null)
            return ResultDto.Fail("کاربر پیدا نشد.", 404);

        user.PasswordHash = _passwordHasher.Hash(password);
        await _database.SaveChangesAsync(cancellationToken);

        return ResultDto.Ok("رمز عبور کاربر با موفقیت تغییر کرد.");
    }

    public async Task<ResultDto> DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        if (_currentUser.UserId == id)
            return ResultDto.Fail("حذف کاربر جاری مجاز نیست.", 400);

        var user = await _database.Users
            .SingleOrDefaultAsync(x => x.Id == id && !x.IsRemoved, cancellationToken);

        if (user is null)
            return ResultDto.Fail("کاربر پیدا نشد.", 404);

        user.IsRemoved = true;
        user.IsActive = false;

        await _database.SaveChangesAsync(cancellationToken);
        return ResultDto.Ok("کاربر با موفقیت حذف شد.");
    }
}