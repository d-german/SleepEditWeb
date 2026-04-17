# Sleep Note Feature — Complete Implementation Reference

## Overview
The Sleep Note is a clinical checklist-based note generator ported from the WinForms `TechNote` dialog (`C:\sleepEdit-master\sleepEdit\Technote.cs`). It produces natural-language narrative from technician-selected options covering study type, body position, snoring, respiratory events, CPAP/BIPAP treatment info, and miscellaneous clinical observations.

## Architecture

### Files Created
| File | Purpose |
|------|---------|
| `SleepEditWeb/Models/SleepNoteModels.cs` | Domain models: StudyType, TitrationMode enums; PressureSettings, SleepNoteFormData, SleepNoteConfiguration, SleepNoteGeneratedResult records |
| `SleepEditWeb/Services/SleepNoteNarrativeGenerator.cs` | Pure static class — all narrative logic, 9 methods |
| `SleepEditWeb/Services/SleepNoteService.cs` | ISleepNoteService interface + SleepNoteService implementation (thin orchestration) |
| `SleepEditWeb/Infrastructure/SleepNote/ISleepNoteConfigRepository.cs` | Config repository interface |
| `SleepEditWeb/Infrastructure/SleepNote/LiteDbSleepNoteConfigRepository.cs` | LiteDB config persistence (sleepnote-config.db) |
| `SleepEditWeb/Controllers/SleepNoteController.cs` | API controller — 7 endpoints |
| `SleepEditWeb/Components/SleepNote/SleepNoteForm.razor` | Blazor form UI markup |
| `SleepEditWeb/Components/SleepNote/SleepNoteForm.razor.cs` | Blazor form code-behind |
| `SleepEditWeb/Views/SleepNote/Index.cshtml` | Razor view hosting Blazor component via iframe |
| `SleepEditWeb.Tests/SleepNoteNarrativeGeneratorTests.cs` | 40 unit tests for narrative generator |
| `SleepEditWeb.Tests/SleepNoteServiceTests.cs` | 6 service delegation tests |
| `SleepEditWeb.Tests/SleepNoteControllerTests.cs` | 13 controller tests |

### Modified Files
| File | Change |
|------|--------|
| `SleepEditWeb/Program.cs` | Added DI: `ISleepNoteConfigRepository` (Singleton), `ISleepNoteService` (Scoped) |
| `SleepEditWeb/SleepEditWeb.csproj` | Added `InternalsVisibleTo` for test project |
| `SleepEditWeb/Views/Admin/Medications.cshtml` | Sleep Note as first tab (before Protocol Editor and Medication) |

## Domain Models
- `StudyType` enum: Polysomnogram, CpapBipapTitration, SplitNight
- `TitrationMode` enum: None, Cpap, Bipap
- `PressureSettings` record: InitialCpap, FinalCpap, InitialIpap, InitialEpap, FinalIpap, FinalEpap
- `SleepNoteFormData` record: Uses `IReadOnlySet<string>` for checkbox groups (BodyPositions, SnoringLevels, Events, Effects, MiscOptions)
- `SleepNoteConfiguration` record: MaskTypes, MaskSizes, TechnicianNames, PressureValues (all List<>)
- `SleepNoteGeneratedResult` positional record: (NarrativeText, GeneratedUtc)

## Narrative Assembly Order
Misc (Ambien, O2) → Body Position → Snoring → Respiratory → CPAP Criteria → Treatment Info → Events/Arrhythmias → Patient Machine → Effects

## API Endpoints
| Method | Path | Description |
|--------|------|-------------|
| GET | /SleepNote | View (iframe target) |
| POST | /SleepNote/api/generate | Generate narrative from SleepNoteFormData |
| GET | /SleepNote/api/config | Get configuration |
| POST | /SleepNote/api/config/mask-types | Add mask type (body: `{"value": "..."}`) |
| DELETE | /SleepNote/api/config/mask-types/{maskType} | Remove mask type |
| POST | /SleepNote/api/config/mask-sizes | Add mask size |
| DELETE | /SleepNote/api/config/mask-sizes/{maskSize} | Remove mask size |

## Key Design Decisions
1. **Set-based model**: `IReadOnlySet<string>` for checkbox groups — add/remove items without model changes
2. **Pure static generator**: No mocking needed, 100% testable
3. **Separate LiteDB file**: `sleepnote-config.db` — decoupled from protocol DB
4. **Singleton config document**: Single BsonId key, seeds defaults on first access
5. **Tuple pattern matching**: Replaced WinForms `for(;;){if...break}` pseudo-switch patterns
6. **Conditional UI**: CPAP/BIPAP panels only visible for Titration/SplitNight study types

## How to Add/Remove Items
- **Body positions/Snoring/Events/Effects/Misc**: Add string to the set key, add checkbox in SleepNoteForm.razor, add pattern match case in corresponding generator method
- **Mask types/sizes**: Runtime add/remove via API endpoints or Blazor UI config badges
- **Pressure values**: Stored in SleepNoteConfiguration, seeded as range 4–20

## Test Coverage (179 total tests, 59 new for Sleep Note)
- Narrative generator: 40 tests (all combos)
- Service: 6 tests (delegation verification)
- Controller: 13 tests (status codes, bad request handling)

## WinForms → Web Method Mapping
| WinForms Method | Web Equivalent |
|----------------|----------------|
| `getBodyPos()` | `SleepNoteNarrativeGenerator.GenerateBodyPosition()` |
| `getSnor()` | `SleepNoteNarrativeGenerator.GenerateSnoring()` |
| `getArrPlm()` | `SleepNoteNarrativeGenerator.GenerateEventsAndArrhythmias()` |
| `getTxInfo()` | `SleepNoteNarrativeGenerator.GenerateTreatmentInfo()` |
| `getResInfo()` | `SleepNoteNarrativeGenerator.GenerateRespiratoryInfo()` |
| `getEffects()` | `SleepNoteNarrativeGenerator.GenerateEffects()` |
| `button_makeNote_Click()` | `SleepNoteNarrativeGenerator.Generate()` |
| `resetValues()` | `SleepNoteForm.ResetForm()` |
