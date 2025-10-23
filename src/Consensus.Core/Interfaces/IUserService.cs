using Consensus.Core.Entities;
using Microsoft.AspNetCore.Identity;

namespace Consensus.Core.Interfaces;

/// <summary>
/// Service interface for user management operations
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Get all users with their roles
    /// </summary>
    Task<IEnumerable<ApplicationUser>> GetAllUsersAsync();

    /// <summary>
    /// Get a user by ID with roles
    /// </summary>
    Task<ApplicationUser?> GetUserByIdAsync(string userId);

    /// <summary>
    /// Get a user by email
    /// </summary>
    Task<ApplicationUser?> GetUserByEmailAsync(string email);

    /// <summary>
    /// Get all available roles
    /// </summary>
    Task<IEnumerable<ApplicationRole>> GetAllRolesAsync();

    /// <summary>
    /// Get roles assigned to a user
    /// </summary>
    Task<IList<string>> GetUserRolesAsync(ApplicationUser user);

    /// <summary>
    /// Get roles assigned to a user by user ID
    /// </summary>
    Task<IList<string>> GetUserRolesAsync(string userId);

    /// <summary>
    /// Assign a role to a user
    /// </summary>
    Task<IdentityResult> AssignRoleToUserAsync(string userId, string roleName, string assignedByUserId);

    /// <summary>
    /// Remove a role from a user
    /// </summary>
    Task<IdentityResult> RemoveRoleFromUserAsync(string userId, string roleName, string removedByUserId);

    /// <summary>
    /// Update user information
    /// </summary>
    Task<IdentityResult> UpdateUserAsync(ApplicationUser user, string updatedByUserId);

    /// <summary>
    /// Delete a user (soft delete by deactivating)
    /// </summary>
    Task<IdentityResult> DeleteUserAsync(string userId, string deletedByUserId);

    /// <summary>
    /// Enable or disable a user account
    /// </summary>
    Task<IdentityResult> SetUserEnabledAsync(string userId, bool enabled, string modifiedByUserId);

    /// <summary>
    /// Reset user password (admin only)
    /// </summary>
    Task<(IdentityResult Result, string? NewPassword)> ResetUserPasswordAsync(string userId, string resetByUserId);

    /// <summary>
    /// Check if user has a specific permission
    /// </summary>
    Task<bool> HasPermissionAsync(string userId, string permission);

    /// <summary>
    /// Get user permissions based on roles
    /// </summary>
    Task<IEnumerable<string>> GetUserPermissionsAsync(string userId);

    /// <summary>
    /// Log audit event for user actions
    /// </summary>
    Task LogAuditEventAsync(string userId, string action, string entityType, string? entityId = null, 
        string? oldValue = null, string? newValue = null, string? ipAddress = null, string? userAgent = null);

    /// <summary>
    /// Get audit logs for a user
    /// </summary>
    Task<IEnumerable<AuditLog>> GetUserAuditLogsAsync(string userId, int pageNumber = 1, int pageSize = 50);

    /// <summary>
    /// Get all audit logs (admin only)
    /// </summary>
    Task<IEnumerable<AuditLog>> GetAllAuditLogsAsync(int pageNumber = 1, int pageSize = 50);
}