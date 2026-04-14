using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ContosoDashboard.Models;

/// <summary>
/// Associates a document with a project for access control and project document views.
/// </summary>
public class ProjectDocument
{
    [Key]
    public int ProjectDocumentId { get; set; }

    [Required]
    public int DocumentId { get; set; }

    [Required]
    public int ProjectId { get; set; }

    [Required]
    public int AttachedByUserId { get; set; }

    public DateTime AttachedDate { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("DocumentId")]
    public virtual Document Document { get; set; } = null!;

    [ForeignKey("ProjectId")]
    public virtual Project Project { get; set; } = null!;

    [ForeignKey("AttachedByUserId")]
    public virtual User AttachedByUser { get; set; } = null!;
}
