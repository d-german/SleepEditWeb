using SleepEditWeb.Models;

namespace SleepEditWeb.Services;

public static class SleepNoteNarrativeGenerator
{
    public static string Generate(SleepNoteFormData data)
    {
        var parts = new List<string>();

        AppendMiscOptions(parts, data.MiscOptions);
        AppendBodyPosition(parts, data.BodyPositions);
        AppendSnoring(parts, data.SnoringLevels, data.StudyType);
        AppendRespiratoryInfo(parts, data.Events, data.StudyType);
        AppendCpapCriteria(parts, data.MiscOptions);
        AppendTreatmentInfo(parts, data);
        AppendEventsAndArrhythmias(parts, data.Events, data.Arrhythmias);
        AppendPatientMachine(parts, data);
        AppendEffects(parts, data.Effects);

        return string.Join("", parts).Trim();
    }

    internal static string GenerateBodyPosition(IReadOnlySet<string> positions)
    {
        var has = (string key) => positions.Contains(key);
        var supine = has("Supine");
        var lateral = has("Lateral");
        var prone = has("Prone");

        var descriptor = (supine, lateral, prone) switch
        {
            (true, true, true) => "in all positions.",
            (true, true, false) => "laterally and supine.",
            (true, false, true) => "prone and supine.",
            (true, false, false) => "supine only.",
            (false, true, true) => "laterally and prone.",
            (false, true, false) => "laterally only.",
            (false, false, true) => "prone only.",
            _ => "supine only."
        };

        return $"The patient slept {descriptor}";
    }

    internal static string GenerateSnoring(IReadOnlySet<string> levels, StudyType studyType)
    {
        var mild = levels.Contains("Mild");
        var moderate = levels.Contains("Moderate");
        var loud = levels.Contains("Loud");

        var intensity = (mild, moderate, loud) switch
        {
            (false, false, false) => " No",
            (true, false, false) => " Mild",
            (false, true, false) => " Moderate",
            (false, false, true) => " Loud",
            (true, true, false) => " Mild to moderate",
            (false, true, true) => " Moderate to loud",
            (true, false, true) => " Mild to loud",
            (true, true, true) => " Mild to loud",
        };

        return $"{intensity} snoring was heard.";
    }

    internal static string GenerateRespiratoryInfo(IReadOnlySet<string> events, StudyType studyType)
    {
        if (studyType == StudyType.CpapBipapTitration)
            return string.Empty;

        return events.Contains("RespiratoryEvents")
            ? " Respiratory events were observed."
            : string.Empty;
    }

    internal static string GenerateTreatmentInfo(SleepNoteFormData data)
    {
        if (data.TherapyCourse.Count == 0)
            return string.Empty;

        var parts = new List<string>();

        for (var index = 0; index < data.TherapyCourse.Count; index++)
        {
            var stage = data.TherapyCourse[index];
            parts.Add(index == 0
                ? GenerateInitialTherapyStage(stage)
                : GenerateTherapyTransition(stage));
        }

        if (!string.IsNullOrEmpty(data.MaskType))
            parts.Add($" A {data.MaskSize} {data.MaskType} mask was used.");

        if (data.ChinStrap)
            parts.Add(" A chin strap was used.");

        if (data.HeatedHumidifier)
            parts.Add(" Heated humidity was used.");

        return string.Join("", parts);
    }

    private static string GenerateInitialTherapyStage(PapTherapyStage stage) =>
        stage.Mode switch
        {
            PapTherapyMode.Cpap =>
                $" CPAP was initiated at {stage.Pressures.InitialCpap} cm H2O and was titrated to {stage.Pressures.FinalCpap} cm H2O.",
            PapTherapyMode.Bipap =>
                $" BIPAP was initiated at {stage.Pressures.InitialIpap}/{stage.Pressures.InitialEpap} cm H2O and was titrated to {stage.Pressures.FinalIpap}/{stage.Pressures.FinalEpap} cm H2O.",
            PapTherapyMode.BipapSt =>
                $" BIPAP ST was initiated at {stage.Pressures.InitialIpap}/{stage.Pressures.InitialEpap} cm H2O{GenerateBackupRatePhrase(stage.BackupRate)} and was titrated to {stage.Pressures.FinalIpap}/{stage.Pressures.FinalEpap} cm H2O.",
            _ => string.Empty
        };

    private static string GenerateTherapyTransition(PapTherapyStage stage)
    {
        var reason = string.IsNullOrWhiteSpace(stage.TransitionReason)
            ? string.Empty
            : $" Due to {FormatSentenceFragment(stage.TransitionReason)},";
        var prefix = reason.Length == 0
            ? " Therapy"
            : $"{reason} therapy";

        var treatment = stage.Mode switch
        {
            PapTherapyMode.Cpap =>
                $"CPAP at {stage.Pressures.InitialCpap} cm H2O and was titrated to {stage.Pressures.FinalCpap} cm H2O.",
            PapTherapyMode.Bipap =>
                $"BIPAP at {stage.Pressures.InitialIpap}/{stage.Pressures.InitialEpap} cm H2O and was titrated to {stage.Pressures.FinalIpap}/{stage.Pressures.FinalEpap} cm H2O.",
            PapTherapyMode.BipapSt =>
                $"BIPAP ST at {stage.Pressures.InitialIpap}/{stage.Pressures.InitialEpap} cm H2O{GenerateBackupRatePhrase(stage.BackupRate)} and was titrated to {stage.Pressures.FinalIpap}/{stage.Pressures.FinalEpap} cm H2O.",
            _ => string.Empty
        };

        return treatment.Length == 0
            ? string.Empty
            : $"{prefix} was changed to {treatment}";
    }

    private static string GenerateBackupRatePhrase(int? backupRate) =>
        backupRate is > 0
            ? $" with a backup rate of {backupRate} bpm"
            : string.Empty;

    private static string FormatSentenceFragment(string value)
    {
        value = value.Trim();
        if (value.Length == 0)
            return value;

        var startsWithAcronym =
            value.Length > 1 &&
            char.IsUpper(value[0]) &&
            char.IsUpper(value[1]);

        return startsWithAcronym
            ? value
            : char.ToLowerInvariant(value[0]) + value[1..];
    }

    internal static string GenerateEventsAndArrhythmias(
        IReadOnlySet<string> events,
        IReadOnlySet<string> arrhythmias)
    {
        var plm = events.Contains("PLMs");
        var hasArrhythmias = arrhythmias.Count > 0;

        return (hasArrhythmias, plm) switch
        {
            (true, true) => $"{GenerateArrhythmiaSentence(arrhythmias)} PLMs were also noted.",
            (true, false) => $"{GenerateArrhythmiaSentence(arrhythmias)} No PLMs were noted.",
            (false, true) => " PLMs were noted. No arrhythmias were noted.",
            (false, false) => " Neither arrhythmias nor PLMs were noted.",
        };
    }

    internal static string GenerateArrhythmiaSentence(IReadOnlySet<string> arrhythmias)
    {
        var phrases = ArrhythmiaCatalog.Common
            .Where(option => arrhythmias.Contains(option.Id))
            .Select(option => option.NarrativePhrase)
            .ToList();

        if (phrases.Count == 0)
            return " No arrhythmias were noted.";

        var list = FormatNaturalLanguageList(phrases);
        var sentenceStart = char.ToUpperInvariant(list[0]) + list[1..];
        return $" {sentenceStart} were noted.";
    }

    internal static string GenerateEffects(IReadOnlySet<string> effects)
    {
        var parts = new List<string>();

        if (effects.Contains("PositionEffect"))
            parts.Add(" A position effect is noted.");

        if (effects.Contains("RemEffect"))
            parts.Add(" A REM effect is noted.");

        return string.Join("", parts);
    }

    internal static string GenerateMiscOptions(IReadOnlySet<string> miscOptions)
    {
        var parts = new List<string>();

        if (miscOptions.Contains("Ambien"))
            parts.Add(" The patient took 10 mg Ambien as per protocol.");

        if (miscOptions.Contains("O2Mask"))
            parts.Add(" The patient was placed on 15 lpm O2 via NRB mask as per protocol.");

        return string.Join("", parts);
    }

    internal static string GeneratePatientMachine(SleepNoteFormData data)
    {
        if (!data.PatientHasMachine)
            return string.Empty;

        return $" Patient has and brought machine. Pressure verified at {data.PressureVerifiedAt} cm H2O and changed to {data.PressureChangedTo} cm H2O.";
    }

    private static void AppendMiscOptions(List<string> parts, IReadOnlySet<string> miscOptions)
    {
        var text = GenerateMiscOptions(miscOptions);
        if (!string.IsNullOrEmpty(text))
            parts.Add(text);
    }

    private static void AppendBodyPosition(List<string> parts, IReadOnlySet<string> positions)
    {
        parts.Add(" " + GenerateBodyPosition(positions));
    }

    private static void AppendSnoring(List<string> parts, IReadOnlySet<string> levels, StudyType studyType)
    {
        var text = GenerateSnoring(levels, studyType);
        if (!string.IsNullOrEmpty(text))
            parts.Add(text);
    }

    private static void AppendRespiratoryInfo(List<string> parts, IReadOnlySet<string> events, StudyType studyType)
    {
        var text = GenerateRespiratoryInfo(events, studyType);
        if (!string.IsNullOrEmpty(text))
            parts.Add(text);
    }

    private static void AppendCpapCriteria(List<string> parts, IReadOnlySet<string> miscOptions)
    {
        if (miscOptions.Contains("TooLateForCpap"))
            parts.Add(" The patient did not meet criteria for CPAP until too late to begin treatment.");

        if (miscOptions.Contains("NoSplitNightCriteria"))
            parts.Add(" The patient did not meet split night criteria to initiate CPAP.");
    }

    private static void AppendTreatmentInfo(List<string> parts, SleepNoteFormData data)
    {
        if (data.TherapyCourse.Count == 0)
            return;

        if (data.StudyType is not (StudyType.CpapBipapTitration or StudyType.SplitNight))
            return;

        var text = GenerateTreatmentInfo(data);
        if (!string.IsNullOrEmpty(text))
            parts.Add(text);
    }

    private static void AppendEventsAndArrhythmias(
        List<string> parts,
        IReadOnlySet<string> events,
        IReadOnlySet<string> arrhythmias)
    {
        parts.Add(GenerateEventsAndArrhythmias(events, arrhythmias));
    }

    private static void AppendPatientMachine(List<string> parts, SleepNoteFormData data)
    {
        var text = GeneratePatientMachine(data);
        if (!string.IsNullOrEmpty(text))
            parts.Add(text);
    }

    private static void AppendEffects(List<string> parts, IReadOnlySet<string> effects)
    {
        var text = GenerateEffects(effects);
        if (!string.IsNullOrEmpty(text))
            parts.Add(text);
    }

    private static string FormatNaturalLanguageList(IReadOnlyList<string> items)
    {
        return items.Count switch
        {
            0 => string.Empty,
            1 => items[0],
            2 => $"{items[0]} and {items[1]}",
            _ => $"{string.Join(", ", items.Take(items.Count - 1))}, and {items[^1]}"
        };
    }
}
