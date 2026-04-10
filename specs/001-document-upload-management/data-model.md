# Data Model: Document Upload and Management

## Core Entities

### Document
Represents an uploaded file and its metadata.

- `DocumentId` (PK)
- `OwnerUserId` (FK to `User`)
- `Title` (required)
- `Description` (optional)
- `Category` (required)
- `Tags` (optional, comma-separated)
- `ProjectId` (optional FK to `Project`)
- `FileName` (original uploaded filename)
- `ContentType` (MIME type)
- `FileSize` (bytes)
- `StoragePath` (server-side file path)
- `UploadedDate`
- `UpdatedDate`
- `IsDeleted` (soft-delete marker)

Relationships:
- `OwnerUser` (`User`)
- `Project` (`Project`)
- `DocumentShares` collection
- `ProjectDocuments` collection
- `TaskDocuments` collection
- `DocumentActivities` collection

### DocumentShare
Maps a document to an explicitly shared recipient.

- `DocumentShareId` (PK)
- `DocumentId` (FK to `Document`)
- `RecipientUserId` (FK to `User`)
- `SharedByUserId` (FK to `User`)
- `SharedDate`
- `CanEditMetadata` (bool, optional)
- `IsActive` (bool)

Relationships:
- `Document`
- `RecipientUser`
- `SharedByUser`

### ProjectDocument
Associates a document with a project for access control and project document views.

- `ProjectDocumentId` (PK)
- `DocumentId` (FK to `Document`)
- `ProjectId` (FK to `Project`)
- `AttachedByUserId` (FK to `User`)
- `AttachedDate`

Relationships:
- `Document`
- `Project`
- `AttachedByUser`

### TaskDocument
Associates a document with a task and its related project context.

- `TaskDocumentId` (PK)
- `DocumentId` (FK to `Document`)
- `TaskId` (FK to `TaskItem`)
- `AttachedByUserId` (FK to `User`)
- `AttachedDate`

Relationships:
- `Document`
- `Task`
- `AttachedByUser`

### DocumentActivity
Captures audit events for document lifecycle operations.

- `DocumentActivityId` (PK)
- `DocumentId` (FK to `Document`)
- `ActivityType` (Upload, MetadataEdit, ReplaceFile, Share, RevokeShare, AttachProject, AttachTask, Delete, Download)
- `PerformedByUserId` (FK to `User`)
- `Timestamp`
- `Notes` (optional)

Relationships:
- `Document`
- `PerformedByUser`

## Integration with existing models

- `User` is already established as the authenticated actor and permission source.
- `Project` already supports membership through `ProjectMember`; a user can access a project document if they are a project member or manager.
- `TaskItem` already links to projects and users, enabling task-level attachments and cascading access through the owning project.

## Validation rules

- `Title`: required, max 255 characters.
- `Category`: required, max 100 characters.
- `FileSize`: reject uploads larger than 25 MB.
- `ContentType`: allow only approved MIME types.
- `StoragePath`: must be a server-local path outside `wwwroot`.

## Access rules

- Owners may view, edit metadata, replace files, share, and delete their documents.
- Explicit recipients may view and download shared documents.
- Project members and managers may view project documents.
- Task attachments are visible to users authorized for the associated task and project.
- Deleted documents are soft-deleted, removed from all user-facing lists, and retained for audit.
