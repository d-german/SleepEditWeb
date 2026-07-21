# Sleep Note Editor end-to-end plan

## Scope

Exercise the user-facing Sleep Note Editor workflow in Chromium. Protocol Viewer insertion is included; Protocol Editor authoring and administration are intentionally excluded.

## Scenarios

1. Editor shell
   - Tool order is Generate Sleep Note, Protocol Viewer, Medication Tool.
   - Help is progressive disclosure, theme toggles, and the layout has no mobile overflow.
2. Editor document actions
   - Direct editing, formatting, explicit save, reload persistence, and print-window creation.
3. Sleep Note Generator
   - Diagnostic selections produce a narrative and Insert into Editor changes only the editor.
   - PAP/BIPAP pressure, backup-rate, therapy-transition, mask-course, and mask-manager paths generate the expected narrative.
   - Reset and Cancel do not insert content.
4. Medication Tool
   - Add, remove, clear, Insert at Cursor, Replace Medications Section, Copy Narrative, and drug-information rendering.
5. Protocol Viewer
   - A selected protocol item inserts into the editor and Cancel leaves the document unchanged.

## Regression boundary

Before opening a tool, tests may deliberately place the browser selection on the page heading. Tool output must still append to the editor rather than replacing unrelated page content.
