namespace SleepEditWeb.Models;

/// <summary>
/// Tracks database metadata for seed version management.
/// Only one record exists in this collection (singleton pattern).
/// </summary>
public sealed class DatabaseMetadata
{
    /// <summary>
    /// Always 1 - singleton record.
    /// </summary>
    public int Id { get; set; } = 1;

    /// <summary>
    /// Current seed data version in the database.
    /// Compared against code version to determine if re-seeding needed.
    /// </summary>
    public int SeedVersion { get; set; }

    /// <summary>
    /// Timestamp of the last database seeding operation.
    /// </summary>
    public DateTime LastSeeded { get; set; }
}
