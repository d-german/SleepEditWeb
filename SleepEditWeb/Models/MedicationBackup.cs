namespace SleepEditWeb.Models;

/// <summary>
/// DTO for JSON export/import of medication database backups.
/// Not stored in database - used only for serialization.
/// </summary>
public sealed class MedicationBackup
{
    /// <summary>
    /// When the backup was exported.
    /// </summary>
    public DateTime ExportDate { get; init; }

    /// <summary>
    /// Seed version at time of export.
    /// </summary>
    public int SeedVersion { get; init; }

    /// <summary>
    /// Total number of medications in backup.
    /// </summary>
    public int TotalCount { get; init; }

    /// <summary>
    /// All medications in the backup.
    /// </summary>
    public required List<Medication> Medications { get; init; }
}
