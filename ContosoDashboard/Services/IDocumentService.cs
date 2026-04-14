using ContosoDashboard.Models;

namespace ContosoDashboard.Services;

/// <summary>
/// Service interface for document lifecycle operations, authorization, and file management.
/// </summary>
public interface IDocumentService
{
    /// <summary>
    /// Upload a new document with metadata and file content.
    /// </summary>
    Task<int> UploadDocumentAsync(int userId, DocumentUploadRequest request, Stream fileStream);

    /// <summary>
    /// Get documents for a user, applying ownership/share/project/task access rules.
    /// </summary>
    Task<IEnumerable<DocumentSummary>> GetDocumentsForUserAsync(int userId, DocumentQuery query);

    /// <summary>
    /// Get full details for a single document (authorization checked).
    /// </summary>
    Task<DocumentDetails> GetDocumentDetailsAsync(int documentId, int userId);

    /// <summary>
    /// Update document metadata (title, description, category, tags, project).
    /// </summary>
    Task UpdateDocumentMetadataAsync(int documentId, int userId, DocumentMetadataUpdate update);

    /// <summary>
    /// Replace the file content of a document, preserving metadata.
    /// </summary>
    Task ReplaceDocumentFileAsync(int documentId, int userId, Stream fileStream);

    /// <summary>
    /// Mark a document as deleted (soft-delete).
    /// </summary>
    Task DeleteDocumentAsync(int documentId, int userId);

    /// <summary>
    /// Share a document with specific recipients.
    /// </summary>
    Task ShareDocumentAsync(int documentId, int userId, DocumentShareRequest request);

    /// <summary>
    /// Revoke a document share.
    /// </summary>
    Task RevokeDocumentShareAsync(int documentShareId, int userId);

    /// <summary>
    /// Attach a document to a project.
    /// </summary>
    Task AttachDocumentToProjectAsync(int documentId, int projectId, int userId);

    /// <summary>
    /// Attach a document to a task.
    /// </summary>
    Task AttachDocumentToTaskAsync(int documentId, int taskId, int userId);

    /// <summary>
    /// Open a readable stream for a document (authorization checked).
    /// </summary>
    Task<Stream> OpenDocumentStreamAsync(int documentId, int userId);

    /// <summary>
    /// Get audit activity history for a document.
    /// </summary>
    Task<IEnumerable<DocumentActivityRecord>> GetDocumentActivityAsync(int documentId, int userId);

    /// <summary>
    /// Get available categories for document filtering (for authorized user).
    /// </summary>
    Task<IEnumerable<string>> GetAvailableCategoriesAsync(int userId);

    /// <summary>
    /// Get available projects for document filtering (user's projects only).
    /// </summary>
    Task<IEnumerable<(int ProjectId, string ProjectName)>> GetUserProjectsForDocumentsAsync(int userId);

    /// <summary>
    /// Get total document count for authorized user (for summary display).
    /// </summary>
    Task<int> GetDocumentCountForUserAsync(int userId);
}

/// <summary>
/// Request data for uploading a new document.
/// </summary>
public class DocumentUploadRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = string.Empty;
    public string? Tags { get; set; }
    public int? ProjectId { get; set; }
}

/// <summary>
/// Query parameters for fetching documents.
/// </summary>
public class DocumentQuery
{
    public string? SearchTerm { get; set; }
    public string? Category { get; set; }
    public int? ProjectId { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

/// <summary>
/// Summary data for document list display.
/// </summary>
public class DocumentSummary
{
    public int DocumentId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public DateTime UploadedDate { get; set; }
    public long FileSize { get; set; }
    public string? ProjectName { get; set; }
    public int OwnerUserId { get; set; }
}

/// <summary>
/// Full details for a single document.
/// </summary>
public class DocumentDetails
{
    public int DocumentId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = string.Empty;
    public string? Tags { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public DateTime UploadedDate { get; set; }
    public DateTime UpdatedDate { get; set; }
    public int OwnerUserId { get; set; }
    public string? ProjectName { get; set; }
    public int? ProjectId { get; set; }
}

/// <summary>
/// Request data for updating document metadata.
/// </summary>
public class DocumentMetadataUpdate
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Category { get; set; }
    public string? Tags { get; set; }
    public int? ProjectId { get; set; }
}

/// <summary>
/// Request data for sharing a document.
/// </summary>
public class DocumentShareRequest
{
    public List<int> RecipientUserIds { get; set; } = new();
    public bool CanEditMetadata { get; set; } = false;
    public string? Message { get; set; }
}

/// <summary>
/// Activity record for audit history.
/// </summary>
public class DocumentActivityRecord
{
    public int DocumentActivityId { get; set; }
    public string ActivityType { get; set; } = string.Empty;
    public string PerformedByUserName { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// Basic user information for share recipient selection.
/// </summary>
public class DocumentUserInfo
{
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
}
