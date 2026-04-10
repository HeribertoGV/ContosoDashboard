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

1. Navigate to the `Documents` section after logging in.
2. Upload a document with title, category, and optional description.
3. Confirm the document appears in `My Documents` with metadata and project context.
4. Share the document with another user and verify the shared recipient receives a notification.
5. Attach the document to a task and confirm the task shows the linked document.
6. Edit metadata, replace the file, and delete the document to verify lifecycle behavior.

## Developer notes

- Document binaries should be stored in a server-side folder outside of `wwwroot` to preserve authorization controls.
- Metadata lives in SQLite via `ApplicationDbContext`.
- If the `UploadedDocuments` folder does not exist, create it at the application root.
- Use the existing `NotificationService` and authorization policies for consistency.
