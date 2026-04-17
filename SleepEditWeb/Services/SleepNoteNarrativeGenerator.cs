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
        AppendEventsAndArrhythmias(parts, data.Events);
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
        if (data.TitrationMode != TitrationMode.Cpap && data.TitrationMode != TitrationMode.Bipap)
            return string.Empty;

        var parts = new List<string>();

        if (data.TitrationMode == TitrationMode.Cpap)
        {
            parts.Add($" CPAP was initiated at {data.Pressures?.InitialCpap} cm H2O and was titrated to {data.Pressures?.FinalCpap} cm H2O.");
        }
        else
        {
            parts.Add($" BIPAP was initiated at {data.Pressures?.InitialIpap}/{data.Pressures?.InitialEpap} cm H2O and was increased to {data.Pressures?.FinalIpap}/{data.Pressures?.FinalEpap} cm H2O.");
        }

        if (!string.IsNullOrEmpty(data.MaskType))
            parts.Add($" A {data.MaskSize} {data.MaskType} mask was used.");

        if (data.ChinStrap)
            parts.Add(" A chin strap was used.");

        if (data.HeatedHumidifier)
            parts.Add(" Heated humidity was used.");

        return string.Join("", parts);
    }

    internal static string GenerateEventsAndArrhythmias(IReadOnlySet<string> events)
    {
        var arr = events.Contains("Arrhythmias");
        var plm = events.Contains("PLMs");

        return (arr, plm) switch
        {
            (true, true) => " Arrhythmias and PLM's were noted.",
            (true, false) => " Arrhythmias were noted. No PLM's were noted.",
            (false, true) => " PLM's were noted. No arrhythmias were noted.",
            (false, false) => " Neither arrhythmias nor PLM's were noted.",
        };
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
        if (data.TitrationMode is not (TitrationMode.Cpap or TitrationMode.Bipap))
            return;

        if (data.StudyType is not (StudyType.CpapBipapTitration or StudyType.SplitNight))
            return;

        var text = GenerateTreatmentInfo(data);
        if (!string.IsNullOrEmpty(text))
            parts.Add(text);
    }

    private static void AppendEventsAndArrhythmias(List<string> parts, IReadOnlySet<string> events)
    {
        parts.Add(GenerateEventsAndArrhythmias(events));
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
}
