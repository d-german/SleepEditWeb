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
    public void GenerateSnoring_CpapTitration_StillReportsSnoring()
    {
        var result = SleepNoteNarrativeGenerator.GenerateSnoring(Set("Mild", "Loud"), StudyType.CpapBipapTitration);
        Assert.That(result, Is.EqualTo(" Mild to loud snoring was heard."));
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
    public void GenerateEventsAndArrhythmias_WithArrhythmiaAndPlms_ReturnsTwoGrammaticalSentences()
    {
        var result = SleepNoteNarrativeGenerator.GenerateEventsAndArrhythmias(
            Set("PLMs"),
            Set("pvc"));

        Assert.That(result, Is.EqualTo(
            " Premature ventricular contractions (PVCs) were noted. PLMs were also noted."));
    }

    [Test]
    public void GenerateEventsAndArrhythmias_WithArrhythmiaOnly_ReportsNoPlms()
    {
        var result = SleepNoteNarrativeGenerator.GenerateEventsAndArrhythmias(
            Set(),
            Set("pvc"));

        Assert.That(result, Is.EqualTo(
            " Premature ventricular contractions (PVCs) were noted. No PLMs were noted."));
    }

    [Test]
    public void GenerateEventsAndArrhythmias_PlmsOnly_ReturnsPlms()
    {
        var result = SleepNoteNarrativeGenerator.GenerateEventsAndArrhythmias(Set("PLMs"), Set());
        Assert.That(result, Is.EqualTo(" PLMs were noted. No arrhythmias were noted."));
    }

    [Test]
    public void GenerateEventsAndArrhythmias_Neither_ReturnsNeither()
    {
        var result = SleepNoteNarrativeGenerator.GenerateEventsAndArrhythmias(Set(), Set());
        Assert.That(result, Is.EqualTo(" Neither arrhythmias nor PLMs were noted."));
    }

    [Test]
    public void GenerateArrhythmiaSentence_OneSelection_ReturnsSingleFinding()
    {
        var result = SleepNoteNarrativeGenerator.GenerateArrhythmiaSentence(Set("pvc"));

        Assert.That(result, Is.EqualTo(
            " Premature ventricular contractions (PVCs) were noted."));
    }

    [Test]
    public void GenerateArrhythmiaSentence_TwoSelections_UsesAnd()
    {
        var result = SleepNoteNarrativeGenerator.GenerateArrhythmiaSentence(Set("pac", "pvc"));

        Assert.That(result, Is.EqualTo(
            " Premature atrial contractions (PACs) and premature ventricular contractions (PVCs) were noted."));
    }

    [Test]
    public void GenerateArrhythmiaSentence_ThreeSelections_UsesOxfordComma()
    {
        var result = SleepNoteNarrativeGenerator.GenerateArrhythmiaSentence(
            Set("atrial-fibrillation", "pvc", "sinus-bradycardia"));

        Assert.That(result, Is.EqualTo(
            " Premature ventricular contractions (PVCs), sinus bradycardia, and atrial fibrillation (AFib) were noted."));
    }

    [Test]
    public void GenerateArrhythmiaSentence_UnknownSelection_IgnoresUnsupportedValue()
    {
        var result = SleepNoteNarrativeGenerator.GenerateArrhythmiaSentence(Set("unsupported"));

        Assert.That(result, Is.EqualTo(" No arrhythmias were noted."));
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

    // ── Treatment Info ─────────────────────────────────────────────────

    [Test]
    public void GenerateTreatmentInfo_Cpap_ReturnsInitiatedAndTitrated()
    {
        var data = CreateFormData() with
        {
            TherapyCourse =
            [
                new PapTherapyStage
                {
                    Mode = PapTherapyMode.Cpap,
                    Pressures = new PressureSettings { InitialCpap = 4, FinalCpap = 10 }
                }
            ]
        };

        var result = SleepNoteNarrativeGenerator.GenerateTreatmentInfo(data);
        Assert.That(result, Does.Contain("CPAP was initiated at 4 cm H2O"));
        Assert.That(result, Does.Contain("titrated to 10 cm H2O"));
    }

    [Test]
    public void GenerateTreatmentInfo_Bipap_ReturnsInitiatedAndTitrated()
    {
        var data = CreateFormData() with
        {
            TherapyCourse =
            [
                new PapTherapyStage
                {
                    Mode = PapTherapyMode.Bipap,
                    Pressures = new PressureSettings
                    {
                        InitialIpap = 8,
                        InitialEpap = 4,
                        FinalIpap = 30,
                        FinalEpap = 26
                    }
                }
            ]
        };

        var result = SleepNoteNarrativeGenerator.GenerateTreatmentInfo(data);
        Assert.That(result, Does.Contain("BIPAP was initiated at 8/4 cm H2O"));
        Assert.That(result, Does.Contain("titrated to 30/26 cm H2O"));
    }

    [Test]
    public void GenerateTreatmentInfo_BipapSt_IncludesBackupRate()
    {
        var data = CreateFormData() with
        {
            TherapyCourse =
            [
                new PapTherapyStage
                {
                    Mode = PapTherapyMode.BipapSt,
                    BackupRate = 10,
                    Pressures = new PressureSettings
                    {
                        InitialIpap = 8,
                        InitialEpap = 4,
                        FinalIpap = 20,
                        FinalEpap = 16
                    }
                }
            ]
        };

        var result = SleepNoteNarrativeGenerator.GenerateTreatmentInfo(data);

        Assert.That(result, Does.Contain("BIPAP ST was initiated at 8/4 cm H2O"));
        Assert.That(result, Does.Contain("with a backup rate of 10 bpm"));
        Assert.That(result, Does.Contain("titrated to 20/16 cm H2O"));
    }

    [Test]
    public void GenerateTreatmentInfo_CpapToBipap_ReportsReasonAndChronology()
    {
        var data = CreateFormData() with
        {
            TherapyCourse =
            [
                new PapTherapyStage
                {
                    Mode = PapTherapyMode.Cpap,
                    Pressures = new PressureSettings { InitialCpap = 5, FinalCpap = 20 }
                },
                new PapTherapyStage
                {
                    Mode = PapTherapyMode.Bipap,
                    TransitionReason = "Persistent obstructive events",
                    Pressures = new PressureSettings
                    {
                        InitialIpap = 20,
                        InitialEpap = 16,
                        FinalIpap = 25,
                        FinalEpap = 21
                    }
                }
            ]
        };

        var result = SleepNoteNarrativeGenerator.GenerateTreatmentInfo(data);

        Assert.That(result, Does.StartWith(" CPAP was initiated at 5 cm H2O"));
        Assert.That(result, Does.Contain(
            "Due to persistent obstructive events, therapy was changed to BIPAP at 20/16 cm H2O"));
        Assert.That(result, Does.Contain("titrated to 25/21 cm H2O"));
    }

    [TestCase("CPAP intolerance", "Due to CPAP intolerance")]
    [TestCase("REM-related events", "Due to REM-related events")]
    [TestCase("OSA persisted", "Due to OSA persisted")]
    [TestCase("Persistent central apneas", "Due to persistent central apneas")]
    public void GenerateTreatmentInfo_TransitionReason_PreservesAcronymsAndFlowsAsSentence(
        string reason,
        string expected)
    {
        var data = CreateFormData() with
        {
            TherapyCourse =
            [
                new PapTherapyStage
                {
                    Mode = PapTherapyMode.Cpap,
                    Pressures = new PressureSettings { InitialCpap = 5, FinalCpap = 11 }
                },
                new PapTherapyStage
                {
                    Mode = PapTherapyMode.Bipap,
                    TransitionReason = reason,
                    Pressures = new PressureSettings
                    {
                        InitialIpap = 11,
                        InitialEpap = 7,
                        FinalIpap = 15,
                        FinalEpap = 11
                    }
                }
            ]
        };

        var result = SleepNoteNarrativeGenerator.GenerateTreatmentInfo(data);

        Assert.That(result, Does.Contain($"{expected}, therapy was changed to BIPAP"));
        Assert.That(result, Does.Not.Contain("cPAP"));
    }

    [Test]
    public void GenerateTreatmentInfo_CpapToBipapToSt_ReportsAllStagesInOrder()
    {
        var data = CreateFormData() with
        {
            TherapyCourse =
            [
                new PapTherapyStage
                {
                    Mode = PapTherapyMode.Cpap,
                    Pressures = new PressureSettings { InitialCpap = 5, FinalCpap = 11 }
                },
                new PapTherapyStage
                {
                    Mode = PapTherapyMode.Bipap,
                    TransitionReason = "Persistent central apneas",
                    Pressures = new PressureSettings
                    {
                        InitialIpap = 11,
                        InitialEpap = 7,
                        FinalIpap = 15,
                        FinalEpap = 11
                    }
                },
                new PapTherapyStage
                {
                    Mode = PapTherapyMode.BipapSt,
                    TransitionReason = "Persistent central apneas",
                    BackupRate = 10,
                    Pressures = new PressureSettings
                    {
                        InitialIpap = 15,
                        InitialEpap = 11,
                        FinalIpap = 18,
                        FinalEpap = 14
                    }
                }
            ]
        };

        var result = SleepNoteNarrativeGenerator.GenerateTreatmentInfo(data);

        Assert.That(result.IndexOf("CPAP was initiated", StringComparison.Ordinal),
            Is.LessThan(result.IndexOf("changed to BIPAP at", StringComparison.Ordinal)));
        Assert.That(result.IndexOf("changed to BIPAP at", StringComparison.Ordinal),
            Is.LessThan(result.IndexOf("changed to BIPAP ST", StringComparison.Ordinal)));
        Assert.That(result, Does.Contain("backup rate of 10 bpm"));
    }

    [Test]
    public void GenerateTreatmentInfo_WithMaskCourse_AppendsInitialMaskAndHumidity()
    {
        var data = CreateFormData() with
        {
            TherapyCourse =
            [
                new PapTherapyStage
                {
                    Mode = PapTherapyMode.Cpap,
                    Pressures = new PressureSettings { InitialCpap = 4, FinalCpap = 8 }
                }
            ],
            MaskCourse =
            [
                new MaskSetupStage
                {
                    MaskType = "Respironics Comfort Select",
                    MaskSize = "medium",
                    ChinStrap = true
                }
            ]
        };

        var result = SleepNoteNarrativeGenerator.GenerateTreatmentInfo(data);
        Assert.That(result, Does.Contain("medium Respironics Comfort Select mask with a chin strap was used"));
        Assert.That(result, Does.Contain("Heated humidity was used"));
    }

    [Test]
    public void GenerateTreatmentInfo_MaskCourse_ReportsChinStrapAndMaskChangesInOrder()
    {
        var data = CreateFormData() with
        {
            TherapyCourse =
            [
                new PapTherapyStage
                {
                    Mode = PapTherapyMode.Cpap,
                    Pressures = new PressureSettings { InitialCpap = 4, FinalCpap = 8 }
                }
            ],
            MaskCourse =
            [
                new MaskSetupStage
                {
                    MaskType = "Nasal pillows",
                    MaskSize = "medium"
                },
                new MaskSetupStage
                {
                    MaskType = "Nasal pillows",
                    MaskSize = "medium",
                    ChinStrap = true,
                    TransitionReason = "Oral leak or mouth opening"
                },
                new MaskSetupStage
                {
                    MaskType = "Full face",
                    MaskSize = "medium",
                    TransitionReason = "Persistent oral leak despite chin strap"
                }
            ]
        };

        var result = SleepNoteNarrativeGenerator.GenerateTreatmentInfo(data);

        var initialMask = result.IndexOf("medium Nasal pillows mask was used", StringComparison.Ordinal);
        var chinStrapAdded = result.IndexOf("Due to oral leak or mouth opening, a chin strap was added", StringComparison.Ordinal);
        var fullFaceMask = result.IndexOf(
            "Due to persistent oral leak despite chin strap, the mask was changed to a medium Full face mask",
            StringComparison.Ordinal);

        Assert.That(initialMask, Is.GreaterThanOrEqualTo(0));
        Assert.That(chinStrapAdded, Is.GreaterThan(initialMask));
        Assert.That(fullFaceMask, Is.GreaterThan(chinStrapAdded));
    }

    [Test]
    public void GenerateTreatmentInfo_NoTitration_ReturnsEmpty()
    {
        var data = CreateFormData();
        var result = SleepNoteNarrativeGenerator.GenerateTreatmentInfo(data);
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
        Assert.That(result, Does.Contain("PLMs were noted"));
        Assert.That(result, Does.Contain("position effect is noted"));
    }

    [Test]
    public void Generate_CpapTitration_IncludesSnoringButSuppressesRespiratory()
    {
        var data = CreateFormData() with
        {
            StudyType = StudyType.CpapBipapTitration,
            BodyPositions = Set("Supine"),
            SnoringLevels = Set("Mild", "Moderate"),
            Events = Set("RespiratoryEvents"),
            TherapyCourse =
            [
                new PapTherapyStage
                {
                    Mode = PapTherapyMode.Cpap,
                    Pressures = new PressureSettings { InitialCpap = 5, FinalCpap = 12 }
                }
            ],
            MaskCourse =
            [
                new MaskSetupStage
                {
                    MaskType = "F&P Flexifit HC407",
                    MaskSize = "large"
                }
            ]
        };

        var result = SleepNoteNarrativeGenerator.Generate(data);

        Assert.That(result, Does.Contain("snoring was heard"));
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
            BodyPositions = Set("Supine", "Lateral"),
            SnoringLevels = Set("Moderate", "Loud"),
            TherapyCourse =
            [
                new PapTherapyStage
                {
                    Mode = PapTherapyMode.Cpap,
                    Pressures = new PressureSettings { InitialCpap = 6, FinalCpap = 10 }
                }
            ]
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
            BodyPositions = Set(),
            SnoringLevels = Set(),
            Events = Set(),
            Arrhythmias = Set(),
            Effects = Set(),
            MiscOptions = Set()
        };
}
