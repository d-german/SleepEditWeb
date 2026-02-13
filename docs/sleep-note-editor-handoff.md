# Sleep Note Editor POC Handoff

## Scope of This Handoff
This handoff covers the sleep note editor POC workflow and how to validate legacy-parity behavior using:
- `docs/sleep-note-editor-manual-validation.md`

Owned artifacts in this handoff:
- `docs/sleep-note-editor-manual-validation.md`
- `docs/sleep-note-editor-handoff.md`

## POC Summary
The sleep note editor POC provides:
1. A document-first `contenteditable` sleep note.
2. A modal medication tool embedded in the editor flow.
3. Three apply modes:
   - `Insert at Cursor`
   - `Replace Medications Section`
   - `Copy Narrative`
4. Session-backed persistence for note content and selected medications.

Legacy parity target for this POC is command/workflow familiarity (`Enter`, `+`, `-`, `cls`, copy, clear), not full backend behavioral parity.

## Main Runtime Paths
- Editor UI: `SleepEditWeb/Views/SleepNoteEditor/Index.cshtml`
- API/controller endpoints: `SleepEditWeb/Controllers/SleepNoteEditorController.cs`
- Orchestration: `SleepEditWeb/Services/SleepNoteEditorOrchestrator.cs`
- Narrative generation: `SleepEditWeb/Services/MedicationNarrativeBuilder.cs`
- Insert/replace/copy logic: `SleepEditWeb/Services/EditorInsertionService.cs`
- Session persistence: `SleepEditWeb/Services/SleepNoteEditorSessionStore.cs`
- Feature flag: `Features:SleepNoteEditorEnabled` in `SleepEditWeb/appsettings*.json`

## Validation Handoff Instructions
1. Execute all LP-01 through LP-07 scenarios in `docs/sleep-note-editor-manual-validation.md`.
2. Fill each `Observed outcome`, `Status`, and summary row during run.
3. Capture screenshots for failures (especially insert/replace/copy mode transitions).
4. Confirm whether parity expectation is met for each workflow stage:
   - open tool
   - search/select meds
   - apply insert/replace/copy
   - clear/copy behavior
   - edit-after-insert

## Known Gaps / Risks
1. `+name` legacy mismatch:
   - Legacy MedList uses `+`/`-` to mutate master med list.
   - POC tool uses commands locally for selected list composition only.
2. Potential close-tab autosave failure:
   - `beforeunload` uses `sendBeacon` to `/SleepNoteEditor/Save`, which is antiforgery-protected.
3. Clipboard UX hard-fail risk:
   - Clipboard calls do not provide robust fallback UX when browser permissions block writes.
4. Formatting loss risk:
   - Re-applied editor content is plain text based, so rich formatting can be dropped after apply.
5. Limited automated coverage:
   - No dedicated tests currently guard end-to-end parity workflow in this POC path.

## Prioritized Next Steps Backlog
1. P0 - Resolve autosave reliability.
   - Implement beacon-safe endpoint/token strategy and validate on tab close/navigation.
2. P0 - Finalize parity contract for command semantics.
   - Decide whether POC should remain selection-only or support legacy master-list mutation behavior.
3. P1 - Add integration tests for core workflow.
   - Cover open/search/select/insert/replace/copy/clear/edit-after-insert paths.
4. P1 - Add user-visible error handling.
   - Provide clear toasts/messages for save/apply/clipboard failures.
5. P2 - Improve content fidelity.
   - Preserve richer editor structure when applying medication updates.
6. P2 - Improve observability.
   - Add structured logs/events around tool completion mode and persistence outcomes.

## Acceptance Placeholder
- Manual validation complete: `<yes/no>`
- High-priority blockers outstanding: `<list>`
- Go/No-Go recommendation: `<fill>`
- Handoff owner and date: `<fill>`
