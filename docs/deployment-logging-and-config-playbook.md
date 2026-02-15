# Deployment Logging and Config Playbook

## Protocol Editor/Viewer Modular Frontend

### Required Runtime Expectations

1. `SleepEditWeb/Views/ProtocolEditor/Index.cshtml` loads `/js/protocol-editor-ui.js` via `<script type="module">`.
2. `SleepEditWeb/Views/ProtocolViewer/Index.cshtml` loads `/js/protocol-viewer-bootstrap.js` via `<script type="module">`.
3. Protocol persistence endpoints remain unchanged:
   - `/ProtocolEditor/SaveXml`
   - `/ProtocolEditor/SetDefaultProtocol`
   - `/ProtocolEditor/ImportXmlUpload`

### Pre-Deploy Guardrail Commands

Run from `SleepEditWeb/`:

1. `npm run lint:frontend`
2. `npm run test:frontend`
3. `dotnet test ../SleepEditWeb.sln`

### Logging Focus Areas

When validating rollout health, prioritize these categories:

- `SleepEditWeb.Controllers.ProtocolEditorController`
- `SleepEditWeb.Services.ProtocolEditorService`
- `SleepEditWeb.Services.ProtocolXmlService`
- `SleepEditWeb.Services.ProtocolEditorSessionStore`

### Fast Failure Indicators

Rollback to the previous artifact if any occur:

1. Browser console shows module import errors for editor/viewer bootstrap scripts.
2. Save/default/import protocol endpoints produce repeated 5xx responses.
3. Contract tests fail in CI for protocol route/view markers.
