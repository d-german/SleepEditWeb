# Sleep Note Editor POC Manual Validation

## Purpose
Validate legacy-parity workflow behavior for the sleep note editor medication tool:
- open tool
- search/select meds
- apply insert/replace/copy modes
- clear/copy behavior
- edit-after-insert continuity

## Test Run Metadata
- Tester: `<name>`
- Date: `<yyyy-mm-dd>`
- Build/Commit: `<sha or branch>`
- Environment: `<local/dev/test>`
- Browser: `<name/version>`

## Preconditions
1. Ensure feature flag is enabled:
   - `SleepEditWeb/appsettings.json` -> `Features:SleepNoteEditorEnabled = true`
2. Start app from repo root:
   - `dotnet run --project SleepEditWeb/SleepEditWeb.csproj`
3. Open:
   - Legacy baseline page: `https://localhost:<port>/MedList`
   - POC page: `https://localhost:<port>/SleepNoteEditor`

## Test Data
- Known med example: `zolpidem` (or any autocomplete hit from repository).
- Unknown med example: `MyManualUnknownMed`.
- Secondary med example: `melatonin`.

## Validation Scenarios

### LP-01 Open Medication Tool
Steps:
1. Go to `/SleepNoteEditor`.
2. Click `Medication Tool`.

Expected:
- Modal opens.
- `Search Medications` input is focused.
- Selected list preview is visible and matches current session state.

Observed outcome: `<fill>`
Status: `Not run | Pass | Fail`
Notes/Evidence: `<fill>`

### LP-02 Search and Select Medications (Legacy-Style Commands)
Steps:
1. In modal search input, type at least 2 chars (example: `zo`) and confirm suggestions appear.
2. Press `Enter` on `zolpidem` (or click `Add` with same value).
3. Type `melatonin`, press `Enter`.
4. Type `+MyManualUnknownMed`, press `Enter`.
5. Type `-MyManualUnknownMed`, press `Enter`.
6. Type `cls`, press `Enter`.
7. Re-add `zolpidem` and `melatonin` for downstream scenarios.

Expected:
- Search suggestions appear for 2+ characters.
- Added meds are appended to `Selected Medications` preview.
- `+name` command adds a local unknown selection entry.
- `-name` removes matching selected entry.
- `cls` clears all selected entries.

Observed outcome: `<fill>`
Status: `Not run | Pass | Fail`
Notes/Evidence: `<fill>`

### LP-03 Apply Mode: Insert at Cursor
Steps:
1. In editor body, place cursor in `History:` section after a known anchor phrase.
2. Open modal and confirm at least two meds are selected.
3. Set `Apply Mode` to `Insert at Cursor`.
4. Click `Done`.

Expected:
- Modal closes.
- Narrative text is inserted at cursor position.
- Status updates to `Medication narrative applied` then `Saved`.
- If unknown meds were selected, warning banner lists them.

Observed outcome: `<fill>`
Status: `Not run | Pass | Fail`
Notes/Evidence: `<fill>`

### LP-04 Apply Mode: Replace Medications Section
Steps:
1. Ensure note contains heading `Medications:`.
2. Change selection (example: keep only `zolpidem`).
3. Set `Apply Mode` to `Replace Medications Section`.
4. Click `Done`.
5. Optional edge check: remove `Medications:` heading manually, run replace mode again.

Expected:
- Existing medication section body is replaced between `Medications:` and next heading.
- If heading is missing, medication section is appended to end of note.
- Status updates to applied/saved messages.

Observed outcome: `<fill>`
Status: `Not run | Pass | Fail`
Notes/Evidence: `<fill>`

### LP-05 Apply Mode: Copy Narrative
Steps:
1. Select one or more meds in modal.
2. Set `Apply Mode` to `Copy Narrative`.
3. Click `Done`.
4. Paste clipboard into temporary text area (outside app) to inspect content.

Expected:
- Modal closes.
- Narrative is copied to clipboard.
- Editor note content is unchanged by this mode.
- Status shows clipboard success message and save result.

Observed outcome: `<fill>`
Status: `Not run | Pass | Fail`
Notes/Evidence: `<fill>`

### LP-06 Clear and Copy Buttons in Tool
Steps:
1. Add 2-3 meds in modal.
2. Click `Copy` in selected-meds panel.
3. Paste clipboard to confirm copied value.
4. Click `Clear`.
5. Close/reopen modal.

Expected:
- `Copy` copies comma-separated selected medication names (not full narrative).
- `Clear` empties selected list preview immediately.
- Reopened modal still shows cleared state for current session.

Observed outcome: `<fill>`
Status: `Not run | Pass | Fail`
Notes/Evidence: `<fill>`

### LP-07 Edit After Insert/Replace
Steps:
1. Complete LP-03 or LP-04.
2. Continue typing directly in editor after modal operation.
3. Click `Save`.
4. Refresh page and verify text persists.

Expected:
- User can continue editing with no lockup after tool completion.
- Post-insert manual edits persist across save/refresh.

Observed outcome: `<fill>`
Status: `Not run | Pass | Fail`
Notes/Evidence: `<fill>`

## Observed Outcome Summary Placeholder
| Scenario | Result | Evidence Link/Screenshot | Notes |
| --- | --- | --- | --- |
| LP-01 | `<fill>` | `<fill>` | `<fill>` |
| LP-02 | `<fill>` | `<fill>` | `<fill>` |
| LP-03 | `<fill>` | `<fill>` | `<fill>` |
| LP-04 | `<fill>` | `<fill>` | `<fill>` |
| LP-05 | `<fill>` | `<fill>` | `<fill>` |
| LP-06 | `<fill>` | `<fill>` | `<fill>` |
| LP-07 | `<fill>` | `<fill>` | `<fill>` |

## Known Gaps (Current POC)
1. Legacy `+name` behavior differs: in POC it adds unknown selection only; it does not update master medication repository.
2. `beforeunload` autosave uses `sendBeacon` to an antiforgery-protected endpoint, so close-tab autosave may fail.
3. Clipboard operations do not show user-facing fallback on permission/secure-context errors.
4. Editor update path uses plain text (`innerText`), so rich formatting can be lost when content is re-applied.

## Prioritized Next Steps Backlog
1. P0: Fix close-tab autosave by using a token-compatible save path or dedicated non-antiforgery beacon endpoint.
2. P0: Align command parity decision for `+/-` semantics (selection-only vs legacy master-list mutation) and implement chosen behavior.
3. P1: Add automated integration tests for insert/replace/copy flows and clear/copy commands.
4. P1: Add explicit error handling/toasts for clipboard and apply/save failures.
5. P2: Preserve richer editor formatting during insert/replace operations.
