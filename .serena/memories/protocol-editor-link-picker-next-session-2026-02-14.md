## Additional Audit & Fixes (Follow-up after hosted default mismatch)

### Context
User reported that protocol edits appeared locally but not on hosted. Root issue was fallback file mismatch and additional consistency concerns.

### New fixes applied

1. **Startup candidate precedence adjusted**
- File: `SleepEditWeb/Services/ProtocolStarterService.cs`
- `GetStartupCandidatePaths()` now orders candidates:
  1) `DefaultProtocolPath`
  2) `SaveProtocolPath`
  3) `StartupProtocolPath`
  4) fallback `default-protocol.xml`
  5) fallback `protocol.xml`
- This prevents stale startup file preference over explicitly configured save path when default path is not set.

2. **Upload import no longer clobbers default fallback file**
- File: `SleepEditWeb/Controllers/ProtocolEditorController.cs`
- `ResolveUploadedFileSavePath()` now uses `SaveProtocolPath` only when explicitly configured.
- Without configured save path, upload now writes to `Data/protocols/<uploaded-file-name>.xml`.
- Avoids accidental overwrite of `default-protocol.xml` during upload imports.

3. **Protocol editor fetch hardening (network failure visibility)**
- File: `SleepEditWeb/Views/ProtocolEditor/Index.cshtml`
- Added `try/catch` around:
  - `refreshState`
  - `onImportXmlSelected`
  - `onSaveXml`
  - `onSetDefaultProtocol`
  - `postState`
- UI now shows explicit status on network/reachability failures instead of silently failing.

### Added regression tests

- File: `SleepEditWeb.Tests/ProtocolStarterServiceTests.cs`
  - `Create_PrefersSaveProtocolPath_OverStartupProtocolPath_WhenDefaultIsNotConfigured`

- File: `SleepEditWeb.Tests/ProtocolEditorControllerTests.cs`
  - `ImportXmlUpload_WithoutConfiguredSavePath_UsesUploadedFileNameFallbackPath`

### Documentation updates

- File: `docs/protocol-editor-deployment-runbook.md`
  - Updated startup candidate precedence.
  - Added explicit `ImportXmlUpload` save-path behavior notes.

### Validation
- `dotnet test SleepEditWeb.sln` passed.
- Test count now: 50 passing.
