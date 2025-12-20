using CSharpFunctionalExtensions;
using SleepEditWeb.Models;

namespace SleepEditWeb.Data;

/// <summary>
/// Repository interface for medication data access.
/// Follows Interface Segregation Principle - methods grouped by purpose.
/// </summary>
public interface IMedicationRepository
{
    /// <summary>
    /// Gets all medication names in the database.
    /// </summary>
    IEnumerable<string> GetAllMedicationNames();

    /// <summary>
    /// Searches for medications matching the query (case-insensitive StartsWith).
    /// </summary>
    IEnumerable<string> SearchMedications(string query);

    /// <summary>
    /// Checks if a medication exists in the database.
    /// </summary>
    bool MedicationExists(string name);

    /// <summary>
    /// Adds a user medication if it doesn't already exist.
    /// </summary>
    /// <returns>Result indicating success or reason for failure.</returns>
    Result AddUserMedication(string name);

    /// <summary>
    /// Removes a user-added medication (cannot remove system meds).
    /// </summary>
    /// <returns>Result indicating success or reason for failure.</returns>
    Result RemoveUserMedication(string name);

    /// <summary>
    /// Gets the path to the database file.
    /// </summary>
    string DatabasePath { get; }

    /// <summary>
    /// Exports all medications for backup.
    /// </summary>
    MedicationBackup ExportAll();

    /// <summary>
    /// Replaces all medications with imported data (destructive).
    /// </summary>
    Result ImportReplace(List<Medication> medications);

    /// <summary>
    /// Merges imported medications (adds new, preserves existing).
    /// </summary>
    Result ImportMerge(List<Medication> medications);

    /// <summary>
    /// Gets database statistics for admin dashboard.
    /// </summary>
    MedicationStats GetStats();

    /// <summary>
    /// Resets database to original seed data.
    /// </summary>
    Result Reseed();

    /// <summary>
    /// Removes all user-added medications, keeps system meds.
    /// </summary>
    Result ClearUserMedications();
}
