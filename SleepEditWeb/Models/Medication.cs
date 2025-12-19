namespace SleepEditWeb.Models;

/// <summary>
/// Represents a medication in the LiteDB database.
/// </summary>
public sealed class Medication
{
    /// <summary>
    /// Auto-increment primary key.
    /// </summary>
    public int Id { get; init; }

    /// <summary>
    /// The medication name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// True if loaded from seed data, false if user-added.
    /// </summary>
    public bool IsSystemMed { get; init; }

    /// <summary>
    /// When the medication was added to the database.
    /// </summary>
    public DateTime CreatedAt { get; init; }
}
