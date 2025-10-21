using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Consensus.Core.Entities;

/// <summary>
/// Application user entity extending IdentityUser
/// </summary>
public class ApplicationUser : IdentityUser
{
    /// <summary>
    /// User's first name
    /// </summary>
    [StringLength(50)]
    public string? FirstName { get; set; }

    /// <summary>
    /// User's last name
    /// </summary>
    [StringLength(50)]
    public string? LastName { get; set; }

    /// <summary>
    /// User's full display name
    /// </summary>
    public string DisplayName => $"{FirstName} {LastName}".Trim();

    /// <summary>
    /// When the user was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the user last logged in
    /// </summary>
    public DateTime? LastLoginAt { get; set; }

    /// <summary>
    /// Whether the user account is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// User preferences as JSON
    /// </summary>
    public string? Preferences { get; set; }

    /// <summary>
    /// User's organization or department
    /// </summary>
    [StringLength(100)]
    public string? Organization { get; set; }

    /// <summary>
    /// User's job title
    /// </summary>
    [StringLength(100)]
    public string? JobTitle { get; set; }

    // Navigation properties for audit trails
    public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
    public virtual ICollection<SimulationRun> SimulationRuns { get; set; } = new List<SimulationRun>();
}

/// <summary>
/// Application role entity extending IdentityRole
/// </summary>
public class ApplicationRole : IdentityRole
{
    /// <summary>
    /// Role description
    /// </summary>
    [StringLength(200)]
    public string? Description { get; set; }

    /// <summary>
    /// When the role was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Whether the role is system-defined (cannot be deleted)
    /// </summary>
    public bool IsSystemRole { get; set; }

    /// <summary>
    /// Permissions associated with this role as JSON
    /// </summary>
    public string? Permissions { get; set; }
}