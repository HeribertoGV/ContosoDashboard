using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ContosoDashboard.Models;

/// <summary>
/// Represents an explicit share of a document with a specific recipient.
/// </summary>
public class DocumentShare
{
    [Key]
    public int DocumentShareId { get; set; }

    [Required]
    public int DocumentId { get; set; }

    [Required]
    public int RecipientUserId { get; set; }

    [Required]
    public int SharedByUserId { get; set; }

    public DateTime SharedDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Whether the recipient can edit document metadata.
    /// </summary>
    public bool CanEditMetadata { get; set; } = false;

    /// <summary>
    /// Whether the share is currently active (not revoked).
    /// </summary>
    public bool IsActive { get; set; } = true;

    // Navigation properties
    [ForeignKey("DocumentId")]
    public virtual Document Document { get; set; } = null!;

    [ForeignKey("RecipientUserId")]
    public virtual User RecipientUser { get; set; } = null!;

    [ForeignKey("SharedByUserId")]
    public virtual User SharedByUser { get; set; } = null!;
}
