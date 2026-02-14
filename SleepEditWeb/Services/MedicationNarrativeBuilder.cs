using SleepEditWeb.Models;
using Microsoft.Extensions.Logging;

namespace SleepEditWeb.Services;

public interface IMedicationNarrativeBuilder
{
    MedicationNarrative Build(IReadOnlyCollection<string> selectedMedicationNames, IReadOnlySet<string> knownMedicationNames);
}

public sealed class MedicationNarrativeBuilder : IMedicationNarrativeBuilder
{
    private const string UnknownMarker = "[UNKNOWN MEDICATION]";
    private readonly ILogger<MedicationNarrativeBuilder> _logger;

    public MedicationNarrativeBuilder(ILogger<MedicationNarrativeBuilder> logger)
    {
        _logger = logger;
    }

    public MedicationNarrative Build(IReadOnlyCollection<string> selectedMedicationNames, IReadOnlySet<string> knownMedicationNames)
    {
        var normalized = Normalize(selectedMedicationNames);
        _logger.LogDebug("Medication narrative build requested. NormalizedSelectionCount: {Count}", normalized.Count);
        if (normalized.Count == 0)
        {
            return new MedicationNarrative
            {
                Text = "Medications: none documented.",
                UnknownMedications = []
            };
        }

        var rendered = normalized.Select(name => Render(name, knownMedicationNames)).ToList();
        var unknowns = normalized.Where(name => !knownMedicationNames.Contains(name)).ToList();
        _logger.LogDebug("Medication narrative build completed. UnknownCount: {UnknownCount}", unknowns.Count);

        return new MedicationNarrative
        {
            Text = $"Medications: {string.Join(", ", rendered)}.",
            UnknownMedications = unknowns
        };
    }

    private static List<string> Normalize(IReadOnlyCollection<string> selectedMedicationNames)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var ordered = new List<string>();

        foreach (var medicationName in selectedMedicationNames)
        {
            var normalized = medicationName?.Trim();
            if (string.IsNullOrWhiteSpace(normalized))
            {
                continue;
            }

            if (seen.Add(normalized))
            {
                ordered.Add(normalized);
            }
        }

        return ordered;
    }

    private static string Render(string medicationName, IReadOnlySet<string> knownMedicationNames)
    {
        return knownMedicationNames.Contains(medicationName)
            ? medicationName
            : $"{medicationName} {UnknownMarker}";
    }
}
