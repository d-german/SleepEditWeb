# Protocol Editor Deployment Runbook

## Scope

This runbook covers production configuration and diagnostics for:

- Protocol Editor save/import/export behavior
- Protocol Viewer startup protocol loading
- Protocol editor/viewer ES module bootstrap and guardrail checks
- LiteDB database persistence and backup
- Logging settings needed for incident triage

## Protocol Persistence Model

**All protocol persistence uses LiteDB with multi-protocol support.**

### Collections

- **`saved_protocols`**: Stores named protocol metadata (ID, name, default flag, timestamps). Each protocol is an independent document tree.
- **`protocol_versions`**: Version history for all protocols. Each entry optionally tagged with a `ProtocolId` to associate with a saved protocol.
- **`current_protocol`** (legacy): Retained for backward compatibility. New installations may not use this collection.

### Operations

- **Save**: Writes to both `current_protocol` (legacy compat) and the active protocol in `saved_protocols` + `protocol_versions`.
- **Set Default**: Marks a protocol in `saved_protocols` as default. The Protocol Viewer loads the default protocol.
- **Import (upload)**: Parses XML and saves to the active protocol.
- **Export**: Serializes the current session protocol to XML for download.
- **Create Protocol**: Creates a new entry in `saved_protocols` with a seed document.
- **Switch Protocol**: Auto-saves current editor state, then loads the selected protocol.
- **Delete Protocol**: Removes from `saved_protocols`. Cannot delete the active or default protocol.

### Migration from Single to Multi-Protocol

> **Important**: Existing deployments that used a single protocol will automatically migrate on first startup. The `EnsureMigration()` method in `LiteDbProtocolRepository` copies the existing `current_protocol` document into `saved_protocols` as the default protocol. This migration runs once (lazily) on the first call to `ListProtocols()` or `GetDefaultProtocol()`.

No manual intervention is required. The migration is idempotent — running it multiple times is safe.

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
- `SleepEditWeb.Services.ProtocolManagementService`

Reset to `Information`/`Warning` after incident closure.

## Incident Triage Checklist

### Endpoint Troubleshooting Matrix

| Endpoint | HTTP | Typical Cause | First Checks |
| --- | --- | --- | --- |
| `POST /ProtocolEditor/SaveXml` | `500` | DB write failure (permissions, disk full, corrupted DB) | Check Data directory permissions and disk space |
| `POST /ProtocolEditor/SetDefaultProtocol` | `500` | DB write failure | Same as SaveXml |
| `POST /ProtocolEditor/ImportXmlUpload` | `400` | No file, oversized file, or invalid XML | Validate upload payload and size (< 2 MB) |
| `POST /ProtocolEditor/ImportXmlUpload` | `500` | DB write failure after successful parse | Check Data directory and DB file health |
| `POST /ProtocolEditor/CreateProtocol` | `400` | Missing or blank protocol name | Validate request body has non-empty `name` field |
| `POST /ProtocolEditor/LoadProtocol/{id}` | `404` | Protocol ID not found in database | Verify protocol exists via `ListProtocols` endpoint |
| `POST /ProtocolEditor/DeleteProtocol/{id}` | `400` | Attempting to delete active or default protocol | Switch to different protocol first, or unset as default |
| `POST /ProtocolEditor/RenameProtocol/{id}` | `400` | Missing or blank new name | Validate request body has non-empty `name` field |

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
7. Create a new protocol via the Protocol Selector "New" button.
8. Switch between protocols and verify the tree updates.
9. Rename a protocol and verify the name change persists.
10. Set a non-default protocol as default and verify the Viewer loads it.
11. Delete a non-active, non-default protocol and verify it's removed from the list.
