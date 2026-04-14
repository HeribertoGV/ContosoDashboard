using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ContosoDashboard.Models;

/// <summary>
/// Captures audit events for document lifecycle operations.
/// </summary>
public class DocumentActivity
{
    [Key]
    public int DocumentActivityId { get; set; }

    [Required]
    public int DocumentId { get; set; }

    [Required]
    [MaxLength(50)]
    public string ActivityType { get; set; } = string.Empty;

    [Required]
    public int PerformedByUserId { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [MaxLength(500)]
    public string? Notes { get; set; }

    // Navigation properties
    [ForeignKey("DocumentId")]
    public virtual Document Document { get; set; } = null!;

    [ForeignKey("PerformedByUserId")]
    public virtual User PerformedByUser { get; set; } = null!;
}

/// <summary>
/// Enumeration of document activity types for type-safe logging.
/// </summary>
public enum DocumentActivityType
{
    Upload,
    MetadataEdit,
    ReplaceFile,
    Share,
    RevokeShare,
    AttachProject,
    AttachTask,
    Delete,
    Download
}
