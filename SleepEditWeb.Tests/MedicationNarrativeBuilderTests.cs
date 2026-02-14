using SleepEditWeb.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace SleepEditWeb.Tests;

[TestFixture]
public class MedicationNarrativeBuilderTests
{
    private MedicationNarrativeBuilder _builder;

    [SetUp]
    public void SetUp()
    {
        _builder = new MedicationNarrativeBuilder(NullLogger<MedicationNarrativeBuilder>.Instance);
    }

    [Test]
    public void Build_NoValidSelections_ReturnsNoneDocumentedNarrative()
    {
        // Arrange
        var selectedMedicationNames = new List<string?> { null, string.Empty, "   " };
        var knownMedicationNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Aspirin" };

        // Act
        var result = _builder.Build(
            selectedMedicationNames.Select(name => name!).ToList(),
            knownMedicationNames);

        // Assert
        Assert.That(result.Text, Is.EqualTo("Medications: none documented."));
        Assert.That(result.UnknownMedications, Is.Empty);
    }

    [Test]
    public void Build_KnownAndUnknownSelections_AppendsUnknownMarkerDeterministically()
    {
        // Arrange
        var selectedMedicationNames = new[] { " Aspirin ", "UnknownMed" };
        var knownMedicationNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "aspirin" };

        // Act
        var result = _builder.Build(selectedMedicationNames, knownMedicationNames);

        // Assert
        Assert.That(result.Text, Is.EqualTo("Medications: Aspirin, UnknownMed [UNKNOWN MEDICATION]."));
        Assert.That(result.UnknownMedications, Is.EqualTo(new[] { "UnknownMed" }));
    }

    [Test]
    public void Build_DeduplicatesCaseInsensitive_PreservesFirstOccurrenceOrderAndCasing()
    {
        // Arrange
        var selectedMedicationNames = new[]
        {
            "  ASpirin  ",
            "aspirin",
            " Melatonin ",
            "melatonin "
        };

        var knownMedicationNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Aspirin" };

        // Act
        var result = _builder.Build(selectedMedicationNames, knownMedicationNames);

        // Assert
        Assert.That(result.Text, Is.EqualTo("Medications: ASpirin, Melatonin [UNKNOWN MEDICATION]."));
        Assert.That(result.UnknownMedications, Is.EqualTo(new[] { "Melatonin" }));
    }
}
