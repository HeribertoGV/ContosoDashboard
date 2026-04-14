# Tasks: Document Upload and Management

**Input**: Design documents from `/specs/001-document-upload-management/`

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Establish the document feature structure and configuration needed before implementation.

- [ ] T001 [P] Create `ContosoDashboard/ContosoDashboard/Models/Document.cs` with metadata fields for title, description, category, tags, project, file path, size, content type, owner, timestamps, and soft-delete.
- [ ] T002 [P] Create `ContosoDashboard/ContosoDashboard/Models/DocumentShare.cs` with fields for document ID, recipient user ID, shared-by user ID, share date, edit permission, and active flag.
- [ ] T003 [P] Create `ContosoDashboard/ContosoDashboard/Models/ProjectDocument.cs` with fields for document ID, project ID, attaching user ID, and attached date.
- [ ] T004 [P] Create `ContosoDashboard/ContosoDashboard/Models/TaskDocument.cs` with fields for document ID, task ID, attaching user ID, and attached date.
- [ ] T005 [P] Create `ContosoDashboard/ContosoDashboard/Models/DocumentActivity.cs` with fields for document ID, activity type, performed-by user ID, timestamp, and notes.
- [ ] T006 Create `ContosoDashboard/ContosoDashboard/Services/IDocumentService.cs` defining document lifecycle contract methods such as `UploadDocumentAsync`, `GetDocumentsForUserAsync`, `UpdateDocumentMetadataAsync`, `ReplaceDocumentFileAsync`, `DeleteDocumentAsync`, `ShareDocumentAsync`, `AttachDocumentToProjectAsync`, `AttachDocumentToTaskAsync`, and `OpenDocumentStreamAsync`.
- [ ] T007 Create `ContosoDashboard/ContosoDashboard/Services/DocumentService.cs` as the concrete implementation stub for document storage, authorization, and activity logging.
- [ ] T008 Add a `DocumentStorage` configuration section to `ContosoDashboard/ContosoDashboard/appsettings.json` and define a safe local upload folder path outside `wwwroot`.
- [ ] T009 [P] Add a `DocumentPages` route stub or navigation entry in `ContosoDashboard/ContosoDashboard/Shared/NavMenu.razor` linking to `/documents`.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Connect the new document domain to the existing application and enforce security before user story work begins.

- [ ] T010 Update `ContosoDashboard/ContosoDashboard/Data/ApplicationDbContext.cs` to add `DbSet<Document> Documents`, `DbSet<DocumentShare> DocumentShares`, `DbSet<ProjectDocument> ProjectDocuments`, `DbSet<TaskDocument> TaskDocuments`, and `DbSet<DocumentActivity> DocumentActivities`, and configure relationships/indexes.
- [ ] T011 Register `IDocumentService` with `DocumentService` in `ContosoDashboard/ContosoDashboard/Program.cs` and add any required service dependencies.
- [ ] T012 Add document storage folder creation and validation logic in `ContosoDashboard/ContosoDashboard/Program.cs` or `ContosoDashboard/ContosoDashboard/Services/DocumentService.cs` to ensure the upload directory exists at startup.
- [ ] T013 Extend `ContosoDashboard/ContosoDashboard/Services/NotificationService.cs` to support document share notifications and project/task attachment notifications.
- [ ] T014 [P] Update `ContosoDashboard/ContosoDashboard/Models/User.cs` navigation collection or relationships only if needed for shared document access; otherwise verify existing user role and membership relations are sufficient.
- [ ] T015 [P] Verify `ContosoDashboard/ContosoDashboard/Program.cs` authorization policies support the document access rules and add a dedicated `DocumentViewer` or `Employee` policy if necessary.

---

## Phase 3: User Story 1 - Upload and manage personal documents (Priority: P1) 🎯 MVP

**Goal**: Enable authenticated employees to upload documents with metadata, view their own documents, edit metadata, replace files, and delete documents.

**Independent Test**: Verify an authenticated user can upload a supported file, see it in `My Documents`, edit title/category, and delete it successfully.

- [ ] T016 [US1] Implement the upload and metadata workflow in `ContosoDashboard/ContosoDashboard/Services/DocumentService.cs`, including file validation and storage, metadata persistence, and owner-based access checks.
- [ ] T017 [US1] Create `ContosoDashboard/ContosoDashboard/Pages/DocumentUpload.razor` to allow file selection, title, category, description, project assignment, and submit upload.
- [ ] T018 [US1] Create `ContosoDashboard/ContosoDashboard/Pages/Documents.razor` to display the user's personal document list with recent documents and summary counts.
- [ ] T019 [US1] Create `ContosoDashboard/ContosoDashboard/Pages/DocumentDetails.razor` to show document metadata, download/preview actions, edit metadata controls, replace file controls, and delete confirmation.
- [ ] T020 [US1] Implement file replacement support in `ContosoDashboard/ContosoDashboard/Services/DocumentService.cs`, preserving metadata and logging the replacement activity in `DocumentActivity`.
- [ ] T021 [US1] Implement document deletion support in `ContosoDashboard/ContosoDashboard/Services/DocumentService.cs`, marking documents as soft-deleted and removing them from user-facing lists.
- [ ] T022 [US1] Add validation rules in `ContosoDashboard/ContosoDashboard/Pages/DocumentUpload.razor` and `ContosoDashboard/ContosoDashboard/Pages/DocumentDetails.razor` to reject unsupported file types and files larger than 25 MB.
- [ ] T023 [US1] Add audit logging for upload, metadata edit, replace, and delete actions in `ContosoDashboard/ContosoDashboard/Services/DocumentService.cs` and persist audit events in `DocumentActivity`.
- [ ] T024 [US1] Implement secure document stream access in `ContosoDashboard/ContosoDashboard/Services/DocumentService.cs` and expose preview/download functionality from `ContosoDashboard/ContosoDashboard/Pages/DocumentDetails.razor`.

---

## Phase 4: User Story 2 - Browse, search, and filter documents (Priority: P2)

**Goal**: Provide document browsing, search, and filtering for authorized documents, with fast list rendering and friendly no-results feedback.

**Independent Test**: Verify that document lists load, filters by category/project work, search returns only authorized results, and no-results messages display.

- [ ] T025 [P] [US2] Implement document query support in `ContosoDashboard/ContosoDashboard/Services/DocumentService.cs` for title/tag search, category filter, project filter, and authorized document scoping.
- [ ] T026 [P] [US2] Extend `ContosoDashboard/ContosoDashboard/Pages/Documents.razor` with search input, category filter, project filter, and list refresh controls.
- [ ] T027 [US2] Update `ContosoDashboard/ContosoDashboard/Pages/Documents.razor` to render document rows with title, category, date, size, project, and view action links.
- [ ] T028 [US2] Implement friendly no-results and error states in `ContosoDashboard/ContosoDashboard/Pages/Documents.razor`.
- [ ] T029 [US2] Add server-side list performance safeguards in `ContosoDashboard/ContosoDashboard/Services/DocumentService.cs` for query filtering and sorting.
- [ ] T030 [US2] Ensure search results exclude unauthorized documents in `ContosoDashboard/ContosoDashboard/Services/DocumentService.cs` by applying ownership/share/project/task access rules.

---

## Phase 5: User Story 3 - Share documents and integrate with projects/tasks (Priority: P3)

**Goal**: Enable explicit document sharing, notifications, and attachment to projects/tasks for collaborative access.

**Independent Test**: Verify document sharing sends notifications and task/project attachments appear in the corresponding project/task views.

- [ ] T031 [P] [US3] Create `ContosoDashboard/ContosoDashboard/Pages/DocumentShared.razor` to show documents explicitly shared with the current user.
- [ ] T032 [P] [US3] Add share controls to `ContosoDashboard/ContosoDashboard/Pages/DocumentDetails.razor` that allow the owner to select recipients and send a share.
- [ ] T033 [US3] Implement `ShareDocumentAsync` and `RevokeDocumentShareAsync` in `ContosoDashboard/ContosoDashboard/Services/DocumentService.cs` and persist `DocumentShare` records.
- [ ] T034 [US3] Implement shared document access checks in `ContosoDashboard/ContosoDashboard/Services/DocumentService.cs` for recipients and active share status.
- [ ] T035 [US3] Create `ContosoDashboard/ContosoDashboard/Pages/ProjectDocuments.razor` to display documents attached to a project for project members and managers.
- [ ] T036 [US3] Create `ContosoDashboard/ContosoDashboard/Pages/TaskAttachDocument.razor` to allow attaching a document to a task and choosing the related project context.
- [ ] T037 [US3] Implement `AttachDocumentToProjectAsync` and `AttachDocumentToTaskAsync` in `ContosoDashboard/ContosoDashboard/Services/DocumentService.cs` and persist `ProjectDocument`/`TaskDocument` records.
- [ ] T038 [US3] Add notification dispatch to `ContosoDashboard/ContosoDashboard/Services/DocumentService.cs` for share recipients and task/project attachments using `NotificationService`.
- [ ] T039 [US3] Implement authorization checks for project document access and task attachment access in `ContosoDashboard/ContosoDashboard/Services/DocumentService.cs`.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Finish shared improvements, documentation, and validation across the new document feature.

- [ ] T040 [P] Polish UI copy, error messages, and confirmation dialogs in `ContosoDashboard/ContosoDashboard/Pages/Documents.razor`, `DocumentUpload.razor`, `DocumentDetails.razor`, `DocumentShared.razor`, `ProjectDocuments.razor`, and `TaskAttachDocument.razor`.
- [ ] T041 [P] Validate the feature flow against `specs/001-document-upload-management/quickstart.md` and update the quickstart if needed.
- [ ] T042 [P] Review and document the new document service API surfaces in `specs/001-document-upload-management/contracts/document-upload.md`.
- [ ] T043 [P] Refactor `ContosoDashboard/ContosoDashboard/Data/ApplicationDbContext.cs` entity configuration and indexes for the final document access model.
- [ ] T044 [P] Confirm all document access rules and edge cases from the spec are enforced in `ContosoDashboard/ContosoDashboard/Services/DocumentService.cs`.
- [ ] T045 [P] Add or update any developer notes in `specs/001-document-upload-management/quickstart.md` for setup, folder creation, and test credentials.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1: Setup** must complete before Phase 2 begins.
- **Phase 2: Foundational** must complete before any user story work begins.
- **Phase 3/4/5: User Stories** can begin after Phase 2 and may proceed in parallel where team capacity allows.
- **Phase 6: Polish** depends on completion of all user stories.

### User Story Dependencies

- **US1** can be implemented independently after foundational support is available.
- **US2** depends only on foundations and the document list/query surface; it should not require US1 completion beyond shared models and services.
- **US3** depends on foundations and uses document sharing/attachment features that may reuse US1 APIs, but it is still independently testable after Phase 2.

### Parallel Opportunities

- `T001`–`T005` are parallelizable model creation tasks.
- `T011`, `T013`, and `T015` are parallelizable foundational service and auth checks.
- `T025` and `T026` are parallelizable UI/query implementation tasks for US2.
- `T031` and `T032` can be executed in parallel for US3 page creation and share UI layout.
- `T040`–`T045` are cross-cutting polish tasks that can be assigned in parallel once feature implementation is complete.

### Suggested MVP Scope

Deliver **User Story 1 (P1)** as the MVP:
1. Complete Phase 1 setup.
2. Complete Phase 2 foundational integration.
3. Deliver US1 upload/manage personal documents.
4. Validate the story independently before adding US2/US3.

---

## Independent Test Criteria

- **US1**: Upload a supported file, verify it appears in `My Documents`, update metadata, and delete it.
- **US2**: Search and filter the document list, verify only authorized documents are shown, and view friendly no-results behavior.
- **US3**: Share a document, confirm recipient notification and shared access, attach a document to a task/project, and verify it appears in the proper context.
