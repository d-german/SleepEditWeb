using SleepEditWeb.Models;
using SleepEditWeb.Services;

namespace SleepEditWeb.Tests;

[TestFixture]
public sealed class SleepNoteNarrativeGeneratorTests
{
    // ── Body Position ──────────────────────────────────────────────────

    [Test]
    public void GenerateBodyPosition_SupineOnly_ReturnsSinglePosition()
    {
        var result = SleepNoteNarrativeGenerator.GenerateBodyPosition(Set("Supine"));
        Assert.That(result, Is.EqualTo("The patient slept supine only."));
    }

    [Test]
    public void GenerateBodyPosition_LateralOnly_ReturnsSinglePosition()
    {
        var result = SleepNoteNarrativeGenerator.GenerateBodyPosition(Set("Lateral"));
        Assert.That(result, Is.EqualTo("The patient slept laterally only."));
    }

    [Test]
    public void GenerateBodyPosition_ProneOnly_ReturnsSinglePosition()
    {
        var result = SleepNoteNarrativeGenerator.GenerateBodyPosition(Set("Prone"));
        Assert.That(result, Is.EqualTo("The patient slept prone only."));
    }

    [Test]
    public void GenerateBodyPosition_LateralAndSupine_ReturnsCombination()
    {
        var result = SleepNoteNarrativeGenerator.GenerateBodyPosition(Set("Supine", "Lateral"));
        Assert.That(result, Is.EqualTo("The patient slept laterally and supine."));
    }

    [Test]
    public void GenerateBodyPosition_LateralAndProne_ReturnsCombination()
    {
        var result = SleepNoteNarrativeGenerator.GenerateBodyPosition(Set("Lateral", "Prone"));
        Assert.That(result, Is.EqualTo("The patient slept laterally and prone."));
    }

    [Test]
    public void GenerateBodyPosition_ProneAndSupine_ReturnsCombination()
    {
        var result = SleepNoteNarrativeGenerator.GenerateBodyPosition(Set("Supine", "Prone"));
        Assert.That(result, Is.EqualTo("The patient slept prone and supine."));
    }

    [Test]
    public void GenerateBodyPosition_AllThree_ReturnsAllPositions()
    {
        var result = SleepNoteNarrativeGenerator.GenerateBodyPosition(Set("Supine", "Lateral", "Prone"));
        Assert.That(result, Is.EqualTo("The patient slept in all positions."));
    }

    [Test]
    public void GenerateBodyPosition_Empty_ReturnsDefaultSupine()
    {
        var result = SleepNoteNarrativeGenerator.GenerateBodyPosition(Set());
        Assert.That(result, Is.EqualTo("The patient slept supine only."));
    }

    // ── Snoring ────────────────────────────────────────────────────────

    [Test]
    public void GenerateSnoring_None_ReturnsNoSnoring()
    {
        var result = SleepNoteNarrativeGenerator.GenerateSnoring(Set(), StudyType.Polysomnogram);
        Assert.That(result, Is.EqualTo(" No snoring was heard."));
    }

    [Test]
    public void GenerateSnoring_MildOnly_ReturnsMild()
    {
        var result = SleepNoteNarrativeGenerator.GenerateSnoring(Set("Mild"), StudyType.Polysomnogram);
        Assert.That(result, Is.EqualTo(" Mild snoring was heard."));
    }

    [Test]
    public void GenerateSnoring_ModerateOnly_ReturnsModerate()
    {
        var result = SleepNoteNarrativeGenerator.GenerateSnoring(Set("Moderate"), StudyType.Polysomnogram);
        Assert.That(result, Is.EqualTo(" Moderate snoring was heard."));
    }

    [Test]
    public void GenerateSnoring_LoudOnly_ReturnsLoud()
    {
        var result = SleepNoteNarrativeGenerator.GenerateSnoring(Set("Loud"), StudyType.Polysomnogram);
        Assert.That(result, Is.EqualTo(" Loud snoring was heard."));
    }

    [Test]
    public void GenerateSnoring_MildAndModerate_ReturnsMildToModerate()
    {
        var result = SleepNoteNarrativeGenerator.GenerateSnoring(Set("Mild", "Moderate"), StudyType.Polysomnogram);
        Assert.That(result, Is.EqualTo(" Mild to moderate snoring was heard."));
    }

    [Test]
    public void GenerateSnoring_ModerateAndLoud_ReturnsModerateToLoud()
    {
        var result = SleepNoteNarrativeGenerator.GenerateSnoring(Set("Moderate", "Loud"), StudyType.Polysomnogram);
        Assert.That(result, Is.EqualTo(" Moderate to loud snoring was heard."));
    }

    [Test]
    public void GenerateSnoring_MildAndLoud_ReturnsMildToLoud()
    {
        var result = SleepNoteNarrativeGenerator.GenerateSnoring(Set("Mild", "Loud"), StudyType.Polysomnogram);
        Assert.That(result, Is.EqualTo(" Mild to loud snoring was heard."));
    }

    [Test]
    public void GenerateSnoring_AllThree_ReturnsMildToLoud()
    {
        var result = SleepNoteNarrativeGenerator.GenerateSnoring(Set("Mild", "Moderate", "Loud"), StudyType.Polysomnogram);
        Assert.That(result, Is.EqualTo(" Mild to loud snoring was heard."));
    }

    [Test]
    public void GenerateSnoring_CpapTitration_ReturnsEmpty()
    {
        var result = SleepNoteNarrativeGenerator.GenerateSnoring(Set("Mild", "Loud"), StudyType.CpapBipapTitration);
        Assert.That(result, Is.Empty);
    }

    // ── Respiratory Info ───────────────────────────────────────────────

    [Test]
    public void GenerateRespiratoryInfo_EventsPresent_ReturnsObserved()
    {
        var result = SleepNoteNarrativeGenerator.GenerateRespiratoryInfo(Set("RespiratoryEvents"), StudyType.Polysomnogram);
        Assert.That(result, Is.EqualTo(" Respiratory events were observed."));
    }

    [Test]
    public void GenerateRespiratoryInfo_NoEvents_ReturnsEmpty()
    {
        var result = SleepNoteNarrativeGenerator.GenerateRespiratoryInfo(Set(), StudyType.Polysomnogram);
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void GenerateRespiratoryInfo_CpapTitration_ReturnsEmpty()
    {
        var result = SleepNoteNarrativeGenerator.GenerateRespiratoryInfo(Set("RespiratoryEvents"), StudyType.CpapBipapTitration);
        Assert.That(result, Is.Empty);
    }

    // ── Events and Arrhythmias ─────────────────────────────────────────

    [Test]
    public void GenerateEventsAndArrhythmias_Both_ReturnsCombined()
    {
        var result = SleepNoteNarrativeGenerator.GenerateEventsAndArrhythmias(Set("Arrhythmias", "PLMs"));
        Assert.That(result, Is.EqualTo(" Arrhythmias and PLM's were noted."));
    }

    [Test]
    public void GenerateEventsAndArrhythmias_ArrhythmiasOnly_ReturnsArrhythmias()
    {
        var result = SleepNoteNarrativeGenerator.GenerateEventsAndArrhythmias(Set("Arrhythmias"));
        Assert.That(result, Is.EqualTo(" Arrhythmias were noted. No PLM's were noted."));
    }

    [Test]
    public void GenerateEventsAndArrhythmias_PlmsOnly_ReturnsPlms()
    {
        var result = SleepNoteNarrativeGenerator.GenerateEventsAndArrhythmias(Set("PLMs"));
        Assert.That(result, Is.EqualTo(" PLM's were noted. No arrhythmias were noted."));
    }

    [Test]
    public void GenerateEventsAndArrhythmias_Neither_ReturnsNeither()
    {
        var result = SleepNoteNarrativeGenerator.GenerateEventsAndArrhythmias(Set());
        Assert.That(result, Is.EqualTo(" Neither arrhythmias nor PLM's were noted."));
    }

    // ── Effects ────────────────────────────────────────────────────────

    [Test]
    public void GenerateEffects_PositionEffect_ReturnsPositionNote()
    {
        var result = SleepNoteNarrativeGenerator.GenerateEffects(Set("PositionEffect"));
        Assert.That(result, Is.EqualTo(" A position effect is noted."));
    }

    [Test]
    public void GenerateEffects_RemEffect_ReturnsRemNote()
    {
        var result = SleepNoteNarrativeGenerator.GenerateEffects(Set("RemEffect"));
        Assert.That(result, Is.EqualTo(" A REM effect is noted."));
    }

    [Test]
    public void GenerateEffects_Both_ReturnsBothNotes()
    {
        var result = SleepNoteNarrativeGenerator.GenerateEffects(Set("PositionEffect", "RemEffect"));
        Assert.That(result, Is.EqualTo(" A position effect is noted. A REM effect is noted."));
    }

    [Test]
    public void GenerateEffects_None_ReturnsEmpty()
    {
        var result = SleepNoteNarrativeGenerator.GenerateEffects(Set());
        Assert.That(result, Is.Empty);
    }

    // ── Misc Options ───────────────────────────────────────────────────

    [Test]
    public void GenerateMiscOptions_Ambien_ReturnsAmbienText()
    {
        var result = SleepNoteNarrativeGenerator.GenerateMiscOptions(Set("Ambien"));
        Assert.That(result, Does.Contain("10 mg Ambien as per protocol"));
    }

    [Test]
    public void GenerateMiscOptions_O2Mask_ReturnsO2Text()
    {
        var result = SleepNoteNarrativeGenerator.GenerateMiscOptions(Set("O2Mask"));
        Assert.That(result, Does.Contain("15 lpm O2 via NRB mask"));
    }

    [Test]
    public void GenerateMiscOptions_None_ReturnsEmpty()
    {
        var result = SleepNoteNarrativeGenerator.GenerateMiscOptions(Set());
        Assert.That(result, Is.Empty);
    }

    // ── Treatment Info ─────────────────────────────────────────────────

    [Test]
    public void GenerateTreatmentInfo_Cpap_ReturnsInitiatedAndTitrated()
    {
        var data = CreateFormData() with
        {
            TitrationMode = TitrationMode.Cpap,
            Pressures = new PressureSettings { InitialCpap = 4, FinalCpap = 10 }
        };

        var result = SleepNoteNarrativeGenerator.GenerateTreatmentInfo(data);
        Assert.That(result, Does.Contain("CPAP was initiated at 4 cm H2O"));
        Assert.That(result, Does.Contain("titrated to 10 cm H2O"));
    }

    [Test]
    public void GenerateTreatmentInfo_Bipap_ReturnsInitiatedAndIncreased()
    {
        var data = CreateFormData() with
        {
            TitrationMode = TitrationMode.Bipap,
            Pressures = new PressureSettings
            {
                InitialIpap = 8,
                InitialEpap = 4,
                FinalIpap = 14,
                FinalEpap = 10
            }
        };

        var result = SleepNoteNarrativeGenerator.GenerateTreatmentInfo(data);
        Assert.That(result, Does.Contain("BIPAP was initiated at 8/4 cm H2O"));
        Assert.That(result, Does.Contain("increased to 14/10 cm H2O"));
    }

    [Test]
    public void GenerateTreatmentInfo_WithMaskAndAccessories_AppendsMaskInfo()
    {
        var data = CreateFormData() with
        {
            TitrationMode = TitrationMode.Cpap,
            Pressures = new PressureSettings { InitialCpap = 4, FinalCpap = 8 },
            MaskType = "Respironics Comfort Select",
            MaskSize = "medium",
            ChinStrap = true,
            HeatedHumidifier = true
        };

        var result = SleepNoteNarrativeGenerator.GenerateTreatmentInfo(data);
        Assert.That(result, Does.Contain("medium Respironics Comfort Select mask was used"));
        Assert.That(result, Does.Contain("chin strap was used"));
        Assert.That(result, Does.Contain("Heated humidity was used"));
    }

    [Test]
    public void GenerateTreatmentInfo_NoTitration_ReturnsEmpty()
    {
        var data = CreateFormData() with { TitrationMode = TitrationMode.None };
        var result = SleepNoteNarrativeGenerator.GenerateTreatmentInfo(data);
        Assert.That(result, Is.Empty);
    }

    // ── Patient Machine ────────────────────────────────────────────────

    [Test]
    public void GeneratePatientMachine_HasMachine_ReturnsMachineInfo()
    {
        var data = CreateFormData() with
        {
            PatientHasMachine = true,
            PressureVerifiedAt = 8,
            PressureChangedTo = 12
        };

        var result = SleepNoteNarrativeGenerator.GeneratePatientMachine(data);
        Assert.That(result, Does.Contain("Patient has and brought machine"));
        Assert.That(result, Does.Contain("Pressure verified at 8 cm H2O"));
        Assert.That(result, Does.Contain("changed to 12 cm H2O"));
    }

    [Test]
    public void GeneratePatientMachine_NoMachine_ReturnsEmpty()
    {
        var data = CreateFormData();
        var result = SleepNoteNarrativeGenerator.GeneratePatientMachine(data);
        Assert.That(result, Is.Empty);
    }

    // ── Full Narrative Integration ──────────────────────────────────────

    [Test]
    public void Generate_PsgStudy_ProducesCompleteNarrative()
    {
        var data = CreateFormData() with
        {
            StudyType = StudyType.Polysomnogram,
            BodyPositions = Set("Supine", "Lateral"),
            SnoringLevels = Set("Mild"),
            Events = Set("PLMs"),
            Effects = Set("PositionEffect")
        };

        var result = SleepNoteNarrativeGenerator.Generate(data);

        Assert.That(result, Does.Contain("laterally and supine"));
        Assert.That(result, Does.Contain("Mild snoring was heard"));
        Assert.That(result, Does.Contain("PLM's were noted"));
        Assert.That(result, Does.Contain("position effect is noted"));
    }

    [Test]
    public void Generate_CpapTitration_SuppressesSnoringAndRespiratory()
    {
        var data = CreateFormData() with
        {
            StudyType = StudyType.CpapBipapTitration,
            TitrationMode = TitrationMode.Cpap,
            BodyPositions = Set("Supine"),
            SnoringLevels = Set("Mild", "Moderate"),
            Events = Set("RespiratoryEvents"),
            Pressures = new PressureSettings { InitialCpap = 5, FinalCpap = 12 },
            MaskType = "F&P Flexifit HC407",
            MaskSize = "large"
        };

        var result = SleepNoteNarrativeGenerator.Generate(data);

        Assert.That(result, Does.Not.Contain("snoring"));
        Assert.That(result, Does.Not.Contain("Respiratory events"));
        Assert.That(result, Does.Contain("CPAP was initiated"));
        Assert.That(result, Does.Contain("F&P Flexifit HC407 mask was used"));
    }

    [Test]
    public void Generate_SplitNight_IncludesSnoringAndTreatment()
    {
        var data = CreateFormData() with
        {
            StudyType = StudyType.SplitNight,
            TitrationMode = TitrationMode.Cpap,
            BodyPositions = Set("Supine", "Lateral"),
            SnoringLevels = Set("Moderate", "Loud"),
            Pressures = new PressureSettings { InitialCpap = 6, FinalCpap = 10 }
        };

        var result = SleepNoteNarrativeGenerator.Generate(data);

        Assert.That(result, Does.Contain("Moderate to loud snoring was heard"));
        Assert.That(result, Does.Contain("CPAP was initiated at 6 cm H2O"));
    }

    // ── Helpers ─────────────────────────────────────────────────────────

    private static IReadOnlySet<string> Set(params string[] items) =>
        new HashSet<string>(items);

    private static SleepNoteFormData CreateFormData() =>
        new()
        {
            StudyType = StudyType.Polysomnogram,
            TitrationMode = TitrationMode.None,
            BodyPositions = Set(),
            SnoringLevels = Set(),
            Events = Set(),
            Effects = Set(),
            MiscOptions = Set()
        };
}
