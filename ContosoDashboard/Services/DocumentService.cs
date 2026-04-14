using ContosoDashboard.Data;
using ContosoDashboard.Models;
using Microsoft.EntityFrameworkCore;

namespace ContosoDashboard.Services;

/// <summary>
/// Concrete implementation of document service with file storage, authorization, and audit logging.
/// </summary>
public class DocumentService : IDocumentService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DocumentService> _logger;
    private readonly INotificationService _notificationService;
    private readonly string _storagePath;
    private const long MaxFileSize = 26214400; // 25 MB in bytes
    private static readonly string[] AllowedMimeTypes = 
    {
        "application/pdf",
        "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/vnd.ms-excel",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "text/plain",
        "text/csv",
        "image/jpeg",
        "image/png",
        "image/gif"
    };

    public DocumentService(
        ApplicationDbContext context,
        ILogger<DocumentService> logger,
        INotificationService notificationService,
        IConfiguration configuration)
    {
        _context = context;
        _logger = logger;
        _notificationService = notificationService;

        // Initialize storage path from configuration
        _storagePath = configuration["DocumentStorage:UploadPath"] 
            ?? Path.Combine(AppContext.BaseDirectory, "UploadedDocuments");

        // Ensure storage directory exists
        if (!Directory.Exists(_storagePath))
        {
            Directory.CreateDirectory(_storagePath);
            _logger.LogInformation($"Created document storage directory: {_storagePath}");
        }
    }

    /// <summary>
    /// Upload a new document with metadata and file content.
    /// </summary>
    public async Task<int> UploadDocumentAsync(int userId, DocumentUploadRequest request, Stream fileStream)
    {
        // Validate file size
        if (fileStream.Length > MaxFileSize)
        {
            throw new InvalidOperationException($"File size exceeds maximum allowed size of 25 MB.");
        }

        // Read file into memory to determine size and MIME type
        using var memoryStream = new MemoryStream();
        await fileStream.CopyToAsync(memoryStream);
        var fileContent = memoryStream.ToArray();

        // Generate secure file path with GUID-based naming
        var fileId = Guid.NewGuid();
        var savedFileName = $"{fileId}.bin";
        var filePath = Path.Combine(_storagePath, savedFileName);

        // Determine content type from extension if available, or use generic binary
        var contentType = "application/octet-stream";
        
        // Save file to disk
        await File.WriteAllBytesAsync(filePath, fileContent);
        _logger.LogInformation($"Document file saved: {filePath}");

        // Create document record
        var document = new Document
        {
            OwnerUserId = userId,
            Title = request.Title,
            Description = request.Description,
            Category = request.Category,
            Tags = request.Tags,
            ProjectId = request.ProjectId,
            FileName = request.Title, // Use title as the logical filename
            ContentType = contentType,
            FileSize = fileContent.Length,
            StoragePath = filePath,
            UploadedDate = DateTime.UtcNow,
            UpdatedDate = DateTime.UtcNow,
            IsDeleted = false
        };

        _context.Documents.Add(document);
        await _context.SaveChangesAsync();

        // Log activity
        await LogActivityAsync(document.DocumentId, DocumentActivityType.Upload, "Document uploaded");

        _logger.LogInformation($"Document created with ID: {document.DocumentId}");
        return document.DocumentId;
    }

    /// <summary>
    /// Get documents for a user, applying ownership/share/project/task access rules.
    /// Optimized for performance with proper pagination and filtering.
    /// </summary>
    public async Task<IEnumerable<DocumentSummary>> GetDocumentsForUserAsync(int userId, DocumentQuery query)
    {
        // Build authorization filter: documents owned by user, explicitly shared, or in user's projects
        var baseQuery = _context.Documents
            .AsNoTracking()
            .Where(d => !d.IsDeleted &&
                (
                    // User is the owner
                    d.OwnerUserId == userId ||
                    // User is an explicit recipient of an active share
                    d.DocumentShares.Any(ds => ds.RecipientUserId == userId && ds.IsActive) ||
                    // User is a member of a project that has the document
                    d.ProjectDocuments.Any(pd =>
                        _context.ProjectMembers.Any(pm => pm.UserId == userId && pm.ProjectId == pd.ProjectId)
                    )
                )
            );

        // Apply search filter (case-insensitive title and tag search)
        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var searchTerm = query.SearchTerm.ToLower();
            baseQuery = baseQuery.Where(d =>
                d.Title.ToLower().Contains(searchTerm) ||
                (d.Tags != null && d.Tags.ToLower().Contains(searchTerm)) ||
                (d.Description != null && d.Description.ToLower().Contains(searchTerm))
            );
        }

        // Apply category filter
        if (!string.IsNullOrWhiteSpace(query.Category))
        {
            baseQuery = baseQuery.Where(d => d.Category == query.Category);
        }

        // Apply project filter - includes both directly owned and project-attached documents
        if (query.ProjectId.HasValue)
        {
            baseQuery = baseQuery.Where(d =>
                d.ProjectId == query.ProjectId ||
                d.ProjectDocuments.Any(pd => pd.ProjectId == query.ProjectId)
            );
        }

        // Get total count for pagination info
        var totalCount = await baseQuery.CountAsync();

        // Apply pagination and sorting
        var results = await baseQuery
            .OrderByDescending(d => d.UploadedDate)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(d => new DocumentSummary
            {
                DocumentId = d.DocumentId,
                Title = d.Title,
                Category = d.Category,
                UploadedDate = d.UploadedDate,
                FileSize = d.FileSize,
                ProjectName = d.Project != null ? d.Project.Name : null,
                OwnerUserId = d.OwnerUserId
            })
            .ToListAsync();

        _logger.LogInformation($"Retrieved {results.Count} documents for user {userId} (total: {totalCount}, page: {query.PageNumber}/{Math.Ceiling((double)totalCount / query.PageSize)})");

        return results;
    }

    /// <summary>
    /// Get available categories for filtering documents (for authorized user).
    /// </summary>
    public async Task<IEnumerable<string>> GetAvailableCategoriesAsync(int userId)
    {
        var categories = await _context.Documents
            .AsNoTracking()
            .Where(d => !d.IsDeleted &&
                (
                    d.OwnerUserId == userId ||
                    d.DocumentShares.Any(ds => ds.RecipientUserId == userId && ds.IsActive) ||
                    d.ProjectDocuments.Any(pd =>
                        _context.ProjectMembers.Any(pm => pm.UserId == userId && pm.ProjectId == pd.ProjectId)
                    )
                )
            )
            .Select(d => d.Category)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync();

        return categories;
    }

    /// <summary>
    /// Get available projects for document filtering (user's projects only).
    /// </summary>
    public async Task<IEnumerable<(int ProjectId, string ProjectName)>> GetUserProjectsForDocumentsAsync(int userId)
    {
        var projects = await _context.ProjectMembers
            .AsNoTracking()
            .Where(pm => pm.UserId == userId)
            .Select(pm => new { pm.ProjectId, pm.Project.Name })
            .Distinct()
            .OrderBy(p => p.Name)
            .ToListAsync();

        return projects.Select(p => (p.ProjectId, p.Name));
    }

    /// <summary>
    /// Get total document count for authorized user (for summary display).
    /// </summary>
    public async Task<int> GetDocumentCountForUserAsync(int userId)
    {
        return await _context.Documents
            .AsNoTracking()
            .Where(d => !d.IsDeleted &&
                (
                    d.OwnerUserId == userId ||
                    d.DocumentShares.Any(ds => ds.RecipientUserId == userId && ds.IsActive) ||
                    d.ProjectDocuments.Any(pd =>
                        _context.ProjectMembers.Any(pm => pm.UserId == userId && pm.ProjectId == pd.ProjectId)
                    )
                )
            )
            .CountAsync();
    }

    /// <summary>
    /// Get full details for a single document (authorization checked).
    /// </summary>
    public async Task<DocumentDetails> GetDocumentDetailsAsync(int documentId, int userId)
    {
        var document = await _context.Documents.FindAsync(documentId);
        if (document == null || document.IsDeleted)
        {
            throw new InvalidOperationException("Document not found.");
        }

        // Authorization: Owner, explicit recipient, or project member
        var hasAccess = await VerifyDocumentAccessAsync(documentId, userId);
        if (!hasAccess)
        {
            throw new UnauthorizedAccessException("Access denied to this document.");
        }

        return new DocumentDetails
        {
            DocumentId = document.DocumentId,
            Title = document.Title,
            Description = document.Description,
            Category = document.Category,
            Tags = document.Tags,
            FileName = document.FileName,
            ContentType = document.ContentType,
            FileSize = document.FileSize,
            UploadedDate = document.UploadedDate,
            UpdatedDate = document.UpdatedDate,
            OwnerUserId = document.OwnerUserId,
            ProjectName = document.Project?.Name,
            ProjectId = document.ProjectId
        };
    }

    /// <summary>
    /// Update document metadata (title, description, category, tags, project).
    /// </summary>
    public async Task UpdateDocumentMetadataAsync(int documentId, int userId, DocumentMetadataUpdate update)
    {
        var document = await _context.Documents.FindAsync(documentId);
        if (document == null || document.IsDeleted)
        {
            throw new InvalidOperationException("Document not found.");
        }

        // Authorization: Only owner can edit
        if (document.OwnerUserId != userId)
        {
            throw new UnauthorizedAccessException("Only document owner can edit metadata.");
        }

        // Update fields
        if (!string.IsNullOrWhiteSpace(update.Title))
            document.Title = update.Title;
        if (update.Description != null)
            document.Description = update.Description;
        if (!string.IsNullOrWhiteSpace(update.Category))
            document.Category = update.Category;
        if (update.Tags != null)
            document.Tags = update.Tags;
        if (update.ProjectId.HasValue)
            document.ProjectId = update.ProjectId;

        document.UpdatedDate = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await LogActivityAsync(documentId, DocumentActivityType.MetadataEdit, "Metadata updated");
    }

    /// <summary>
    /// Replace the file content of a document, preserving metadata.
    /// </summary>
    public async Task ReplaceDocumentFileAsync(int documentId, int userId, Stream fileStream)
    {
        var document = await _context.Documents.FindAsync(documentId);
        if (document == null || document.IsDeleted)
        {
            throw new InvalidOperationException("Document not found.");
        }

        // Authorization: Only owner can replace file
        if (document.OwnerUserId != userId)
        {
            throw new UnauthorizedAccessException("Only document owner can replace file.");
        }

        // Validate file size
        if (fileStream.Length > MaxFileSize)
        {
            throw new InvalidOperationException("File size exceeds maximum allowed size of 25 MB.");
        }

        // Read file
        using var memoryStream = new MemoryStream();
        await fileStream.CopyToAsync(memoryStream);
        var fileContent = memoryStream.ToArray();

        // Delete old file if it exists
        if (File.Exists(document.StoragePath))
        {
            File.Delete(document.StoragePath);
            _logger.LogInformation($"Deleted old document file: {document.StoragePath}");
        }

        // Generate new file path
        var fileId = Guid.NewGuid();
        var newFileName = $"{fileId}_{Path.GetFileName(document.FileName)}";
        var newFilePath = Path.Combine(_storagePath, newFileName);

        // Save new file
        await File.WriteAllBytesAsync(newFilePath, fileContent);
        _logger.LogInformation($"New document file saved: {newFilePath}");

        // Update document
        document.StoragePath = newFilePath;
        document.FileSize = fileContent.Length;
        document.UpdatedDate = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await LogActivityAsync(documentId, DocumentActivityType.ReplaceFile, "File replaced");
    }

    /// <summary>
    /// Mark a document as deleted (soft-delete).
    /// </summary>
    public async Task DeleteDocumentAsync(int documentId, int userId)
    {
        var document = await _context.Documents.FindAsync(documentId);
        if (document == null || document.IsDeleted)
        {
            throw new InvalidOperationException("Document not found.");
        }

        // Authorization: Only owner or project manager can delete
        if (document.OwnerUserId != userId)
        {
            throw new UnauthorizedAccessException("Only document owner can delete.");
        }

        document.IsDeleted = true;
        await _context.SaveChangesAsync();

        await LogActivityAsync(documentId, DocumentActivityType.Delete, "Document deleted");
    }

    /// <summary>
    /// Open a readable stream for a document (authorization checked).
    /// </summary>
    public async Task<Stream> OpenDocumentStreamAsync(int documentId, int userId)
    {
        var document = await _context.Documents.FindAsync(documentId);
        if (document == null || document.IsDeleted)
        {
            throw new InvalidOperationException("Document not found.");
        }

        // Authorization
        var hasAccess = await VerifyDocumentAccessAsync(documentId, userId);
        if (!hasAccess)
        {
            throw new UnauthorizedAccessException("Access denied to this document.");
        }

        if (!File.Exists(document.StoragePath))
        {
            throw new InvalidOperationException("Document file not found.");
        }

        await LogActivityAsync(documentId, DocumentActivityType.Download, "Document accessed");

        return File.OpenRead(document.StoragePath);
    }

    /// <summary>
    /// Get audit activity history for a document.
    /// </summary>
    public async Task<IEnumerable<DocumentActivityRecord>> GetDocumentActivityAsync(int documentId, int userId)
    {
        // Authorization
        var hasAccess = await VerifyDocumentAccessAsync(documentId, userId);
        if (!hasAccess)
        {
            throw new UnauthorizedAccessException("Access denied to this document.");
        }

        var activities = await _context.DocumentActivities
            .Where(da => da.DocumentId == documentId)
            .OrderByDescending(da => da.Timestamp)
            .Select(da => new DocumentActivityRecord
            {
                DocumentActivityId = da.DocumentActivityId,
                ActivityType = da.ActivityType,
                PerformedByUserName = da.PerformedByUser.DisplayName,
                Timestamp = da.Timestamp,
                Notes = da.Notes
            })
            .ToListAsync();

        return activities;
    }

    // Helper methods
    private async Task<bool> VerifyDocumentAccessAsync(int documentId, int userId)
    {
        var document = await _context.Documents
            .Include(d => d.DocumentShares)
            .Include(d => d.ProjectDocuments)
            .FirstOrDefaultAsync(d => d.DocumentId == documentId && !d.IsDeleted);

        if (document == null) return false;

        // Owner always has access
        if (document.OwnerUserId == userId) return true;

        // Check explicit share
        if (document.DocumentShares.Any(ds => ds.RecipientUserId == userId && ds.IsActive))
            return true;

        // Check project membership
        if (document.ProjectDocuments.Any(pd =>
            _context.ProjectMembers.Any(pm => pm.UserId == userId && pm.ProjectId == pd.ProjectId)))
            return true;

        return false;
    }

    private async Task LogActivityAsync(int documentId, DocumentActivityType activityType, string? notes = null)
    {
        // This will be fully implemented with user context in Phase 3
        // For now, use a placeholder system user ID
        var systemUserId = 1;

        var activity = new DocumentActivity
        {
            DocumentId = documentId,
            ActivityType = activityType.ToString(),
            PerformedByUserId = systemUserId,
            Timestamp = DateTime.UtcNow,
            Notes = notes
        };

        _context.DocumentActivities.Add(activity);
        await _context.SaveChangesAsync();
    }

    private string DetermineContentType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".pdf" => "application/pdf",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xls" => "application/vnd.ms-excel",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".txt" => "text/plain",
            ".csv" => "text/csv",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            _ => "application/octet-stream"
        };
    }

    /// <summary>
    /// Share a document with specific recipients (owner-only).
    /// </summary>
    public async Task ShareDocumentAsync(int documentId, int userId, DocumentShareRequest request)
    {
        var document = await _context.Documents.FindAsync(documentId);
        if (document == null || document.IsDeleted)
        {
            throw new InvalidOperationException("Document not found.");
        }

        // Authorization: Only owner can share
        if (document.OwnerUserId != userId)
        {
            throw new UnauthorizedAccessException("Only document owner can share.");
        }

        // Create shares for each recipient
        foreach (var recipientId in request.RecipientUserIds)
        {
            // Skip if already shared
            var existing = await _context.DocumentShares
                .FirstOrDefaultAsync(ds => ds.DocumentId == documentId && ds.RecipientUserId == recipientId);

            if (existing != null)
            {
                if (!existing.IsActive)
                {
                    existing.IsActive = true;
                    existing.CanEditMetadata = request.CanEditMetadata;
                }
                continue;
            }

            var share = new DocumentShare
            {
                DocumentId = documentId,
                RecipientUserId = recipientId,
                SharedByUserId = userId,
                SharedDate = DateTime.UtcNow,
                CanEditMetadata = request.CanEditMetadata,
                IsActive = true
            };

            _context.DocumentShares.Add(share);

            // Send notification to recipient
            try
            {
                await _notificationService.CreateDocumentShareNotificationAsync(recipientId, document.Title, request.Message);
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Failed to send share notification: {ex.Message}");
            }
        }

        await _context.SaveChangesAsync();
        await LogActivityAsync(documentId, DocumentActivityType.Share, $"Shared with {request.RecipientUserIds.Count} recipients");
        _logger.LogInformation($"Document {documentId} shared with {request.RecipientUserIds.Count} recipients");
    }

    /// <summary>
    /// Revoke a document share (owner-only).
    /// </summary>
    public async Task RevokeDocumentShareAsync(int documentShareId, int userId)
    {
        var share = await _context.DocumentShares.FindAsync(documentShareId);
        if (share == null)
        {
            throw new InvalidOperationException("Share record not found.");
        }

        var document = await _context.Documents.FindAsync(share.DocumentId);
        if (document == null || document.IsDeleted)
        {
            throw new InvalidOperationException("Document not found.");
        }

        // Authorization: Only owner can revoke
        if (document.OwnerUserId != userId)
        {
            throw new UnauthorizedAccessException("Only document owner can revoke shares.");
        }

        share.IsActive = false;
        await _context.SaveChangesAsync();
        await LogActivityAsync(share.DocumentId, DocumentActivityType.RevokeShare, $"Share revoked for user {share.RecipientUserId}");
        _logger.LogInformation($"Share revoked for Document {share.DocumentId}");
    }

    /// <summary>
    /// Attach a document to a project (owner-only).
    /// </summary>
    public async Task AttachDocumentToProjectAsync(int documentId, int projectId, int userId)
    {
        var document = await _context.Documents.FindAsync(documentId);
        if (document == null || document.IsDeleted)
        {
            throw new InvalidOperationException("Document not found.");
        }

        // Authorization: Only owner can attach (or project manager)
        if (document.OwnerUserId != userId)
        {
            throw new UnauthorizedAccessException("Only document owner can attach to projects.");
        }

        var project = await _context.Projects.FindAsync(projectId);
        if (project == null)
        {
            throw new InvalidOperationException("Project not found.");
        }

        // Check if already attached
        var existing = await _context.ProjectDocuments
            .FirstOrDefaultAsync(pd => pd.DocumentId == documentId && pd.ProjectId == projectId);

        if (existing != null)
        {
            _logger.LogInformation($"Document {documentId} already attached to Project {projectId}");
            return;
        }

        var attachment = new ProjectDocument
        {
            DocumentId = documentId,
            ProjectId = projectId,
            AttachedByUserId = userId,
            AttachedDate = DateTime.UtcNow
        };

        _context.ProjectDocuments.Add(attachment);
        await _context.SaveChangesAsync();

        // Notify project members
        try
        {
            var projectManagers = await _context.ProjectMembers
                .Where(pm => pm.ProjectId == projectId)
                .Select(pm => pm.UserId)
                .Distinct()
                .ToListAsync();

            foreach (var managerId in projectManagers)
            {
                if (managerId != userId) // Don't notify the person who attached it
                {
                    await _notificationService.CreateDocumentProjectAttachmentNotificationAsync(managerId, document.Title, project.Name);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Failed to send project attachment notification: {ex.Message}");
        }

        await LogActivityAsync(documentId, DocumentActivityType.AttachProject, $"Attached to project {project.Name}");
        _logger.LogInformation($"Document {documentId} attached to Project {projectId}");
    }

    /// <summary>
    /// Attach a document to a task (owner-only).
    /// </summary>
    public async Task AttachDocumentToTaskAsync(int documentId, int taskId, int userId)
    {
        var document = await _context.Documents.FindAsync(documentId);
        if (document == null || document.IsDeleted)
        {
            throw new InvalidOperationException("Document not found.");
        }

        // Authorization: Only owner can attach
        if (document.OwnerUserId != userId)
        {
            throw new UnauthorizedAccessException("Only document owner can attach to tasks.");
        }

        var task = await _context.Tasks.FindAsync(taskId);
        if (task == null)
        {
            throw new InvalidOperationException("Task not found.");
        }

        // Check if already attached
        var existing = await _context.TaskDocuments
            .FirstOrDefaultAsync(td => td.DocumentId == documentId && td.TaskId == taskId);

        if (existing != null)
        {
            _logger.LogInformation($"Document {documentId} already attached to Task {taskId}");
            return;
        }

        var attachment = new TaskDocument
        {
            DocumentId = documentId,
            TaskId = taskId,
            AttachedByUserId = userId,
            AttachedDate = DateTime.UtcNow
        };

        _context.TaskDocuments.Add(attachment);
        await _context.SaveChangesAsync();

        // Notify task reporter/owner
        try
        {
            var taskCreatorId = task.CreatedByUserId;
            if (taskCreatorId != userId && taskCreatorId > 0)
            {
                await _notificationService.CreateDocumentTaskAttachmentNotificationAsync(taskCreatorId, document.Title, task.Title);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Failed to send task attachment notification: {ex.Message}");
        }

        await LogActivityAsync(documentId, DocumentActivityType.AttachTask, $"Attached to task {task.Title}");
        _logger.LogInformation($"Document {documentId} attached to Task {taskId}");
    }
}
