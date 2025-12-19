# LiteDB Migration Plan for SleepEditWeb

**Date Created:** December 19, 2025  
**Status:** ✅ IMPLEMENTED - December 19, 2025  
**Branch:** `litedb-migration` (feature branch)

---

## Overview

Migrating SleepEditWeb from flat text file storage to LiteDB (embedded NoSQL database) for medication management. This provides structured storage, indexed searches, ACID transactions, and an admin UI for backup/restore operations.

---

## Why LiteDB?

### Problems with Current Flat File Approach
1. **Deployment complexity** - Multiple attempts needed to get medlist.txt into Docker image
2. **No structure** - Just medication names, can't add metadata later
3. **Limited search** - Simple string matching, not optimized
4. **No concurrency control** - File locks, corruption risk
5. **Can't distinguish** - System meds vs user-added meds

### LiteDB Solution
- **Single file database** - `medications.db` (like SQLite but NoSQL)
- **Embedded resource seeding** - Load from existing medlist.txt on first run
- **Structured data** - Can add categories, dosages, interactions later
- **Indexed searches** - Fast StartsWith queries on 31K+ records
- **ACID transactions** - Thread-safe, no corruption
- **Admin UI** - Backup/restore via web interface

---

## Architecture

### Data Models

```csharp
// Models/Medication.cs
public class Medication
{
    public int Id { get; set; }                    // Auto-increment
    public string Name { get; set; }               // Medication name
    public bool IsSystemMed { get; set; }          // true = from seed, false = user-added
    public DateTime CreatedAt { get; set; }        // When added
}

// Models/DatabaseMetadata.cs
public class DatabaseMetadata
{
    public int Id { get; set; }                    // Always 1 (singleton)
    public int SeedVersion { get; set; }           // Current seed version
    public DateTime LastSeeded { get; set; }       // Last seeding time
}

// Models/MedicationBackup.cs (DTO for export/import)
public class MedicationBackup
{
    public DateTime ExportDate { get; set; }
    public int SeedVersion { get; set; }
    public int TotalCount { get; set; }
    public List<Medication> Medications { get; set; }
}

// Models/MedicationStats.cs (DTO for admin dashboard)
public class MedicationStats
{
    public int TotalCount { get; set; }
    public int SystemMedCount { get; set; }
    public int UserMedCount { get; set; }
    public int SeedVersion { get; set; }
    public DateTime? LastSeeded { get; set; }
    public string LoadedFrom { get; set; }
}
```

### Repository Pattern

```csharp
// Data/IMedicationRepository.cs
public interface IMedicationRepository
{
    // Query
    IEnumerable<string> GetAllMedicationNames();
    IEnumerable<string> SearchMedications(string query);
    bool MedicationExists(string name);
    
    // CRUD
    bool AddUserMedication(string name);
    bool RemoveUserMedication(string name);  // Only user meds, not system
    
    // Admin
    MedicationBackup ExportAll();
    void ImportReplace(List<Medication> medications);
    void ImportMerge(List<Medication> medications);
    MedicationStats GetStats();
    
    // Maintenance
    void Reseed();
    void ClearUserMedications();
}

// Data/LiteDbMedicationRepository.cs
// - Singleton service (registered in Program.cs)
// - Database path: /app/Data/medications.db (Koyeb) or local Data folder
// - Initialization: Load from embedded medlist.txt on first run
// - Index on Name field for fast searches
```

### Controllers

**MedListController (REFACTORED)**
- Inject `IMedicationRepository` via constructor
- Remove static `List<string> MedList` field
- Use repository for all medication operations
- Preserve session-based selected medications (unchanged)
- Keep +add/-remove/cls syntax (unchanged)

**AdminController (NEW)**
- Route: `/Admin/Medications/{secretKey}`
- Secret URL authentication (404 if wrong key)
- Actions:
  - `Index(secretKey)` - Dashboard with stats
  - `ExportMedications(secretKey)` - Download JSON backup
  - `ImportMedications(secretKey, file, mode)` - Upload backup (merge/replace)
  - `Reseed(secretKey)` - Reset to original seed data
  - `ClearUserMeds(secretKey)` - Remove only user-added medications

### Views

**Admin/Medications.cshtml (NEW)**
- Dashboard card with stats (Total, System, User counts)
- Backup section with Export button
- Restore section with file upload + mode selection (Merge/Replace)
- Maintenance section with Reseed and Clear User Meds buttons
- Confirmation dialogs for destructive operations
- Follows existing dashboard-card styling

---

## Database Initialization Flow

```
App Starts
    │
    ▼
Check: /app/Data/medications.db exists?
    │
    ├─ NO (First Run)
    │   ├─ Create new LiteDB database
    │   ├─ Load medlist.txt from embedded resource
    │   ├─ Insert all 31,229 medications (IsSystemMed=true)
    │   ├─ Create index on Name field
    │   └─ Set DatabaseMetadata (SeedVersion=1)
    │
    └─ YES (Database Exists)
        ├─ Open existing database
        └─ Check: Code SeedVersion > DB SeedVersion?
            │
            ├─ YES (Seed Updated)
            │   ├─ Load new seed data
            │   ├─ Add new medications (preserve user meds)
            │   └─ Update SeedVersion
            │
            └─ NO (Same Version)
                └─ Use database as-is
```

---

## Persistence Strategy

### Where Data Lives

```
┌─────────────────────────────────────────────────────────────┐
│  DOCKER CONTAINER (rebuilt on every deploy)                 │
│  ┌───────────────────────────────────────────────────────┐  │
│  │  /app/SleepEditWeb.dll                                │  │
│  │    └── Embedded Resource: medlist.txt (31,229 meds)  │  │
│  └───────────────────────────────────────────────────────┘  │
│                           │                                  │
│                           │ Seeds on first run              │
│                           ▼                                  │
│  ┌───────────────────────────────────────────────────────┐  │
│  │  /app/Data/  ← PERSISTENT VOLUME (survives deploys)  │  │
│  │    └── medications.db                                 │  │
│  │          • System meds (IsSystemMed=true)             │  │
│  │          • User-added meds (IsSystemMed=false)        │  │
│  └───────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
```

### Deployment Scenarios

| Deploy Type | What Happens to User Meds | What Happens to System Meds |
|-------------|---------------------------|----------------------------|
| **Feature changes** (CSS, JS, new buttons) | ✅ Untouched | ✅ Untouched |
| **Seed list changes** (add/remove meds in medlist.txt) | ✅ Untouched | 🔄 Merged with new seed |

**Key Point:** User-added medications are NEVER lost during deployments because they live in the persistent volume, not the container.

---

## Admin UI Features

### Backup/Restore
- **Export** - Downloads `medications_backup_2025-12-19.json` with all data
- **Import Merge** - Adds new medications, preserves existing
- **Import Replace** - Complete overwrite (destructive)

### Maintenance
- **Reseed** - Reset to original 31,229 medications from embedded resource
- **Clear User Meds** - Remove only user-added medications, keep system meds

### JSON Backup Format
```json
{
  "exportDate": "2025-12-19T21:42:00Z",
  "seedVersion": 1,
  "totalCount": 31234,
  "medications": [
    { "id": 1, "name": "acetaminophen", "isSystemMed": true, "createdAt": "2025-12-19T00:00:00Z" },
    { "id": 31230, "name": "My Custom Med", "isSystemMed": false, "createdAt": "2025-12-19T15:30:00Z" }
  ]
}
```

---

## Security: Admin Access

### Secret URL Pattern
```
URL: https://sleep-edit.d-german.net/Admin/Medications/{secretKey}

Wrong key:   /Admin/Medications/abc123          → 404 Not Found
Correct key: /Admin/Medications/your-secret     → Admin Dashboard
```

### Secret Key Storage
- Hardcoded constant in `AdminController.cs` (simple)
- Or configurable via `appsettings.json` (future enhancement)

**Security Note:** This is sufficient for single-admin scenarios. No login UI needed.

---

## File Structure

```
SleepEditWeb/
├── Data/                              [NEW FOLDER]
│   ├── IMedicationRepository.cs       [NEW]
│   └── LiteDbMedicationRepository.cs  [NEW]
├── Models/
│   ├── Medication.cs                  [NEW]
│   ├── DatabaseMetadata.cs            [NEW]
│   ├── MedicationBackup.cs            [NEW]
│   └── MedicationStats.cs             [NEW]
├── Controllers/
│   ├── MedListController.cs           [MODIFY - use repository]
│   └── AdminController.cs             [NEW]
├── Views/
│   └── Admin/                         [NEW FOLDER]
│       └── Medications.cshtml         [NEW]
├── Program.cs                         [MODIFY - register repository]
├── SleepEditWeb.csproj               [MODIFY - add LiteDB package]
└── .gitignore                        [MODIFY - exclude *.db]
```

---

## Implementation Task List (24 Tasks)

### Phase 1: Package & Models (5 tasks)
1. Add LiteDB NuGet package
2. Create Medication model
3. Create DatabaseMetadata model
4. Create MedicationBackup model
5. Create MedicationStats model

### Phase 2: Repository (7 tasks)
6. Create IMedicationRepository interface
7. LiteDbMedicationRepository - Core & initialization
8. LiteDbMedicationRepository - Query methods
9. LiteDbMedicationRepository - CRUD methods
10. LiteDbMedicationRepository - Export & Stats
11. LiteDbMedicationRepository - Import methods
12. LiteDbMedicationRepository - Maintenance methods

### Phase 3: Dependency Injection & Refactoring (2 tasks)
13. Register repository in Program.cs
14. Refactor MedListController to use repository

### Phase 4: Admin Controller (4 tasks)
15. Create AdminController with secret URL
16. AdminController - Export action
17. AdminController - Import action
18. AdminController - Maintenance actions

### Phase 5: Admin UI (2 tasks)
19. Create Admin/Medications.cshtml view
20. Add JavaScript for confirmations

### Phase 6: Finalization (4 tasks)
21. Update DiagnosticInfo endpoint
22. Test local development workflow
23. Update .gitignore
24. Update Serena memory

---

## Git Workflow

### Current State
- **Branch:** `litedb-migration` (feature branch created Dec 19, 2025)
- **Base:** `main` branch
- **Status:** Clean, no commits yet

### Development Workflow
```bash
# Currently on feature branch
git branch: * litedb-migration

# Make changes, commit frequently
git add .
git commit -m "Task X: Description"

# Push to GitHub (will NOT auto-deploy to Koyeb)
git push -u origin litedb-migration

# When ready to deploy
git checkout main
git merge litedb-migration
git push origin main  # ← Triggers Koyeb auto-deploy
```

### Deployment Behavior
- **Feature branch push** → GitHub only, NO deployment (safe development)
- **Main branch push** → Triggers Koyeb auto-deploy to production
- **Manual test** → Can deploy specific branch via Koyeb dashboard (temporary)

---

## Testing Strategy

### Local Testing (Recommended)
1. Run `dotnet run` in SleepEditWeb folder
2. Test MedList page at http://localhost:5000
3. Test Admin page at http://localhost:5000/Admin/Medications/{your-secret-key}
4. Database created at `bin/Debug/net8.0/Data/medications.db`

### Production Testing (After Merge)
1. Merge `litedb-migration` → `main`
2. Koyeb auto-deploys
3. Test at https://sleep-edit.d-german.net
4. Database at `/app/Data/medications.db` (persistent volume)
5. If issues: Quick revert on main branch

---

## Important Notes

### What Stays the Same
- ✅ Session-based selected medications (unchanged)
- ✅ +add/-remove/cls syntax (unchanged)
- ✅ Autocomplete behavior (unchanged)
- ✅ UI styling and layout (unchanged)
- ✅ Embedded resource loading pattern (reused in repository)

### What Changes
- ❌ Static `List<string> MedList` → Repository pattern with DI
- ❌ File system I/O → LiteDB queries
- ❌ No distinction between system/user meds → `IsSystemMed` flag
- ➕ NEW: Admin UI for backup/restore
- ➕ NEW: Indexed searches (performance improvement)
- ➕ NEW: ACID transactions (data safety)

### NuGet Package
- **LiteDB** version 5.x (latest stable)
- Size: ~450KB single DLL
- Zero configuration required

### Koyeb Persistent Volume
- **Already configured:** `/app/Data` volume mounted
- Database will be created automatically on first run
- Survives container restarts and redeployments

---

## Next Session Quick Start

1. **Check branch:** `git branch` (should be on `litedb-migration`)
2. **View task list:** Can use task manager tools or reference this memory
3. **Start with Task 1:** Add LiteDB NuGet package
4. **Test frequently:** Run `dotnet run` after each phase
5. **Commit often:** After each working task or phase

---

## Admin URL (After Implementation)

**Production:**
```
https://sleep-edit.d-german.net/Admin/Medications/{your-secret-key-here}
```

**Local:**
```
http://localhost:5000/Admin/Medications/{your-secret-key-here}
```

---

## References

- **LiteDB Documentation:** https://www.litedb.org/
- **Current Architecture:** See `koyeb-deployment-context` memory
- **Existing Code Patterns:** See `MedListController.cs` for reference
- **Deployment Guide:** See `domain-and-koyeb-setup-guide` memory