# Contract: Document Upload and Management

## Feature scope

This feature is implemented inside the Blazor Server application and exposes the following internal contracts:
- UI page routes and user flows.
- Server-side service methods for document lifecycle operations.
- Authorized download and preview endpoints.

## UI routes and user experience

- `/documents`: personal document dashboard and search/filter panel.
- `/documents/upload`: upload form for title, category, description, project, tags, and file.
- `/documents/shared`: documents explicitly shared with the current user.
- `/documents/{documentId}`: document details, metadata edit, replace file, sharing controls, activity history.
- `/projects/{projectId}/documents`: project documents list for project members and managers.
- `/tasks/{taskId}/attach-document`: task attachment workflow.

## Service contract

### `IDocumentService`

Proposed interface methods:

- `Task<IEnumerable<DocumentSummary>> GetDocumentsForUserAsync(int userId, DocumentQuery query)`
- `Task<DocumentDetails> GetDocumentDetailsAsync(int documentId, int userId)`
- `Task<int> UploadDocumentAsync(DocumentUploadRequest request, Stream fileStream)`
- `Task UpdateDocumentMetadataAsync(int documentId, int userId, DocumentMetadataUpdate update)`
- `Task ReplaceDocumentFileAsync(int documentId, int userId, Stream fileStream)`
- `Task DeleteDocumentAsync(int documentId, int userId)`
- `Task ShareDocumentAsync(int documentId, int userId, DocumentShareRequest request)`
- `Task RevokeDocumentShareAsync(int documentShareId, int userId)`
- `Task AttachDocumentToProjectAsync(int documentId, int projectId, int userId)`
- `Task AttachDocumentToTaskAsync(int documentId, int taskId, int userId)`
- `Task<Stream> OpenDocumentStreamAsync(int documentId, int userId)`
- `Task<IEnumerable<DocumentActivityRecord>> GetDocumentActivityAsync(int documentId, int userId)`

### Helper Methods

- `Task<IEnumerable<string>> GetAvailableCategoriesAsync(int userId)` - Returns list of unique categories the user has documents in; used for filter dropdowns
- `Task<IEnumerable<(int ProjectId, string ProjectName)>> GetUserProjectsForDocumentsAsync(int userId)` - Returns list of projects user is member of for document attachment selection
- `Task<int> GetDocumentCountForUserAsync(int userId)` - Returns total count of documents owned by, shared with, or accessible via project membership; used for dashboard summary

## Data contracts

### `DocumentUploadRequest`
- `Title` (required, max 255 chars): Document title for searching and display
- `Description` (optional, max 2000 chars): Additional context about document
- `Category` (required, max 100 chars): Classification for filtering (e.g., Report, Contract, Specification)
- `Tags` (optional): Comma-separated tags for detailed searching
- `ProjectId` (optional): Associate document with a project context

### `DocumentQuery`
- `SearchTerm` (optional): Search in title, tags, description (case-insensitive)
- `Category` (optional): Filter by exact category match
- `ProjectId` (optional): Filter to documents for this project only
- `PageNumber` (default 1): Pagination support
- `PageSize` (default 20): Items per page

### `DocumentSummary`
- `DocumentId`: Unique identifier
- `Title`: Document title
- `Category`: Category badge
- `UploadedDate`: Timestamp of upload
- `FileSize`: File size in bytes
- `ProjectName` (nullable): Associated project name if applicable
- `OwnerUserId`: ID of document owner

### `DocumentDetails`
- All fields from `DocumentSummary`, plus:
- `Description` (nullable): Full description text
- `Tags` (nullable): Comma-separated tags
- `FileName`: Original uploaded filename
- `ContentType`: MIME type (e.g., application/pdf, text/plain)
- `UpdatedDate`: Timestamp of last modification
- `ProjectId` (nullable): Associated project ID

### `DocumentMetadataUpdate`
- `Title` (optional): New title
- `Description` (optional): New description
- `Category` (optional): New category
- `Tags` (optional): New tags
- `ProjectId` (optional): New project association

### `DocumentShareRequest`
- `RecipientUserIds` (required, min 1): List of user IDs to share with
- `CanEditMetadata` (optional, default false): Allow recipients to edit metadata (not currently implemented in UI)
- `Message` (optional): Optional message for share notification

### `DocumentActivityRecord`
- `DocumentActivityId`: Unique audit record ID
- `ActivityType` (enum): Upload, MetadataEdit, ReplaceFile, Share, RevokeShare, AttachProject, AttachTask, Delete, Download
- `PerformedByUserName`: User who performed the action
- `Timestamp`: When action occurred
- `Notes` (nullable): Additional context

### `DocumentUserInfo`
- `UserId`: User ID
- `UserName`: Display name for selection in share modal

## Endpoint contract

- `GET /documents/download/{documentId}`
  - Authorization: must own, be shared with, or have project/task access.
  - Response: binary file download with original filename.

- `GET /documents/preview/{documentId}`
  - Authorization: same as download.
  - Response: inline preview for supported MIME types; otherwise redirect to download.

## Error handling

### Common exceptions

- `UnauthorizedAccessException`: Thrown when user lacks permission to access/modify document
  - Own another user's document
  - Share without being owner
  - Delete shared document as recipient
  - Access document in project they're not member of

- `InvalidOperationException`: Thrown for business logic violations
  - Upload exceeds size limit (25 MB)
  - Upload has unsupported MIME type
  - Share already recipient
  - Revoke share that isn't active

- `ArgumentException`: Thrown for invalid input
  - Null or empty title
  - Invalid file stream
  - Non-existent document/user/project/task IDs

## Validation rules

### File upload
- Required MIME types: PDF, DOC/DOCX, XLS/XLSX, TXT, CSV, common images (PNG, JPG, GIF, WebP)
- Rejected MIME types: Executable (.exe, .msi), archives without approval
- Size limit: 25 MB (26,214,400 bytes)
- Filename: Sanitized to remove path traversal attempts, stored as `{DocumentId}_{Timestamp}_{OriginalName}`

### Metadata
- Title: 1-255 characters, required
- Category: 1-100 characters, required
- Description: 0-2000 characters, optional
- Tags: Comma-separated, case-insensitive for searching
- ProjectId: Must exist in Projects table, user must be member/manager

### Sharing
- RecipientUserIds: Min 1 recipient, max practical limit (50 suggested)
- CanEditMetadata: Only full document owner has edit rights (recipient flag not enforced in Phase 1)
- Message: 0-500 characters for notification context

## Performance requirements

- Document list query: < 2 seconds for 500 documents (with indexes)
- File upload: < 30 seconds for 25 MB
- File download: < 5 seconds (depends on network)
- Search with filters: < 2 seconds
- Pagination: 20-100 items per page

## Security contract

- All access is enforced server-side, not via direct static file links.
- Users may not access documents unless:
  - they own the document,
  - they are an explicit recipient with active `DocumentShare` record,
  - they are a project member/manager for a project-attached document,
  - they are authorized for the task associated with a task document.
- Delete actions require confirmation in the UI.
- File deletion: Old files physically removed when replaced or document deleted (except soft-delete records kept for audit)
- Activity logging: All document operations recorded in `DocumentActivity` table with user and timestamp
