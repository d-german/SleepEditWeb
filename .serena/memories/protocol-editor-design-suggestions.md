# Protocol Editor — Design Improvement Suggestions

These are architectural and design improvement suggestions for the protocol editor system, identified during a comprehensive review of the codebase on 2026-02-14. The current design was originally conceived ~15-20 years ago and migrated from WinForms/WPF to ASP.NET Core MVC. The core abstraction (recursive tree of selectable protocol statements with cross-references and enumerated options) is fundamentally sound. These suggestions target the infrastructure around it.

---

## 1. Multi-Branch Link Model (Replace Single-Link per Node)

**Current design**: Each `ProtocolNodeModel` has exactly one `LinkId` and one `LinkText`. A node can only reference one other node.

**Problem**: Clinical protocols have branching decision points (e.g., "if SpO2 < 88% → CPAP, if SpO2 < 50% → BiPAP, if arrhythmia → Emergency"). The single-link constraint forces splitting logic across multiple sibling nodes, fragmenting what should be one branching statement.

**Suggested change**: Replace `LinkId`/`LinkText` with `Links: List<ProtocolLink>` where:
```csharp
public sealed record ProtocolLink(int TargetId, string Label, string Condition = "");
```
- `TargetId`: ID of the target node
- `Label`: Display text for the link
- `Condition`: Optional condition text (e.g., "SpO2 < 50%")

**Impact**: Model change, XML schema change (backward-compatible if `<Link>` elements wrap existing `<LinkId>`/`<LinkText>`), editor UI needs multi-link management, viewer needs multi-link buttons per node.

**Files affected**: `ProtocolEditorModels.cs`, `ProtocolXmlService.cs`, `ProtocolEditorService.cs`, `ProtocolEditor/Index.cshtml` (link info card + link picker), `ProtocolViewer/Index.cshtml` (link rendering), all protocol test files.

---

## 2. Persistent Storage with Versioning (Replace Session-Based State)

**Current design**: Protocol document lives in `ISession` (in-memory, per-user, JSON-serialized). No database. Session timeout = lost work.

**Problems**:
- No concurrent multi-user editing
- No audit trail (who changed what, when)
- No version history beyond current session's undo stack
- Session timeout or server restart loses all unsaved work

**Suggested change**: Introduce a lightweight persistence layer:
- Store protocol documents in LiteDB (already a project dependency) or SQLite
- Each save creates a versioned record: `{ Id, DocumentXml, Author, Timestamp, Comment }`
- Undo/redo remains in-session for responsiveness, but explicit "Save" persists a version
- Add a version history view to browse and restore previous versions

**Schema sketch**:
```csharp
public sealed record ProtocolVersion(
    int Id,
    string DocumentXml,
    string Author,
    DateTime SavedUtc,
    string? Comment);
```

**Files affected**: New `IProtocolRepository` service, `ProtocolEditorSessionStore.cs` (fallback to DB), `ProtocolEditorController.cs` (save/load endpoints), `Program.cs` (DI registration).

---

## 3. Command-Pattern Undo/Redo (Replace Snapshot Cloning)

**Current design**: Entire `ProtocolDocument` is deep-cloned (via JSON round-trip) before every mutation. Clone pushes to undo stack (max 100 entries).

**Problem**: Increasingly expensive as protocol documents grow. Every keystroke on a large protocol clones the whole tree. The legacy code actually had a more sophisticated command pattern (`UndoManager` with `Command.Do()/Undo()/Redo()`).

**Suggested change**: Implement command/event-based undo:
```csharp
public interface IProtocolCommand
{
    void Execute(ProtocolDocument doc);
    void Undo(ProtocolDocument doc);
    string Description { get; }
}
```
- Each mutation creates a command object with minimal delta info
- Undo replays the inverse operation
- Optional: event sourcing where the mutation log IS the persistence format

**Alternative (lower effort)**: Keep snapshot approach but use a more efficient cloning strategy (e.g., structural sharing / immutable data structures).

**Files affected**: `ProtocolEditorService.cs` (major refactor), new `Commands/` folder with command classes, `ProtocolEditorSnapshot` model.

---

## 4. FHIR-Aligned Protocol Schema (Optional, for Interoperability)

**Current design**: Proprietary XML schema (`<Protocol><Section><SubSection>...`). Not interoperable with EHR systems.

**Problem**: Modern healthcare systems use HL7 FHIR. The current schema can't integrate with EHR clinical decision support without a translation layer.

**Suggested change**: Align the internal model with FHIR `PlanDefinition` resource concepts:
- `PlanDefinition.action` maps to protocol sections
- `action.action` maps to subsections (recursive)
- `action.condition` maps to link conditions
- `action.relatedAction` maps to cross-references
- `action.input` could model SubText as coded inputs

This would be an optional export format alongside the existing XML, not a replacement of the internal model.

**Files affected**: New `ProtocolFhirExportService`, new export endpoint, no changes to existing functionality.

---

## 5. Visual Decision-Flow Graph View (Alongside List View)

**Current design**: Viewer renders protocol as a linear checkbox list within Bootstrap tabs. Cross-section links provide navigation but the clinician can't see the decision flow at a glance.

**Problem**: Clinicians think in flowcharts ("if this, then that, otherwise this"). The list view is functional but doesn't convey the decision structure visually.

**Suggested change**: Add a toggleable "Flow View" in the Protocol Viewer:
- Render protocol nodes as a directed graph where links become visible arrows
- Use a client-side library: Mermaid.js (simplest, already diagram-capable), React Flow, or GoJS
- Sections become swimlanes or clusters; nodes become boxes; links become directed edges
- Keep the existing list view as the default; add a "Show Flow" toggle

**Implementation approach with Mermaid.js**:
```javascript
function buildMermaidGraph(document) {
    let lines = ['graph TD'];
    for (const section of document.sections) {
        for (const node of flattenNodes(section)) {
            lines.push(`  N${node.id}["${escapeLabel(node.text)}"]`);
            if (node.linkId > 0) {
                lines.push(`  N${node.id} -->|"${node.linkText}"| N${node.linkId}`);
            }
        }
    }
    return lines.join('\n');
}
```

**Files affected**: `ProtocolViewer/Index.cshtml` (new tab/toggle + Mermaid rendering), `wwwroot/` (Mermaid.js library), `site.css` (flow view styles).

---

## 6. Typed SubText Items (Replace Plain Strings)

**Current design**: `SubText` is `List<string>`. Items like "50%" or "edit" are untyped text.

**Problem**: No validation, no structured data capture, no computational use. The system can't check whether a clinician selected a valid SpO2 threshold or aggregate SubText selections for reporting.

**Suggested change**: Replace `List<string>` with `List<SubTextItem>`:
```csharp
public sealed record SubTextItem(
    string Label,
    string Value,
    string? Code = null,            // e.g., SNOMED code
    SubTextType Type = SubTextType.Text);

public enum SubTextType { Text, Numeric, Coded, Boolean }
```

**Backward compatibility**: Migration reads existing plain strings as `SubTextItem(Label: s, Value: s, Type: Text)`. XML serialization adds optional `<Type>` and `<Code>` attributes.

**Files affected**: `ProtocolEditorModels.cs`, `ProtocolXmlService.cs`, editor SubText card UI, viewer SubText dropdown rendering, all SubText-related tests.

---

## 7. Extract Frontend JavaScript into Modules

**Current design**: 1,338 lines of inline JavaScript in `ProtocolEditor/Index.cshtml`. All state management, tree rendering, drag-and-drop, link picker, context menus, and keyboard shortcuts are embedded.

**Problem**: Untestable, hard to debug, difficult to extend, no code reuse between editor and viewer.

**Suggested change**: Extract into dedicated JS modules:
```
wwwroot/js/
  protocol-tree.js          # Tree rendering and state management
  protocol-drag-drop.js     # Drag-and-drop logic
  protocol-link-picker.js   # Link picker modal
  protocol-editor-api.js    # fetch() calls to backend
  protocol-viewer.js        # Viewer-specific logic
```

Use ES modules (`import`/`export`) or a lightweight bundler. This enables:
- Frontend unit testing (Jest/Vitest)
- Code sharing between editor and viewer (tree rendering, link navigation)
- Better IDE support (autocompletion, refactoring)

**Alternative (lower effort)**: Use Alpine.js or HTMX to reduce JS boilerplate while staying in Razor views.

**Files affected**: `ProtocolEditor/Index.cshtml` (extract `<script>` blocks), `ProtocolViewer/Index.cshtml`, new files in `wwwroot/js/`.

---

## Priority Ranking

| # | Suggestion | Effort | Impact | Recommended Priority |
|---|-----------|--------|--------|---------------------|
| 7 | Extract JS into modules | Medium | High (maintainability) | 1 — Do first, enables everything else |
| 2 | Persistent storage + versioning | Medium | High (reliability) | 2 — Eliminates data loss risk |
| 1 | Multi-branch link model | Medium | High (functionality) | 3 — Biggest UX improvement |
| 3 | Command-pattern undo | High | Medium (performance) | 4 — Only matters at scale |
| 6 | Typed SubText items | Low | Medium (data quality) | 5 — Quick win |
| 5 | Visual flow graph | Medium | Medium (UX) | 6 — Nice to have |
| 4 | FHIR export | High | Low (unless EHR integration needed) | 7 — Only if interop required |
