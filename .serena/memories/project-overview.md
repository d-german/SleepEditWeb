# SleepEditWeb — Project Overview

## Purpose
SleepEditWeb is an ASP.NET Core MVC web application for clinical sleep study documentation. It provides:
1. **Sleep Note Editor** — composing clinical sleep study notes with medication narratives
2. **Protocol Editor** — building hierarchical clinical protocol statements (admin side)
3. **Protocol Viewer** — clinician-facing protocol item selection during sleep studies
4. **Medication Database** — managing a local medication list backed by LiteDB + OpenFDA lookups

## Tech Stack
- **Runtime**: .NET 8.0 (ASP.NET Core MVC)
- **Language**: C# with nullable reference types enabled, implicit usings, latest LangVersion
- **Database**: LiteDB 5.0.21 (embedded NoSQL) for medications
- **Session**: ASP.NET Core distributed memory cache sessions (protocol state stored in session)
- **Frontend**: Razor views with Bootstrap 5, Bootstrap Icons, inline JavaScript (no JS framework)
- **Testing**: NUnit 4.2, Moq 4.20, coverlet for coverage
- **Deployment**: Docker (Dockerfile at repo root), GitHub Actions CI to Azure/Koyeb
- **Legacy**: `sleepEdit_sln_only_20260213_090126/` contains the original WinForms/WPF desktop app being migrated

## Codebase Structure
```
SleepEditWeb/                    # Main web project
  Controllers/                   # MVC controllers (Home, Admin, MedList, ProtocolEditor, ProtocolViewer, SleepNoteEditor)
  Services/                      # Business logic (ProtocolEditorService, ProtocolXmlService, ProtocolStarterService, etc.)
  Models/                        # POCOs, ViewModels, feature options
  Views/                         # Razor views per controller
  Data/                          # LiteDB repo, protocol XML files
  Resources/                     # Embedded resources (medlist.txt)
  wwwroot/                       # Static files (CSS, JS, libs)
SleepEditWeb.Tests/              # NUnit test project
docs/                            # Markdown documentation
sleepEdit_sln_only_20260213_090126/  # Legacy WinForms/WPF source
```

## Key Architectural Patterns
- **Controller → Service → SessionStore/Repository** layered architecture
- **Interface-based DI** — all services registered via interfaces in Program.cs
- **Session-based state** for protocol editor (snapshot-based undo/redo)
- **XML as canonical protocol format** — ProtocolXmlService handles serialization
- **Feature flags** via Options pattern (ProtocolEditorFeatureOptions, SleepNoteEditorFeatureOptions)
- **Anti-forgery tokens** on all POST endpoints
