using Consensus.Core.Constants;
using Consensus.Core.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Consensus.Data.Seed;

/// <summary>
/// Database seeder for Identity roles and default admin user
/// </summary>
public class IdentitySeeder
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<IdentitySeeder> _logger;

    public IdentitySeeder(IServiceProvider serviceProvider, ILogger<IdentitySeeder> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// Seed roles and default admin user
    /// </summary>
    public async Task SeedAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        try
        {
            await SeedRolesAsync(roleManager);
            await SeedDefaultAdminUserAsync(userManager, roleManager);
            _logger.LogInformation("Identity seeding completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during Identity seeding");
            throw;
        }
    }

    /// <summary>
    /// Seed default roles (Viewer, Operator, Admin)
    /// </summary>
    private async Task SeedRolesAsync(RoleManager<ApplicationRole> roleManager)
    {
        var rolesToSeed = new[]
        {
            new { Name = Roles.Viewer, Description = "Read-only access to simulations, analytics, and block explorer", Permissions = Permissions.ViewerPermissions },
            new { Name = Roles.Operator, Description = "Can run simulations, export data, and modify protocol settings", Permissions = Permissions.OperatorPermissions },
            new { Name = Roles.Admin, Description = "Full system access including user management and system administration", Permissions = Permissions.AdminPermissions }
        };

        foreach (var roleInfo in rolesToSeed)
        {
            var existingRole = await roleManager.FindByNameAsync(roleInfo.Name);
            if (existingRole == null)
            {
                var role = new ApplicationRole
                {
                    Name = roleInfo.Name,
                    Description = roleInfo.Description,
                    IsSystemRole = true,
                    CreatedAt = DateTime.UtcNow,
                    Permissions = string.Join(",", roleInfo.Permissions)
                };

                var result = await roleManager.CreateAsync(role);
                if (result.Succeeded)
                {
                    _logger.LogInformation("Created role: {RoleName}", roleInfo.Name);
                    
                    // Add permission claims to the role
                    foreach (var permission in roleInfo.Permissions)
                    {
                        await roleManager.AddClaimAsync(role, new System.Security.Claims.Claim("permission", permission));
                    }
                    
                    _logger.LogInformation("Added {PermissionCount} permissions to role {RoleName}", roleInfo.Permissions.Length, roleInfo.Name);
                }
                else
                {
                    _logger.LogError("Failed to create role {RoleName}: {Errors}", roleInfo.Name, 
                        string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
            else
            {
                _logger.LogInformation("Role {RoleName} already exists, skipping creation", roleInfo.Name);
                
                // Update permissions for existing roles
                await UpdateRolePermissionsAsync(roleManager, existingRole, roleInfo.Permissions);
            }
        }
    }

    /// <summary>
    /// Update permissions for existing roles
    /// </summary>
    private async Task UpdateRolePermissionsAsync(RoleManager<ApplicationRole> roleManager, ApplicationRole role, string[] permissions)
    {
        // Get current permission claims
        var currentClaims = await roleManager.GetClaimsAsync(role);
        var currentPermissions = currentClaims.Where(c => c.Type == "permission").Select(c => c.Value).ToHashSet();
        var newPermissions = permissions.ToHashSet();

        // Remove permissions that are no longer needed
        foreach (var permission in currentPermissions.Except(newPermissions))
        {
            await roleManager.RemoveClaimAsync(role, new System.Security.Claims.Claim("permission", permission));
            _logger.LogDebug("Removed permission {Permission} from role {RoleName}", permission, role.Name);
        }

        // Add new permissions
        foreach (var permission in newPermissions.Except(currentPermissions))
        {
            await roleManager.AddClaimAsync(role, new System.Security.Claims.Claim("permission", permission));
            _logger.LogDebug("Added permission {Permission} to role {RoleName}", permission, role.Name);
        }

        // Update the role's permission string
        role.Permissions = string.Join(",", permissions);
        await roleManager.UpdateAsync(role);

        if (currentPermissions.Count != newPermissions.Count || !currentPermissions.SetEquals(newPermissions))
        {
            _logger.LogInformation("Updated permissions for role {RoleName}", role.Name);
        }
    }

    /// <summary>
    /// Seed default admin user
    /// </summary>
    private async Task SeedDefaultAdminUserAsync(UserManager<ApplicationUser> userManager, RoleManager<ApplicationRole> roleManager)
    {
        const string defaultAdminEmail = "admin@consensus-lab.dev";
        const string defaultAdminPassword = "Admin@123!";

        var existingAdmin = await userManager.FindByEmailAsync(defaultAdminEmail);
        if (existingAdmin == null)
        {
            var adminUser = new ApplicationUser
            {
                UserName = defaultAdminEmail,
                Email = defaultAdminEmail,
                EmailConfirmed = true,
                FirstName = "System",
                LastName = "Administrator",
                Organization = "Consensus Lab",
                JobTitle = "System Administrator",
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            var result = await userManager.CreateAsync(adminUser, defaultAdminPassword);
            if (result.Succeeded)
            {
                _logger.LogInformation("Created default admin user: {Email}", defaultAdminEmail);

                // Assign Admin role
                var roleResult = await userManager.AddToRoleAsync(adminUser, Roles.Admin);
                if (roleResult.Succeeded)
                {
                    _logger.LogInformation("Assigned Admin role to default admin user");
                }
                else
                {
                    _logger.LogError("Failed to assign Admin role to default admin user: {Errors}", 
                        string.Join(", ", roleResult.Errors.Select(e => e.Description)));
                }

                _logger.LogWarning("Default admin user created with email: {Email} and password: {Password}. " +
                    "Please change the password after first login!", defaultAdminEmail, defaultAdminPassword);
            }
            else
            {
                _logger.LogError("Failed to create default admin user: {Errors}", 
                    string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
        else
        {
            _logger.LogInformation("Default admin user already exists: {Email}", defaultAdminEmail);
            
            // Ensure admin user has the Admin role
            if (!await userManager.IsInRoleAsync(existingAdmin, Roles.Admin))
            {
                var roleResult = await userManager.AddToRoleAsync(existingAdmin, Roles.Admin);
                if (roleResult.Succeeded)
                {
                    _logger.LogInformation("Added Admin role to existing admin user");
                }
                else
                {
                    _logger.LogError("Failed to add Admin role to existing admin user: {Errors}", 
                        string.Join(", ", roleResult.Errors.Select(e => e.Description)));
                }
            }
        }
    }
}