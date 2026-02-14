# Protocol Editor Deployment Runbook

## Scope

This runbook covers production configuration and diagnostics for:

- Protocol Editor save/import/default path behavior
- Protocol Viewer startup protocol loading
- Logging settings needed for incident triage

## Required Configuration

`ProtocolEditorStartupOptions` binds from the `ProtocolEditor` section:

- `ProtocolEditor:DefaultProtocolPath`
- `ProtocolEditor:StartupProtocolPath`
- `ProtocolEditor:SaveProtocolPath`

Feature flags:

- `Features:ProtocolEditorEnabled` must be `true` for editor endpoints.
- `Features:SleepNoteEditorEnabled` should remain `true` for end-to-end tech note workflows.

### Environment Variable Names

Use ASP.NET Core double-underscore mapping:

- `ProtocolEditor__DefaultProtocolPath`
- `ProtocolEditor__StartupProtocolPath`
- `ProtocolEditor__SaveProtocolPath`
- `Features__ProtocolEditorEnabled`

### Example Values

`appsettings.Production.json`:

```json
{
  "Features": {
    "ProtocolEditorEnabled": true
  },
  "ProtocolEditor": {
    "DefaultProtocolPath": "/mnt/shared/sleepedit/protocols/default-protocol.xml",
    "StartupProtocolPath": "/mnt/shared/sleepedit/protocols/default-protocol.xml",
    "SaveProtocolPath": "/mnt/shared/sleepedit/protocols/protocol.xml"
  }
}
```

Equivalent environment variables:

```text
Features__ProtocolEditorEnabled=true
ProtocolEditor__DefaultProtocolPath=/mnt/shared/sleepedit/protocols/default-protocol.xml
ProtocolEditor__StartupProtocolPath=/mnt/shared/sleepedit/protocols/default-protocol.xml
ProtocolEditor__SaveProtocolPath=/mnt/shared/sleepedit/protocols/protocol.xml
```

## Path Resolution Behavior

When values are empty, runtime fallback behavior is:

1. Save path resolution uses:
   - `SaveProtocolPath`
   - `StartupProtocolPath`
   - `DefaultProtocolPath`
   - fallback: `<AppContext.BaseDirectory>/Data/protocols/default-protocol.xml`
2. Default path resolution uses:
   - `DefaultProtocolPath`
   - `StartupProtocolPath`
   - `SaveProtocolPath`
   - fallback: `<AppContext.BaseDirectory>/Data/protocols/default-protocol.xml`
3. Protocol Viewer startup load candidates use:
   - `DefaultProtocolPath`
   - `SaveProtocolPath`
   - `StartupProtocolPath`
   - fallback: `<AppContext.BaseDirectory>/Data/protocols/default-protocol.xml`
   - fallback: `<AppContext.BaseDirectory>/Data/protocols/protocol.xml`

Upload behavior:

- `ImportXmlUpload` writes to `SaveProtocolPath` when explicitly configured.
- If no explicit save path is configured, upload writes to `<AppContext.BaseDirectory>/Data/protocols/<uploaded-file-name>.xml` to avoid clobbering `default-protocol.xml`.

## Filesystem Requirements

- The app identity must have read/write permission to configured path directories.
- If using fallback paths, ensure write access to:
  - `<AppBase>/Data/protocols/`
- Persist storage across restarts. Ephemeral container disks will lose saved/default protocol files.

## Multi-Instance Caveats

- If running multiple app instances behind a load balancer, use shared storage for protocol XML files.
- If instances use local disks only, one node may save/set default while another node loads stale/missing files.
- If shared storage is not available, pin admin/protocol operations to a single instance.

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

Reset to `Information`/`Warning` after incident closure.

## Incident Triage Checklist

### Endpoint Troubleshooting Matrix

| Endpoint | HTTP | Typical Cause | First Checks |
| --- | --- | --- | --- |
| `POST /ProtocolEditor/SaveXml` | `400` | No resolved save path (unexpected after fallback unless path computation is overridden) | Confirm feature flag, inspect resolved path log, validate options binding |
| `POST /ProtocolEditor/SaveXml` | `500` | Write failure (permissions, invalid path, unavailable storage) | Check app identity write permission and mounted volume health |
| `POST /ProtocolEditor/SetDefaultProtocol` | `400` | No resolved default path | Confirm config binding and fallback behavior |
| `POST /ProtocolEditor/SetDefaultProtocol` | `500` | Write failure to default target | Validate target directory permissions and storage |
| `POST /ProtocolEditor/ImportXml` | `400` | Missing file path or invalid XML format | Confirm file exists and validate XML |
| `POST /ProtocolEditor/ImportXml` | `500` | Read/IO/permission failure | Check file ACLs, storage connectivity, and path correctness |
| `POST /ProtocolEditor/ImportXmlUpload` | `400` | No file, oversized file, or invalid XML | Validate upload payload and size (< 10 MB) |
| `POST /ProtocolEditor/ImportXmlUpload` | `500` | Failed save/read operation after upload | Validate write path and storage availability |

### Save XML returns 400 or 500

1. Confirm `Features__ProtocolEditorEnabled=true`.
2. Check resolved path values for `ProtocolEditor__*Path` environment vars.
3. Verify destination directory exists and is writable by app identity.
4. Review logs for `SaveXml` warnings/errors with resolved path.

### Set As Default succeeds but Protocol Viewer loads old/default data

1. Confirm all three path values point to the same shared storage location, or intentionally aligned locations.
2. Verify app instances all mount the same storage.
3. Review `ProtocolStarterService` startup candidate path logs and confirm a readable file exists at a candidate path.

### Import XML fails

1. Confirm XML file exists and is readable.
2. Validate XML structure if `FormatException` messages appear.
3. Confirm upload size is below 10 MB for upload endpoint.

## Post-Deployment Smoke Tests

1. Open Protocol Editor and run `Save XML`.
2. Run `Set As Default`.
3. Open Protocol Viewer and verify the expected default protocol loads.
4. Import a valid XML file and verify it appears in viewer/editor.
5. Check logs contain success entries for each operation and no warning/error for path resolution.

## Verify Logs in Hosting Platform

1. Set category level to `Information` (or `Debug` for short-lived diagnostics) for `SleepEditWeb.*`.
2. Restart/redeploy the app so settings are applied.
3. Execute `Save XML`, `Set As Default`, `Import XML`, and `Import Upload` once each.
4. Confirm you can find matching log lines:
   - `SaveXml requested. Resolved save path:`
   - `SetDefaultProtocol requested. Resolved default path:`
   - `ImportXml requested. Resolved import path:`
   - `ImportXmlUpload requested for file`
5. Confirm failures include warning/error logs with path/file context and that success logs include completed markers.
