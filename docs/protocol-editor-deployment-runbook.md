# Protocol Editor Deployment Runbook

## Scope

This runbook covers production configuration and diagnostics for:

- Protocol Editor save/import/export behavior
- Protocol Viewer startup protocol loading
- Protocol editor/viewer ES module bootstrap and guardrail checks
- LiteDB database persistence and backup
- Logging settings needed for incident triage

## Protocol Persistence Model

**As of the DB-only migration, all protocol persistence uses LiteDB.**

- **Save/Set Default**: Writes to the `current_protocol` collection in LiteDB and records a version in `protocol_versions`.
- **Import (upload)**: Reads XML from the uploaded file, parses it, and persists the result to the database.
- **Export**: Serializes the current session protocol to XML for browser download. No filesystem write occurs.
- **Startup loading**: `ProtocolStarterService` loads from `GetCurrentProtocol()` (DB), falling back to a hardcoded seed protocol on first-ever startup.

> **Migration Note**: Existing deployments that previously used file-based protocol persistence (`DefaultProtocolPath`, `SaveProtocolPath`, etc.) will start fresh from the seed protocol on first startup unless the protocol was previously saved to the version history database. The `ProtocolEditor` config section and all file path environment variables have been removed.

### Database Location

| Platform | Path |
| --- | --- |
| Linux (Docker/Koyeb) | `/app/Data/protocol-versions.db` |
| Windows (local dev) | `<AppContext.BaseDirectory>/Data/protocol-versions.db` |

### Database Backup

- The LiteDB database file should be backed up regularly.
- On Docker/Koyeb, ensure the `/app/Data` directory is on persistent storage (not ephemeral container disk).
- For backup, copy the `.db` file while the application is stopped, or use LiteDB's checkpoint mechanism.

## Frontend Module Deployment Checks

The protocol editor/viewer bootstraps through ES modules:

- Editor bootstrap import: `/js/protocol-editor-ui.js`
- Viewer bootstrap import: `/js/protocol-viewer-bootstrap.js`

Pre-deployment checks:

1. Run `npm run lint:frontend` from `SleepEditWeb/` and confirm dependency guardrails pass.
2. Run `npm run test:frontend` from `SleepEditWeb/` and confirm pure helper/store tests pass.
3. Run `dotnet test SleepEditWeb.sln` and ensure `ProtocolEditorUiContractsTests` still pass view-contract checks.

Rollback trigger:

- If either bootstrap module fails to load in browser (404/import error/syntax error), rollback to last known-good deployment artifact immediately; no server-side configuration change is required for rollback.

## Required Configuration

Feature flags:

- `Features:ProtocolEditorEnabled` must be `true` for editor endpoints.
- `Features:SleepNoteEditorEnabled` should remain `true` for end-to-end tech note workflows.

### Environment Variable Names

Use ASP.NET Core double-underscore mapping:

- `Features__ProtocolEditorEnabled`

### Example Values

`appsettings.Production.json`:

```json
{
  "Features": {
    "ProtocolEditorEnabled": true
  }
}
```

> **Note**: The `ProtocolEditor` config section with `DefaultProtocolPath`, `StartupProtocolPath`, and `SaveProtocolPath` has been removed. Protocol persistence is now entirely database-driven.

## Storage Requirements

- The app identity must have read/write permission to the `Data/` directory (for the LiteDB database file).
- Persist storage across restarts. Ephemeral container disks will lose the protocol database.
- On Koyeb/Docker, mount a persistent volume at `/app/Data`.

## Multi-Instance Caveats

- LiteDB is a single-file embedded database. It does **not** support concurrent access from multiple processes.
- If running multiple app instances behind a load balancer, pin protocol admin operations (save/set-default/import) to a single instance.
- For multi-instance deployments requiring shared protocol state, consider migrating to a networked database (e.g., PostgreSQL).

## Logging Recommendations

Set at least:

- `Logging:LogLevel:Default=Information`
- `Logging:LogLevel:Microsoft.AspNetCore=Warning`
- `Logging:LogLevel:SleepEditWeb=Information`

For incident debugging, temporarily raise categories to `Debug`:

- `SleepEditWeb.Controllers.ProtocolEditorController`
- `SleepEditWeb.Services.ProtocolStarterService`
- `SleepEditWeb.Services.ProtocolEditorService`
- `SleepEditWeb.Services.ProtocolXmlService`
- `SleepEditWeb.Services.ProtocolEditorSessionStore`
- `SleepEditWeb.Infrastructure.ProtocolPersistence.LiteDbProtocolRepository`

Reset to `Information`/`Warning` after incident closure.

## Incident Triage Checklist

### Endpoint Troubleshooting Matrix

| Endpoint | HTTP | Typical Cause | First Checks |
| --- | --- | --- | --- |
| `POST /ProtocolEditor/SaveXml` | `500` | DB write failure (permissions, disk full, corrupted DB) | Check Data directory permissions and disk space |
| `POST /ProtocolEditor/SetDefaultProtocol` | `500` | DB write failure | Same as SaveXml |
| `POST /ProtocolEditor/ImportXmlUpload` | `400` | No file, oversized file, or invalid XML | Validate upload payload and size (< 2 MB) |
| `POST /ProtocolEditor/ImportXmlUpload` | `500` | DB write failure after successful parse | Check Data directory and DB file health |

### Save XML returns 500

1. Confirm `Features__ProtocolEditorEnabled=true`.
2. Check that `Data/protocol-versions.db` exists and is writable.
3. Check disk space on the volume hosting the Data directory.
4. Review logs for `SaveXml` or `SaveCurrentProtocol` errors.

### Protocol Viewer loads seed data instead of saved protocol

1. Check that `GetCurrentProtocol` returns data — review startup logs for "loaded document from database" vs. "created seed document".
2. Verify the database file exists at the expected path.
3. If migrating from file-based persistence, re-import the protocol XML via the Import upload feature.

### Import XML fails

1. Validate XML structure if `FormatException` messages appear.
2. Confirm upload size is below 2 MB.

## Post-Deployment Smoke Tests

1. Open Protocol Editor and run `Save XML`.
2. Run `Set As Default`.
3. Open Protocol Viewer and verify the expected protocol loads.
4. Import a valid XML file via upload and verify it appears in viewer/editor.
5. Check logs contain success entries for each operation (look for `SaveCurrentProtocol` and `SaveXml completed` messages).
6. Restart the application and verify the Protocol Viewer loads the saved protocol from the database.
