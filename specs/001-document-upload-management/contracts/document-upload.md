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

## Data contracts

### `DocumentUploadRequest`

- `Title`
- `Description`
- `Category`
- `Tags`
- `ProjectId` (optional)
- `TaskId` (optional)
- `OwnerUserId` (derived from current user)

### `DocumentMetadataUpdate`

- `Title`
- `Description`
- `Category`
- `Tags`
- `ProjectId` (optional)

### `DocumentShareRequest`

- `RecipientUserIds`
- `CanEditMetadata` (optional)
- `Message` (optional)

## Endpoint contract

- `GET /documents/download/{documentId}`
  - Authorization: must own, be shared with, or have project/task access.
  - Response: binary file download with original filename.

- `GET /documents/preview/{documentId}`
  - Authorization: same as download.
  - Response: inline preview for supported MIME types; otherwise redirect to download.

## Security contract

- All access is enforced server-side, not via direct static file links.
- Users may not access documents unless:
  - they own the document,
  - they are an explicit recipient,
  - they are a project member/manager for a project document,
  - they are authorized for the task associated with a task document.
- Delete actions require confirmation in the UI.
