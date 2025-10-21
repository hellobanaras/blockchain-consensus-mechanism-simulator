using System.ComponentModel.DataAnnotations;

namespace Consensus.Core.Entities;

/// <summary>
/// Audit log for tracking user actions
/// </summary>
public class AuditLog
{
    /// <summary>
    /// Unique identifier for the audit log entry
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// User who performed the action
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Username at the time of action
    /// </summary>
    [StringLength(256)]
    public string? UserName { get; set; }

    /// <summary>
    /// Action that was performed
    /// </summary>
    [Required]
    [StringLength(100)]
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Entity that was affected
    /// </summary>
    [StringLength(100)]
    public string? EntityType { get; set; }

    /// <summary>
    /// ID of the entity that was affected
    /// </summary>
    [StringLength(50)]
    public string? EntityId { get; set; }

    /// <summary>
    /// Description of the action
    /// </summary>
    [StringLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Old values (JSON) before the action
    /// </summary>
    public string? OldValues { get; set; }

    /// <summary>
    /// New values (JSON) after the action
    /// </summary>
    public string? NewValues { get; set; }

    /// <summary>
    /// IP address of the user
    /// </summary>
    [StringLength(45)]
    public string? IpAddress { get; set; }

    /// <summary>
    /// User agent string
    /// </summary>
    [StringLength(500)]
    public string? UserAgent { get; set; }

    /// <summary>
    /// When the action occurred
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Success or failure of the action
    /// </summary>
    public bool Success { get; set; } = true;

    /// <summary>
    /// Error message if action failed
    /// </summary>
    [StringLength(1000)]
    public string? ErrorMessage { get; set; }

    // Navigation properties
    public virtual ApplicationUser? User { get; set; }
}