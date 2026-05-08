using IdentityService.Data;
using IdentityService.DTOs.User;
using IdentityService.Models;
using Microsoft.EntityFrameworkCore;

namespace IdentityService.Services;

public class UserService
{
    private readonly AppDbContext _db;
    private readonly ImageService _imageService;

    public UserService(AppDbContext db, ImageService imageService)
    {
        _db = db;
        _imageService = imageService;
    }
    public async Task<UserDto?> GetUserByIdAsync(Guid id)
    {
        var user = await _db.Users.FindAsync(id);
        return user == null ? null : MapToDto(user);
    }

    public async Task<List<UserDto>> GetAllUsersAsync()
    {
        return await _db.Users
            .Select(u => MapToDto(u))
            .ToListAsync();
    }

    public async Task<UserDto?> UpdateUserAsync(Guid id, UpdateUserDto dto)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null) return null;

        if (dto.FirstName != null) user.FirstName = dto.FirstName;
        if (dto.LastName != null) user.LastName = dto.LastName;

        // If changing email, make sure it isn't already taken
        if (dto.Email != null && dto.Email.ToLower() != user.Email)
        {
            var emailTaken = await _db.Users.AnyAsync(u => u.Email == dto.Email.ToLower() && u.Id != id);
            if (emailTaken) throw new InvalidOperationException("Email is already in use.");
            user.Email = dto.Email.ToLower();
        }

        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return MapToDto(user);
    }

    public async Task<UserDto?> UpdateProfilePictureAsync(Guid id, IFormFile file)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null) return null;

        user.ProfilePictureUrl = await _imageService.UploadProfilePictureAsync(file);
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return MapToDto(user);
    }

    public async Task<bool> DeleteUserAsync(Guid id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null) return false;
        _db.Users.Remove(user);
        await _db.SaveChangesAsync();
        return true;
    }

    private static UserDto MapToDto(User user) => new()
    {
        Id = user.Id,
        FirstName = user.FirstName,
        LastName = user.LastName,
        Email = user.Email,
        ProfilePictureUrl = user.ProfilePictureUrl,
        CreatedAt = user.CreatedAt
    };
}