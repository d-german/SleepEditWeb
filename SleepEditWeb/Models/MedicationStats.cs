namespace SleepEditWeb.Models;

/// <summary>
/// DTO containing database statistics for admin dashboard display.
/// Not stored in database - populated from queries.
/// </summary>
public sealed class MedicationStats
{
    /// <summary>
    /// Total number of medications in database.
    /// </summary>
    public int TotalCount { get; init; }

    /// <summary>
    /// Number of system (seed) medications.
    /// </summary>
    public int SystemMedCount { get; init; }

    /// <summary>
    /// Number of user-added medications.
    /// </summary>
    public int UserMedCount { get; init; }

    /// <summary>
    /// Current seed version in database.
    /// </summary>
    public int SeedVersion { get; init; }

    /// <summary>
    /// When database was last seeded.
    /// </summary>
    public DateTime? LastSeeded { get; init; }

    /// <summary>
    /// Diagnostic info about database location.
    /// </summary>
    public required string LoadedFrom { get; init; }
}
