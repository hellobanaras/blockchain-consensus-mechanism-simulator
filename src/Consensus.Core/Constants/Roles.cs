namespace Consensus.Core.Constants;

/// <summary>
/// Application role constants
/// </summary>
public static class Roles
{
    /// <summary>
    /// Viewer role - read-only access
    /// </summary>
    public const string Viewer = "Viewer";
    
    /// <summary>
    /// Operator role - can run simulations and manage configurations
    /// </summary>
    public const string Operator = "Operator";
    
    /// <summary>
    /// Admin role - full system access including user management
    /// </summary>
    public const string Admin = "Admin";

    /// <summary>
    /// All available roles
    /// </summary>
    public static readonly string[] All = { Viewer, Operator, Admin };

    /// <summary>
    /// Roles that can run simulations
    /// </summary>
    public static readonly string[] CanRunSimulations = { Operator, Admin };

    /// <summary>
    /// Roles that can manage users
    /// </summary>
    public static readonly string[] CanManageUsers = { Admin };

    /// <summary>
    /// Roles that have read access
    /// </summary>
    public static readonly string[] CanRead = { Viewer, Operator, Admin };
}

/// <summary>
/// Application permissions
/// </summary>
public static class Permissions
{
    // Simulation permissions
    public const string ViewSimulations = "simulations.view";
    public const string CreateSimulations = "simulations.create";
    public const string UpdateSimulations = "simulations.update";
    public const string DeleteSimulations = "simulations.delete";
    public const string StartSimulations = "simulations.start";
    public const string StopSimulations = "simulations.stop";

    // Analytics permissions
    public const string ViewAnalytics = "analytics.view";
    public const string ExportAnalytics = "analytics.export";

    // Block explorer permissions
    public const string ViewBlocks = "blocks.view";
    public const string SearchBlocks = "blocks.search";

    // Protocol playground permissions
    public const string ViewPlayground = "playground.view";
    public const string UpdatePlayground = "playground.update";

    // User management permissions
    public const string ViewUsers = "users.view";
    public const string CreateUsers = "users.create";
    public const string UpdateUsers = "users.update";
    public const string DeleteUsers = "users.delete";
    public const string AssignRoles = "users.assign_roles";

    // Audit permissions
    public const string ViewAuditLogs = "audit.view";

    /// <summary>
    /// Default permissions for Viewer role
    /// </summary>
    public static readonly string[] ViewerPermissions = {
        ViewSimulations,
        ViewAnalytics,
        ViewBlocks,
        SearchBlocks,
        ViewPlayground
    };

    /// <summary>
    /// Default permissions for Operator role (includes Viewer permissions)
    /// </summary>
    public static readonly string[] OperatorPermissions = ViewerPermissions.Concat(new[] {
        CreateSimulations,
        UpdateSimulations,
        StartSimulations,
        StopSimulations,
        ExportAnalytics,
        UpdatePlayground
    }).ToArray();

    /// <summary>
    /// Default permissions for Admin role (includes all permissions)
    /// </summary>
    public static readonly string[] AdminPermissions = OperatorPermissions.Concat(new[] {
        DeleteSimulations,
        ViewUsers,
        CreateUsers,
        UpdateUsers,
        DeleteUsers,
        AssignRoles,
        ViewAuditLogs
    }).ToArray();

    /// <summary>
    /// Get permissions for a role
    /// </summary>
    public static string[] GetPermissionsForRole(string role)
    {
        return role switch
        {
            Roles.Viewer => ViewerPermissions,
            Roles.Operator => OperatorPermissions,
            Roles.Admin => AdminPermissions,
            _ => Array.Empty<string>()
        };
    }
}