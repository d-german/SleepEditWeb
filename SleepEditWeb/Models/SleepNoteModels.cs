namespace SleepEditWeb.Models;

public enum StudyType
{
    Polysomnogram,
    CpapBipapTitration,
    SplitNight
}

public enum TitrationMode
{
    None,
    Cpap,
    Bipap
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

public sealed record SleepNoteFormData
{
    public StudyType StudyType { get; init; } = StudyType.Polysomnogram;
    public TitrationMode TitrationMode { get; init; } = TitrationMode.Cpap;

    public IReadOnlySet<string> BodyPositions { get; init; } = new HashSet<string>();
    public IReadOnlySet<string> SnoringLevels { get; init; } = new HashSet<string>();
    public IReadOnlySet<string> Events { get; init; } = new HashSet<string>();
    public IReadOnlySet<string> MiscOptions { get; init; } = new HashSet<string>();
    public IReadOnlySet<string> Effects { get; init; } = new HashSet<string>();

    public PressureSettings? Pressures { get; init; }

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
