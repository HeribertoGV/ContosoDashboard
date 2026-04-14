# Quickstart: Document Upload and Management

## Run the application

1. Open a terminal in the repository root:
   ```bash
   cd /Users/heriberto/Documents/VSCode/pruebas/SDD/TrainingProjects/ContosoDashboard
   ```
2. Run the ContosoDashboard app:
   ```bash
   dotnet run --project ContosoDashboard/ContosoDashboard.csproj
   ```
3. Open the browser to the URL shown in the terminal, typically `https://localhost:5001`.

## Login

- Use the mock login page at `/login`.
- Available seeded users include:
  - `admin@contoso.com` (Administrator)
  - `camille.nicole@contoso.com` (Project Manager)
  - `floris.kregel@contoso.com` (Team Lead)
  - `ni.kang@contoso.com` (Employee)

## Exercise the feature

### 1. Upload & Manage Documents
1. Navigate to the `Documents` section after logging in.
2. Click **Upload New** to upload a document with title, category, and optional description.
3. Confirm the document appears in `My Documents` with metadata and file size.
4. View document details by clicking the **View** action.
5. Edit metadata, replace the file (**.razor pages support re-upload**), or delete.

### 2. Search & Filter
1. From `My Documents`, use the search box to find documents by title, tags, or description.
2. Filter by **Category** or **Project** dropdowns.
3. Observe that searches are case-insensitive and apply authorization rules (only documents you own, are shared with, or are in your projects appear).

### 3. Share Documents (Phase 5)
1. Open a document detail view and click **Share**.
2. A modal appears allowing you to select recipient users.
3. Optionally add a message for context.
4. Click **Share with Selected Users** - recipients receive notifications.
5. Recipients can view shared documents at `/documents/shared` ("Shared with Me").

### 4. Project Integration (Phase 5)
1. From a project view (if available), navigate to `/projects/{projectId}/documents`.
2. See documents attached to the project.
3. Project members can view these attached documents.

### 5. Task Attachment (Phase 5)
1. Navigate to `/tasks/{taskId}/attach-document`.
2. Select a document from your available documents.
3. Review the document preview details.
4. Click **Attach Document to Task** - task creator receives notification.
5. Attached documents appear in task context.

## Developer notes

### Storage & Configuration
- **File storage**: Server-side folder `UploadedDocuments` outside `wwwroot` to preserve authorization controls
- **Metadata storage**: SQLite via `ApplicationDbContext` (10 tables total including document audit trail)
- **Configuration**: Stored in `appsettings.json` under `DocumentStorage` section
  - `UploadPath`: Relative path to upload folder
  - `MaxFileSizeBytes`: 25 MB (26214400 bytes)
  - `AllowedMimeTypes`: PDF, Word, Excel, Text, CSV, Images

### Database Entities
- `Document`: Core metadata (title, category, tags, file info, owner, timestamps, soft-delete)
- `DocumentShare`: Explicit sharing with recipient permission tracking
- `ProjectDocument`: Project-document associations for project-level access
- `TaskDocument`: Task-document associations for task context
- `DocumentActivity`: Complete audit trail (Upload, Edit, Replace, Share, RevokeShare, AttachProject, AttachTask, Delete, Download)

### Authorization Model (Triple-Path Access)
- **Path 1**: Document owner (full permissions: view, edit, delete, share)
- **Path 2**: Explicit share recipient with active `DocumentShare` record (read-only unless `CanEditMetadata=true`)
- **Path 3**: Project member (read-only for project-attached documents)

### Implementation Details
- Use existing `NotificationService` for share and attachment alerts
- Use existing authorization policies (`DocumentViewer`, `Employee`, etc.) for consistency
- Mock authentication provides test users (admin, project manager, team lead, employee)
- Application startup automatically creates `UploadedDocuments` folder if it doesn't exist
- File naming: GUID-based (`{DocumentId}_{Timestamp}_{OriginalName}`) to prevent collisions

### Performance Considerations
- Queries use `AsNoTracking()` for read optimizations
- 6 database indexes on high-query paths (owner, date, category, recipient, compound keys)
- Pagination: 20 items per page default in UI, 100 in service layer  
- Search is case-insensitive with SQL LIKE patterns on title/tags/description

### Testing the Feature
1. **Fresh start**: Application creates DB and `UploadedDocuments` folder automatically.
2. **Mock users**: Log in with test credentials (see Login section above).
3. **End-to-end flow**: 
   - Login as Employee → Upload doc → Share with Team Lead → Login as Team Lead → View "Shared with Me" → Verify notification sent.
4. **Edge cases**: 
   - Attempt to download doc as unauthorized user → Denied
   - Attempt to delete others' docs → Denied
   - Replace file → Old file deleted, metadata preserved
   - Remove share → Previous recipient loses access

### Folder Creation & Setup

**Automatic Initialization**:
- The application automatically creates the `UploadedDocuments` folder in the project root on first run
- Folder location: `{ProjectRoot}/UploadedDocuments/` (configurable in `appsettings.json`)
- Access: Server process must have read/write permissions
- On Linux/macOS: Ensure file permissions are appropriate (typically `755` or `775`)

**Manual Verification**:
```bash
# After first run, verify the folder exists:
ls -la UploadedDocuments/

# Check folder permissions:
stat UploadedDocuments/

# Expected output: drwxr-xr-x (or similar)
```

**Troubleshooting Folder Issues**:
- **Error**: "Access denied creating upload folder"
  - Solution: Check process permissions, ensure `ContosoDashboard` user has write access to project root
- **Error**: "Upload folder path not found"
  - Solution: Verify `appsettings.json` `DocumentStorage.UploadPath` is set correctly
  - Default: `UploadedDocuments` (relative path from application root)

### Test Credentials & Capability Matrix

| User | Email | Password | Role | Can Upload | Can Share | Can Delete Own | Can Access Project Docs |
|------|-------|----------|------|-----------|-----------|----------------|-------------------------|
| Admin | admin@contoso.com | *mock* | Administrator | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes |
| Manager | camille.nicole@contoso.com | *mock* | Project Manager | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes |
| Lead | floris.kregel@contoso.com | *mock* | Team Lead | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes (member) |
| Employee | ni.kang@contoso.com | *mock* | Employee | ✅ Yes | ✅ Yes | ✅ Yes (own docs) | ✅ If assigned |

**Note**: Using mock authentication - passwords are ignored. Any password works in development.

### File Upload Testing

**Recommended Test Files**:

| Type | Example | Size | MIME Type | Status |
|------|---------|------|-----------|--------|
| PDF | Document.pdf | ~500 KB | application/pdf | ✅ Supported |
| Word | Report.docx | ~200 KB | application/vnd.openxmlformats-officedocument.wordprocessingml.document | ✅ Supported |
| Excel | Data.xlsx | ~100 KB | application/vnd.openxmlformats-officedocument.spreadsheetml.sheet | ✅ Supported |
| Text | Notes.txt | ~10 KB | text/plain | ✅ Supported |
| CSV | Export.csv | ~50 KB | text/csv | ✅ Supported |
| Image | Screenshot.png | ~1-2 MB | image/png | ✅ Supported |
| Image | Photo.jpg | ~2-5 MB | image/jpeg | ✅ Supported |

**Maximum File Size**: 25 MB per upload

**Rejected File Types**:
- Executables: `.exe`, `.msi`, `.bat`, `.sh`, `.cmd`
- Archives (without approval): `.zip`, `.rar`, `.7z` (configurable via appsettings)
- Source code: `.cs`, `.js`, `.py` (depends on MIME type allowlist)

### Database & Data Model Reference

**10 Document-Related Tables**:
1. `Document` - Core document metadata
2. `DocumentShare` - Explicit document shares (recipient + permissions)
3. `ProjectDocument` - Document-to-project associations
4. `TaskDocument` - Document-to-task associations
5. `DocumentActivity` - Audit trail of all document operations
6. (Plus 5 existing tables: User, Project, Task, etc.)

**Audit Trail Activity Types** (9 types):
- `Upload` - Document file uploaded
- `MetadataEdit` - Metadata changed (title, description, etc.)
- `ReplaceFile` - Document file replaced
- `Share` - Document shared with recipient
- `RevokeShare` - Share revoked
- `AttachProject` - Attached to project
- `AttachTask` - Attached to task
- `Delete` - Document soft-deleted
- `Download` - Document downloaded/opened

**Soft-Delete Pattern**:
- Deleted documents have `IsDeleted = true` flag
- Original data preserved for audit purposes
- Excluded from user-facing lists and queries
- Can be hard-deleted via admin tools (future enhancement)

### Common Development Tasks

**Viewing Upload Folder Contents** (on Linux/macOS/bash):
```bash
# List all uploaded files
ls -lah UploadedDocuments/

# Count documents  
find UploadedDocuments -type f | wc -l

# Check file sizes
du -sh UploadedDocuments/
```

**Clearing Test Data**:
```bash
# Remove all uploaded files (keeps database intact)
rm -rf UploadedDocuments/*

# Reset database (delete database file if using SQLite)
rm ContosoDashboard.db
# The app will recreate on next run
```

**Checking Configuration**:
```bash
# View document storage settings
grep -A 5 "DocumentStorage" appsettings.json
```

**Running with Custom Upload Path**:
```bash
# Environment-specific override (if needed in future)
export DocumentStorage__UploadPath=/var/documents
dotnet run --project ContosoDashboard/ContosoDashboard.csproj
```

### API Reference Summary

**Main Service Interface**: `IDocumentService` (12 methods)

**Key Methods**:
- Upload: `UploadDocumentAsync(request, stream)` → `int` (DocumentId)
- Query: `GetDocumentsForUserAsync(userId, query)` → `IEnumerable<DocumentSummary>`
- Detail: `GetDocumentDetailsAsync(documentId, userId)` → `DocumentDetails`
- Manage: `UpdateDocumentMetadataAsync`, `ReplaceDocumentFileAsync`, `DeleteDocumentAsync`
- Share: `ShareDocumentAsync`, `RevokeDocumentShareAsync`
- Attach: `AttachDocumentToProjectAsync`, `AttachDocumentToTaskAsync`
- Access: `OpenDocumentStreamAsync`, `GetDocumentActivityAsync`

**Helper Methods**:
- `GetAvailableCategoriesAsync(userId)` - Used for filter dropdowns
- `GetUserProjectsForDocumentsAsync(userId)` - Used for project selection
- `GetDocumentCountForUserAsync(userId)` - Used for dashboard stats

**See Also**: [Contract Documentation](./contracts/document-upload.md) for full API specs

### Migration & Backup

**Database Backup** (Local SQLite):
```bash
# Backup database before testing
cp ContosoDashboard.db ContosoDashboard.db.backup

# Restore from backup
cp ContosoDashboard.db.backup ContosoDashboard.db
```

**Document Files Backup**:
```bash
# Backup upload folder
cp -r UploadedDocuments UploadedDocuments.backup
```

**Production Considerations** (Future):
- Use cloud storage (Azure Blob, AWS S3) instead of local filesystem
- Implement encryption at rest for sensitive documents
- Set up automated backups for document files and database
- Consider archival strategy for deleted documents
