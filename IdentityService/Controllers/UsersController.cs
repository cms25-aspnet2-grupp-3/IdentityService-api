using IdentityService.DTOs.User;
using IdentityService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace IdentityService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly UserService _userService;

    public UsersController(UserService userService)
    {
        _userService = userService;
    }

    /// <summary>
    /// Get any user by their ID.
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetUser(Guid id)
    {
        var user = await _userService.GetUserByIdAsync(id);
        return user == null ? NotFound() : Ok(user);
    }

    /// <summary>
    /// Get all users.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAllUsers()
    {
        var users = await _userService.GetAllUsersAsync();
        return Ok(users);
    }

    /// <summary>
    /// Get the currently authenticated user's own profile.
    /// </summary>
    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var user = await _userService.GetUserByIdAsync(userId.Value);
        return user == null ? NotFound() : Ok(user);
    }

    /// <summary>
    /// Update a user's name or email.
    /// Users can only update their own profile.
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateUserDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        // Prevent users from editing other accounts
        var currentUserId = GetCurrentUserId();
        if (currentUserId != id) return Forbid();

        try
        {
            var user = await _userService.UpdateUserAsync(id, dto);
            return user == null ? NotFound() : Ok(user);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Upload or replace a profile picture.
    /// Send as multipart/form-data with a field named "file".
    /// Allowed types: JPG, PNG, WEBP. Max size: 5MB.
    /// </summary>
    [HttpPost("{id}/profile-picture")]
    public async Task<IActionResult> UploadProfilePicture(Guid id, IFormFile file)
    {
        // Prevent users from editing other accounts
        var currentUserId = GetCurrentUserId();
        if (currentUserId != id) return Forbid();

        if (file == null || file.Length == 0)
            return BadRequest(new { message = "No file provided." });

        try
        {
            var user = await _userService.UpdateProfilePictureAsync(id, file);
            return user == null ? NotFound() : Ok(user);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Delete a user account.
    /// Users can only delete their own account.
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId != id) return Forbid();

        var deleted = await _userService.DeleteUserAsync(id);
        return deleted ? NoContent() : NotFound();
    }

    // Reads the user's ID from the JWT claims (set in JwtHelper)
    private Guid? GetCurrentUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? User.FindFirstValue("sub");
        return Guid.TryParse(sub, out var id) ? id : null;
    }
}