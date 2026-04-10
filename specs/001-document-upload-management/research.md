# Research: Document Upload and Management

## Decision 1: File storage model

- Decision: Store document metadata in SQLite and save uploaded file binaries on a local server-side filesystem folder outside `wwwroot`.
- Rationale: This training app must support files up to 25 MB without the overhead of large BLOB storage in SQLite. A filesystem-backed binary store also simplifies preview/download handling and keeps the metadata model lightweight.
- Alternatives considered:
  - Store binary content directly in SQLite as BLOB: rejected because large files reduce database performance, increase backup complexity, and make file streaming harder.
  - Store uploaded files in `wwwroot`: rejected because static access would bypass authorization controls and violate the app's security-first principle.

## Decision 2: Access control model

- Decision: Implement a dual access model with explicit `DocumentShare` records and project/task association records (`ProjectDocument` and `TaskDocument`).
- Rationale: The spec requires both direct shares to recipients and integration with projects/tasks. This model supports "Shared with Me" access, project-level visibility, and task attachment scenarios while preserving clear owner-based control.
- Alternatives considered:
  - Project-only sharing: rejected because it would not satisfy explicit recipient sharing and notification requirements.
  - Share-only model: rejected because it would duplicate project/task membership logic and reduce collaboration integration.

## Decision 3: Preview and download flow

- Decision: Use authenticated server-side preview/download endpoints for supported file types, with inline PDF/image preview and download fallback for all other documents.
- Rationale: Server-side endpoints enforce authorization and allow secure access to file content stored outside public folders. Supported preview types will be limited to common browser-friendly formats to keep the implementation simple and reliable.
- Alternatives considered:
  - Direct file URL links: rejected for security reasons.
  - Full browser-based editor/preview of every file type: rejected for scope and training simplicity.

## Decision 4: Metadata and search model

- Decision: Keep metadata simple and searchable: title, description, category, project, tags, size, type, and owner.
- Rationale: A straightforward metadata model supports fast filtering and search without introducing additional lookup tables or complicated taxonomy management.
- Alternatives considered:
  - Normalized category lookup table: rejected to keep the first version of the feature aligned with the learning-focused repository.
  - Separate tag entity: rejected in favor of a comma-delimited tag field for simplicity.

## Decision 5: Notification and audit integration

- Decision: Extend existing `NotificationService` for share and attachment notifications and add `DocumentActivity` audit events for uploads, metadata edits, replacements, permissions changes, and deletes.
- Rationale: The app already provides notification infrastructure. Reusing it avoids introducing a new channel and keeps the implementation consistent with current patterns.
- Alternatives considered:
  - New notification subsystem: rejected because it would add unnecessary complexity for this feature.
  - No audit trail: rejected due to the explicit requirement for document activity logging.
