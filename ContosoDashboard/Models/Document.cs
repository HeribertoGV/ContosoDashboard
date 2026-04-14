using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ContosoDashboard.Models;

/// <summary>
/// Represents an uploaded document with metadata and file information.
/// </summary>
public class Document
{
    [Key]
    public int DocumentId { get; set; }

    [Required]
    public int OwnerUserId { get; set; }

    [Required]
    [MaxLength(255)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; set; }

    [Required]
    [MaxLength(100)]
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Comma-separated tags for categorization and search.
    /// </summary>
    [MaxLength(500)]
    public string? Tags { get; set; }

    /// <summary>
    /// Optional project associated with the document.
    /// </summary>
    public int? ProjectId { get; set; }

    /// <summary>
    /// Original filename as uploaded by the user.
    /// </summary>
    [Required]
    [MaxLength(255)]
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// MIME type (content type) of the document.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// Size of the file in bytes.
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// Server-side file path for secure storage outside wwwroot.
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string StoragePath { get; set; } = string.Empty;

    public DateTime UploadedDate { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Soft-delete flag: when true, remove from user-facing lists but retain for audit.
    /// </summary>
    public bool IsDeleted { get; set; } = false;

    // Navigation properties
    [ForeignKey("OwnerUserId")]
    public virtual User OwnerUser { get; set; } = null!;

    [ForeignKey("ProjectId")]
    public virtual Project? Project { get; set; }

    public virtual ICollection<DocumentShare> DocumentShares { get; set; } = new List<DocumentShare>();
    public virtual ICollection<ProjectDocument> ProjectDocuments { get; set; } = new List<ProjectDocument>();
    public virtual ICollection<TaskDocument> TaskDocuments { get; set; } = new List<TaskDocument>();
    public virtual ICollection<DocumentActivity> DocumentActivities { get; set; } = new List<DocumentActivity>();
}
