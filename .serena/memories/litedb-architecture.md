# LiteDB Architecture - SleepEditWeb

**Last Updated:** December 19, 2025  
**Status:** Implemented and Ready for Deployment

---

## Overview

SleepEditWeb uses LiteDB (embedded NoSQL database) for medication storage. This replaces the previous flat text file approach.

---

## Key Files

### Models (`SleepEditWeb/Models/`)
- **Medication.cs** - Entity with Id, Name, IsSystemMed, CreatedAt
- **DatabaseMetadata.cs** - Singleton for seed version tracking
- **MedicationBackup.cs** - DTO for JSON export/import
- **MedicationStats.cs** - DTO for admin dashboard statistics

### Repository (`SleepEditWeb/Data/`)
- **IMedicationRepository.cs** - Repository interface
- **LiteDbMedicationRepository.cs** - LiteDB implementation (singleton)

### Controllers (`SleepEditWeb/Controllers/`)
- **MedListController.cs** - Refactored to use repository via DI
- **AdminController.cs** - NEW: Admin operations with secret URL auth

### Views (`SleepEditWeb/Views/Admin/`)
- **Medications.cshtml** - Admin dashboard with stats, backup/restore, maintenance

---

## Database Location

| Environment | Path |
|-------------|------|
| **Production (Koyeb)** | `/app/Data/medications.db` |
| **Local Development** | `bin/Debug/net8.0/Data/medications.db` |

The `/app/Data` directory is mounted to a Koyeb persistent volume.

---

## Admin URL

**Pattern:** `/Admin/Medications/{secretKey}`

**Secret Key:** `medAdmin2025xK9!` (hardcoded in AdminController.cs)

**Production URL:**
```
https://sleep-edit.d-german.net/Admin/Medications/medAdmin2025xK9!
```

**Local URL:**
```
http://localhost:5114/Admin/Medications/medAdmin2025xK9!
```

**Security:** Wrong key returns 404 Not Found.

---

## Admin Features

1. **Dashboard** - View stats (total, system, user med counts)
2. **Export** - Download JSON backup file
3. **Import Merge** - Add new medications, preserve existing
4. **Import Replace** - Complete database overwrite (destructive)
5. **Reseed** - Reset to original 31K+ medications from embedded resource
6. **Clear User Meds** - Remove only user-added medications

---

## Seed Version Management

- **CurrentSeedVersion:** Constant in `LiteDbMedicationRepository.cs` (currently = 1)
- **On Startup:** Compares code version vs database version
- **If code > db:** Merges new seed medications while preserving user-added
- **Increment SeedVersion:** When medlist.txt is updated

---

## Medication Types

| IsSystemMed | Source | Removable by User |
|-------------|--------|-------------------|
| `true` | Seed data (medlist.txt) | ❌ No |
| `false` | User-added via +name | ✅ Yes |

---

## DI Registration

```csharp
// Program.cs
builder.Services.AddSingleton<IMedicationRepository, LiteDbMedicationRepository>();
```

Singleton because:
- LiteDB handles thread safety internally
- One database connection per app lifetime

---

## Deployment Notes

1. **First Deployment:** Database auto-seeds from embedded medlist.txt
2. **Subsequent Deploys:** User-added medications are preserved
3. **Seed Updates:** Increment SeedVersion constant and new meds merge automatically
4. **Emergency Reset:** Use Admin UI "Reseed" button

---

## Backup/Restore Workflow

### Export (Backup)
1. Go to Admin URL
2. Click "Export Medications"
3. Downloads `medications_backup_YYYY-MM-DD.json`

### Import (Restore)
1. Go to Admin URL
2. Select backup JSON file
3. Choose mode:
   - **Merge:** Preserves existing, adds new
   - **Replace:** Complete overwrite
4. Click "Import Backup"

---

## File Exclusions

`.gitignore` excludes:
- `*.db`
- `**/Data/medications.db`

---

## Related Memories

- `litedb-migration-plan` - Original planning document (marked complete)
- `koyeb-deployment-context` - Koyeb persistent volume setup
- `domain-and-koyeb-setup-guide` - Domain configuration
