using Consensus.Core.Constants;
using Consensus.Core.Entities;
using Consensus.Core.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Consensus.Data.Services;

/// <summary>
/// Service for user management operations
/// </summary>
public class UserService : IUserService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly ConsensusDbContext _context;
    private readonly ILogger<UserService> _logger;

    public UserService(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        ConsensusDbContext context,
        ILogger<UserService> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<ApplicationUser>> GetAllUsersAsync()
    {
        try
        {
            return await _context.Users
                .OrderBy(u => u.Email)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all users");
            throw;
        }
    }

    public async Task<ApplicationUser?> GetUserByIdAsync(string userId)
    {
        try
        {
            return await _userManager.FindByIdAsync(userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user by ID: {UserId}", userId);
            throw;
        }
    }

    public async Task<ApplicationUser?> GetUserByEmailAsync(string email)
    {
        try
        {
            return await _userManager.FindByEmailAsync(email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user by email: {Email}", email);
            throw;
        }
    }

    public async Task<IEnumerable<ApplicationRole>> GetAllRolesAsync()
    {
        try
        {
            return await _roleManager.Roles
                .OrderBy(r => r.Name)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all roles");
            throw;
        }
    }

    public async Task<IList<string>> GetUserRolesAsync(ApplicationUser user)
    {
        try
        {
            return await _userManager.GetRolesAsync(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving roles for user: {UserId}", user.Id);
            throw;
        }
    }

    public async Task<IList<string>> GetUserRolesAsync(string userId)
    {
        try
        {
            var user = await GetUserByIdAsync(userId);
            if (user == null) return new List<string>();
            
            return await GetUserRolesAsync(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving roles for user ID: {UserId}", userId);
            throw;
        }
    }

    public async Task<IdentityResult> AssignRoleToUserAsync(string userId, string roleName, string assignedByUserId)
    {
        try
        {
            var user = await GetUserByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("Cannot assign role to user - user not found: {UserId}", userId);
                return IdentityResult.Failed(new IdentityError { Description = "User not found" });
            }

            var role = await _roleManager.FindByNameAsync(roleName);
            if (role == null)
            {
                _logger.LogWarning("Cannot assign role - role not found: {RoleName}", roleName);
                return IdentityResult.Failed(new IdentityError { Description = "Role not found" });
            }

            var currentRoles = await GetUserRolesAsync(user);
            var oldValue = string.Join(", ", currentRoles);

            var result = await _userManager.AddToRoleAsync(user, roleName);
            
            if (result.Succeeded)
            {
                var newRoles = await GetUserRolesAsync(user);
                var newValue = string.Join(", ", newRoles);

                await LogAuditEventAsync(assignedByUserId, "AssignRole", "User", userId, oldValue, newValue);
                _logger.LogInformation("Role {RoleName} assigned to user {UserId} by {AssignedBy}", roleName, userId, assignedByUserId);
            }
            else
            {
                _logger.LogWarning("Failed to assign role {RoleName} to user {UserId}: {Errors}", 
                    roleName, userId, string.Join(", ", result.Errors.Select(e => e.Description)));
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning role {RoleName} to user {UserId}", roleName, userId);
            throw;
        }
    }

    public async Task<IdentityResult> RemoveRoleFromUserAsync(string userId, string roleName, string removedByUserId)
    {
        try
        {
            var user = await GetUserByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("Cannot remove role from user - user not found: {UserId}", userId);
                return IdentityResult.Failed(new IdentityError { Description = "User not found" });
            }

            var currentRoles = await GetUserRolesAsync(user);
            var oldValue = string.Join(", ", currentRoles);

            var result = await _userManager.RemoveFromRoleAsync(user, roleName);
            
            if (result.Succeeded)
            {
                var newRoles = await GetUserRolesAsync(user);
                var newValue = string.Join(", ", newRoles);

                await LogAuditEventAsync(removedByUserId, "RemoveRole", "User", userId, oldValue, newValue);
                _logger.LogInformation("Role {RoleName} removed from user {UserId} by {RemovedBy}", roleName, userId, removedByUserId);
            }
            else
            {
                _logger.LogWarning("Failed to remove role {RoleName} from user {UserId}: {Errors}", 
                    roleName, userId, string.Join(", ", result.Errors.Select(e => e.Description)));
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing role {RoleName} from user {UserId}", roleName, userId);
            throw;
        }
    }

    public async Task<IdentityResult> UpdateUserAsync(ApplicationUser user, string updatedByUserId)
    {
        try
        {
            var existingUser = await GetUserByIdAsync(user.Id);
            if (existingUser == null)
            {
                _logger.LogWarning("Cannot update user - user not found: {UserId}", user.Id);
                return IdentityResult.Failed(new IdentityError { Description = "User not found" });
            }

            var oldValue = JsonSerializer.Serialize(new
            {
                existingUser.FirstName,
                existingUser.LastName,
                existingUser.Email,
                existingUser.Organization
            });

            // Update properties
            existingUser.FirstName = user.FirstName;
            existingUser.LastName = user.LastName;
            existingUser.Organization = user.Organization;

            var result = await _userManager.UpdateAsync(existingUser);
            
            if (result.Succeeded)
            {
                var newValue = JsonSerializer.Serialize(new
                {
                    existingUser.FirstName,
                    existingUser.LastName,
                    existingUser.Email,
                    existingUser.Organization
                });

                await LogAuditEventAsync(updatedByUserId, "UpdateUser", "User", user.Id, oldValue, newValue);
                _logger.LogInformation("User {UserId} updated by {UpdatedBy}", user.Id, updatedByUserId);
            }
            else
            {
                _logger.LogWarning("Failed to update user {UserId}: {Errors}", 
                    user.Id, string.Join(", ", result.Errors.Select(e => e.Description)));
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user {UserId}", user.Id);
            throw;
        }
    }

    public async Task<IdentityResult> DeleteUserAsync(string userId, string deletedByUserId)
    {
        try
        {
            var user = await GetUserByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("Cannot delete user - user not found: {UserId}", userId);
                return IdentityResult.Failed(new IdentityError { Description = "User not found" });
            }

            // Soft delete by setting LockoutEnd to a future date
            user.LockoutEnd = DateTimeOffset.MaxValue;
            user.LockoutEnabled = true;

            var result = await _userManager.UpdateAsync(user);
            
            if (result.Succeeded)
            {
                await LogAuditEventAsync(deletedByUserId, "DeleteUser", "User", userId, "Active", "Deleted");
                _logger.LogInformation("User {UserId} deleted (soft delete) by {DeletedBy}", userId, deletedByUserId);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user {UserId}", userId);
            throw;
        }
    }

    public async Task<IdentityResult> SetUserEnabledAsync(string userId, bool enabled, string modifiedByUserId)
    {
        try
        {
            var user = await GetUserByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("Cannot modify user - user not found: {UserId}", userId);
                return IdentityResult.Failed(new IdentityError { Description = "User not found" });
            }

            var oldValue = user.LockoutEnd?.ToString() ?? "Enabled";

            if (enabled)
            {
                user.LockoutEnd = null;
            }
            else
            {
                user.LockoutEnd = DateTimeOffset.MaxValue;
                user.LockoutEnabled = true;
            }

            var result = await _userManager.UpdateAsync(user);
            
            if (result.Succeeded)
            {
                var newValue = enabled ? "Enabled" : "Disabled";
                await LogAuditEventAsync(modifiedByUserId, "SetUserEnabled", "User", userId, oldValue, newValue);
                _logger.LogInformation("User {UserId} {Status} by {ModifiedBy}", userId, newValue, modifiedByUserId);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting user enabled status for {UserId}", userId);
            throw;
        }
    }

    public async Task<(IdentityResult Result, string? NewPassword)> ResetUserPasswordAsync(string userId, string resetByUserId)
    {
        try
        {
            var user = await GetUserByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("Cannot reset password - user not found: {UserId}", userId);
                return (IdentityResult.Failed(new IdentityError { Description = "User not found" }), null);
            }

            // Generate a secure random password
            var newPassword = GenerateRandomPassword();
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
            
            if (result.Succeeded)
            {
                await LogAuditEventAsync(resetByUserId, "ResetPassword", "User", userId, "Password", "Password Reset");
                _logger.LogInformation("Password reset for user {UserId} by {ResetBy}", userId, resetByUserId);
                return (result, newPassword);
            }
            else
            {
                _logger.LogWarning("Failed to reset password for user {UserId}: {Errors}", 
                    userId, string.Join(", ", result.Errors.Select(e => e.Description)));
                return (result, null);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting password for user {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> HasPermissionAsync(string userId, string permission)
    {
        try
        {
            var permissions = await GetUserPermissionsAsync(userId);
            return permissions.Contains(permission);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking permission {Permission} for user {UserId}", permission, userId);
            throw;
        }
    }

    public async Task<IEnumerable<string>> GetUserPermissionsAsync(string userId)
    {
        try
        {
            var roles = await GetUserRolesAsync(userId);
            var permissions = new HashSet<string>();

            foreach (var role in roles)
            {
                var rolePermissions = Permissions.GetPermissionsForRole(role);
                foreach (var permission in rolePermissions)
                {
                    permissions.Add(permission);
                }
            }

            return permissions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving permissions for user {UserId}", userId);
            throw;
        }
    }

    public async Task LogAuditEventAsync(string userId, string action, string entityType, string? entityId = null, 
        string? oldValue = null, string? newValue = null, string? ipAddress = null, string? userAgent = null)
    {
        try
        {
            var auditLog = new AuditLog
            {
                UserId = userId,
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                OldValues = oldValue,
                NewValues = newValue,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                CreatedAt = DateTime.UtcNow,
                Success = true
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging audit event for user {UserId}, action {Action}", userId, action);
            // Don't throw - audit logging failure shouldn't break the main operation
        }
    }

    public async Task<IEnumerable<AuditLog>> GetUserAuditLogsAsync(string userId, int pageNumber = 1, int pageSize = 50)
    {
        try
        {
            return await _context.AuditLogs
                .Where(log => log.UserId == userId)
                .OrderByDescending(log => log.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Include(log => log.User)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving audit logs for user {UserId}", userId);
            throw;
        }
    }

    public async Task<IEnumerable<AuditLog>> GetAllAuditLogsAsync(int pageNumber = 1, int pageSize = 50)
    {
        try
        {
            return await _context.AuditLogs
                .OrderByDescending(log => log.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Include(log => log.User)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all audit logs");
            throw;
        }
    }

    private static string GenerateRandomPassword(int length = 12)
    {
        const string validChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";
        var password = new StringBuilder();
        
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[4];
        
        for (int i = 0; i < length; i++)
        {
            rng.GetBytes(bytes);
            var randomIndex = Math.Abs(BitConverter.ToInt32(bytes, 0)) % validChars.Length;
            password.Append(validChars[randomIndex]);
        }
        
        return password.ToString();
    }
}