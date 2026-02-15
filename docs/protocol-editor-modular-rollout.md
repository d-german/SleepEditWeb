# Protocol Editor/Viewer Modular Rollout Plan

## Goal

Roll out the modularized protocol editor/viewer frontend safely with clear rollback conditions and repeatable operational steps.

## Scope

- Editor module split (`protocol-editor-ui.js`, `protocol-tree.js`, `protocol-drag-drop.js`, `protocol-link-picker.js`)
- Viewer module split (`protocol-viewer.js`, `protocol-viewer-bootstrap.js`)
- Shared utilities (`protocol-shared-utils.js`)
- Frontend architecture guardrails and test harness (`npm run lint:frontend`, `npm run test:frontend`)

## Phase Plan

### Phase 0: Pre-Rollout Validation (Non-Production)

1. Run `npm run lint:frontend` from `SleepEditWeb/`.
2. Run `npm run test:frontend` from `SleepEditWeb/`.
3. Run `dotnet test SleepEditWeb.sln`.
4. Manual browser validation:
   - Editor add/move/link/subtext/import/save/set-default flows.
   - Viewer section navigation, link jump, and output generation.
5. Confirm no browser console module-load errors in editor/viewer pages.

Exit criteria:

- All automated checks pass.
- No critical/manual regressions.

### Phase 1: Controlled Production Release

1. Deploy during low-traffic window.
2. Execute post-deploy smoke test:
   - Open `/ProtocolEditor` and confirm tree renders and updates.
   - Open `/ProtocolViewer` and confirm tabs and node selections render.
   - Perform one `Save Protocol` and `Set As Default` operation.
3. Verify application logs for save/default/import endpoints.

Exit criteria:

- Editor and viewer pages operate without module import/runtime errors.
- Save/default/import endpoints behave as expected.

### Phase 2: Stabilization Window

1. Monitor logs for:
   - frontend bootstrap failures (browser-side reports)
   - endpoint errors (`SaveXml`, `SetDefaultProtocol`, `ImportXmlUpload`)
2. Monitor support/QA feedback for drag-drop/link-picker regressions.
3. Keep rollback package and deployment notes ready for immediate reversion.

Exit criteria:

- No high-severity regressions in stabilization period.

## Rollback Conditions

Rollback immediately if any of the following occur:

1. Editor or viewer fails to load due to missing/broken module import.
2. Core editing workflows (add/move/link/import/save) are blocked.
3. Viewer output generation fails for normal usage.
4. Repeated 5xx errors appear on protocol persistence endpoints after deployment.

## Rollback Procedure

1. Redeploy last known-good artifact.
2. Re-run smoke checks on `/ProtocolEditor` and `/ProtocolViewer`.
3. Validate protocol persistence endpoints with one save/default action.
4. Record incident details and root-cause notes before attempting re-rollout.

## Fallback/Recovery Notes

- Rollback does not require code edits or DB migration steps.
- Configuration keys for protocol paths remain unchanged.
- If persistence errors persist post-rollback, follow `docs/protocol-editor-deployment-runbook.md` triage matrix.

## Non-Production Testability Checklist

Use this checklist before every release candidate:

1. Module guardrails pass (`npm run lint:frontend`).
2. Frontend unit tests pass (`npm run test:frontend`).
3. Contract and backend tests pass (`dotnet test SleepEditWeb.sln`).
4. Browser smoke test confirms no console import/runtime errors.
5. Import/save/default flow validates file-path behavior in configured environment.
