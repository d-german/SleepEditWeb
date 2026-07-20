namespace SleepEditWeb.Models;

public enum StudyType
{
    Polysomnogram,
    CpapBipapTitration,
    SplitNight
}

public enum PapTherapyMode
{
    Cpap,
    Bipap,
    BipapSt
}

public sealed record ArrhythmiaOption(
    string Id,
    string DisplayName,
    string NarrativePhrase);

public static class ArrhythmiaCatalog
{
    public static IReadOnlyList<ArrhythmiaOption> Common { get; } =
    [
        new("pac", "Premature atrial contractions (PACs)", "premature atrial contractions (PACs)"),
        new("pvc", "Premature ventricular contractions (PVCs)", "premature ventricular contractions (PVCs)"),
        new("sinus-bradycardia", "Sinus bradycardia", "sinus bradycardia"),
        new("sinus-tachycardia", "Sinus tachycardia", "sinus tachycardia"),
        new("atrial-fibrillation", "Atrial fibrillation (AFib)", "atrial fibrillation (AFib)")
    ];
}

public sealed record PressureSettings
{
    public int? InitialCpap { get; init; }
    public int? FinalCpap { get; init; }
    public int? InitialIpap { get; init; }
    public int? FinalIpap { get; init; }
    public int? InitialEpap { get; init; }
    public int? FinalEpap { get; init; }
}

public sealed record PapTherapyStage
{
    public PapTherapyMode Mode { get; init; } = PapTherapyMode.Cpap;
    public PressureSettings Pressures { get; init; } = new();
    public int? BackupRate { get; init; }
    public string? TransitionReason { get; init; }
}

public sealed record SleepNoteFormData
{
    public StudyType StudyType { get; init; } = StudyType.Polysomnogram;
    public IReadOnlyList<PapTherapyStage> TherapyCourse { get; init; } = [];

    public IReadOnlySet<string> BodyPositions { get; init; } = new HashSet<string>();
    public IReadOnlySet<string> SnoringLevels { get; init; } = new HashSet<string>();
    public IReadOnlySet<string> Events { get; init; } = new HashSet<string>();
    public IReadOnlySet<string> Arrhythmias { get; init; } = new HashSet<string>();
    public IReadOnlySet<string> MiscOptions { get; init; } = new HashSet<string>();
    public IReadOnlySet<string> Effects { get; init; } = new HashSet<string>();

    public string? MaskType { get; init; }
    public string? MaskSize { get; init; }
    public bool ChinStrap { get; init; }
    public bool HeatedHumidifier { get; init; }
    public bool PatientHasMachine { get; init; }
    public int? PressureVerifiedAt { get; init; }
    public int? PressureChangedTo { get; init; }
}

public sealed record SleepNoteConfiguration
{
    public List<string> MaskTypes { get; init; } = [];
    public List<string> MaskSizes { get; init; } = [];
    public List<string> TechnicianNames { get; init; } = [];
    public List<int> PressureValues { get; init; } = [4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20];
}

public sealed record SleepNoteGeneratedResult(string NarrativeText, DateTime GeneratedUtc);
