using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using SleepEditWeb.Models;
using SleepEditWeb.Services;

namespace SleepEditWeb.Components.SleepNote;

public partial class SleepNoteForm : ComponentBase
{
    [Inject] private ISleepNoteService SleepNoteService { get; set; } = null!;
    [Inject] private IJSRuntime JsRuntime { get; set; } = null!;
    [Inject] private ILogger<SleepNoteForm> Logger { get; set; } = null!;

    private SleepNoteConfiguration _config = new();
    private string _narrativeText = string.Empty;
    private bool _isGenerating;
    private string _statusMessage = string.Empty;

    // Form state
    private StudyType _studyType = StudyType.Polysomnogram;
    private readonly List<TherapyStageState> _therapyStages = [];

    // Checkbox groups
    private readonly HashSet<string> _bodyPositions = [];
    private readonly HashSet<string> _snoringLevels = [];
    private readonly HashSet<string> _events = [];
    private readonly HashSet<string> _arrhythmias = [];
    private readonly HashSet<string> _effects = [];
    private readonly HashSet<string> _miscOptions = [];

    private static readonly IReadOnlyList<PapTherapyMode> StartingTherapyModes =
    [
        PapTherapyMode.Cpap,
        PapTherapyMode.Bipap,
        PapTherapyMode.BipapSt
    ];

    private static readonly IReadOnlyList<int> BipapIpapValues =
        Enumerable.Range(8, 23).ToArray();

    private static readonly IReadOnlyList<int> BipapEpapValues =
        Enumerable.Range(4, 23).ToArray();

    private static readonly IReadOnlyList<string> TransitionReasons =
    [
        "Persistent obstructive events",
        "Persistent central apneas",
        "CPAP intolerance",
        "Other"
    ];

    private static readonly IReadOnlyList<string> MaskTransitionReasons =
    [
        "Oral leak or mouth opening",
        "Persistent oral leak despite chin strap",
        "Excessive mask leak",
        "Poor mask fit",
        "Patient discomfort or intolerance",
        "Skin irritation or pressure point",
        "Claustrophobia",
        "Other"
    ];

    // Mask setup course
    private readonly List<MaskSetupStageState> _maskStages = [];

    // Config management
    private bool _showMaskManager;
    private bool _focusMaskManager;
    private bool _restoreMaskManagerFocus;
    private ElementReference _maskManagerTrigger;
    private ElementReference _maskManagerInitialFocus;
    private string _newMaskType = string.Empty;
    private string _newMaskSize = string.Empty;

    // Computed properties for conditional visibility
    private bool ShowTitrationControls =>
        _studyType is StudyType.CpapBipapTitration or StudyType.SplitNight;

    private string ArrhythmiaSelectionSummary =>
        _arrhythmias.Count == 0
            ? "Select arrhythmias"
            : $"{_arrhythmias.Count} selected";

    private string ArrhythmiaNarrativePreview =>
        SleepNoteNarrativeGenerator.GenerateArrhythmiaSentence(_arrhythmias).Trim();

    protected override void OnInitialized()
    {
        LoadConfiguration();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_focusMaskManager)
        {
            _focusMaskManager = false;
            await _maskManagerInitialFocus.FocusAsync();
            return;
        }

        if (_restoreMaskManagerFocus)
        {
            _restoreMaskManagerFocus = false;
            await _maskManagerTrigger.FocusAsync();
        }
    }

    private void LoadConfiguration()
    {
        try
        {
            _config = SleepNoteService.GetConfiguration();
            EnsureMaskStageSelections();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load sleep note configuration");
            _statusMessage = "Error loading configuration.";
        }
    }

    private void OnStudyTypeChanged(StudyType studyType)
    {
        _studyType = studyType;
        if (!ShowTitrationControls)
        {
            _therapyStages.Clear();
            _showMaskManager = false;
            _focusMaskManager = false;
            _restoreMaskManagerFocus = false;
        }
        else if (_therapyStages.Count == 0)
        {
            _therapyStages.Add(CreateTherapyStageState(PapTherapyMode.Cpap));
        }

        if (!ShowTitrationControls)
        {
            _maskStages.Clear();
        }
        else if (_maskStages.Count == 0)
        {
            _maskStages.Add(CreateMaskSetupStageState());
        }
    }

    private void OnTherapyModeChanged(int stageIndex, PapTherapyMode mode)
    {
        var current = _therapyStages[stageIndex];
        if (current.Mode == mode)
            return;

        var replacement = CreateTherapyStageState(mode);
        replacement.TransitionReason = current.TransitionReason;
        replacement.OtherTransitionReason = current.OtherTransitionReason;
        _therapyStages[stageIndex] = replacement;

        if (_therapyStages.Count > stageIndex + 1)
            _therapyStages.RemoveRange(stageIndex + 1, _therapyStages.Count - stageIndex - 1);
    }

    private void SetTransitionAfter(int stageIndex, bool enabled)
    {
        if (!enabled)
        {
            if (_therapyStages.Count > stageIndex + 1)
                _therapyStages.RemoveRange(stageIndex + 1, _therapyStages.Count - stageIndex - 1);

            return;
        }

        if (_therapyStages.Count > stageIndex + 1)
            return;

        var nextModes = GetAllowedNextModes(_therapyStages[stageIndex].Mode);
        if (nextModes.Count > 0)
            _therapyStages.Add(CreateTherapyStageState(nextModes[0]));
    }

    private void SetMaskTransitionAfter(int stageIndex, bool enabled)
    {
        if (!enabled)
        {
            if (_maskStages.Count > stageIndex + 1)
                _maskStages.RemoveRange(stageIndex + 1, _maskStages.Count - stageIndex - 1);

            return;
        }

        if (_maskStages.Count > stageIndex + 1)
            return;

        var current = _maskStages[stageIndex];
        _maskStages.Add(new MaskSetupStageState
        {
            MaskType = current.MaskType,
            MaskSize = current.MaskSize,
            ChinStrap = current.ChinStrap
        });
    }

    private IReadOnlyList<PapTherapyMode> GetAvailableModesForStage(int stageIndex) =>
        stageIndex == 0
            ? StartingTherapyModes
            : GetAllowedNextModes(_therapyStages[stageIndex - 1].Mode);

    private static IReadOnlyList<PapTherapyMode> GetAllowedNextModes(PapTherapyMode mode) =>
        mode switch
        {
            PapTherapyMode.Cpap => [PapTherapyMode.Bipap],
            PapTherapyMode.Bipap => [PapTherapyMode.BipapSt],
            _ => []
        };

    private static string GetTherapyModeLabel(PapTherapyMode mode) =>
        mode switch
        {
            PapTherapyMode.Cpap => "CPAP",
            PapTherapyMode.Bipap => "BIPAP",
            PapTherapyMode.BipapSt => "BIPAP ST",
            _ => mode.ToString()
        };

    private static bool HasLowPressureSupport(TherapyStageState stage) =>
        stage.InitialIpap - stage.InitialEpap < 4 ||
        stage.FinalIpap - stage.FinalEpap < 4;

    private static string GetLowPressureSupportMessage(TherapyStageState stage)
    {
        var initialIsLow = stage.InitialIpap - stage.InitialEpap < 4;
        var finalIsLow = stage.FinalIpap - stage.FinalEpap < 4;

        return (initialIsLow, finalIsLow) switch
        {
            (true, true) => "The initial and final IPAP/EPAP differences are below 4 cm H2O.",
            (true, false) => "The initial IPAP/EPAP difference is below 4 cm H2O.",
            (false, true) => "The final IPAP/EPAP difference is below 4 cm H2O.",
            _ => string.Empty
        };
    }

    private static TherapyStageState CreateTherapyStageState(PapTherapyMode mode) =>
        new()
        {
            Mode = mode,
            InitialCpap = 4,
            FinalCpap = 4,
            InitialIpap = 8,
            InitialEpap = 4,
            FinalIpap = 8,
            FinalEpap = 4,
            BackupRate = 10
        };

    private MaskSetupStageState CreateMaskSetupStageState() =>
        new()
        {
            MaskType = _config.MaskTypes.FirstOrDefault() ?? string.Empty,
            MaskSize = _config.MaskSizes.FirstOrDefault() ?? string.Empty
        };

    private void EnsureMaskStageSelections()
    {
        foreach (var stage in _maskStages)
        {
            if (!_config.MaskTypes.Contains(stage.MaskType, StringComparer.OrdinalIgnoreCase))
                stage.MaskType = _config.MaskTypes.FirstOrDefault() ?? string.Empty;

            if (!_config.MaskSizes.Contains(stage.MaskSize, StringComparer.OrdinalIgnoreCase))
                stage.MaskSize = _config.MaskSizes.FirstOrDefault() ?? string.Empty;
        }
    }

    private static void ToggleSetItem(HashSet<string> set, string item, bool isChecked)
    {
        if (isChecked)
            set.Add(item);
        else
            set.Remove(item);
    }

    private async Task GenerateNote()
    {
        _isGenerating = true;
        _statusMessage = string.Empty;

        try
        {
            var formData = BuildFormData();
            var result = SleepNoteService.GenerateNote(formData);
            _narrativeText = result.NarrativeText;
            _statusMessage = $"Note generated at {result.GeneratedUtc:HH:mm:ss} UTC";
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to generate sleep note");
            _statusMessage = "Error generating note.";
        }
        finally
        {
            _isGenerating = false;
        }
    }

    private SleepNoteFormData BuildFormData() =>
        new()
        {
            StudyType = _studyType,
            TherapyCourse = ShowTitrationControls
                ? _therapyStages.Select((stage, index) => new PapTherapyStage
                {
                    Mode = stage.Mode,
                    Pressures = new PressureSettings
                    {
                        InitialCpap = stage.Mode == PapTherapyMode.Cpap ? stage.InitialCpap : null,
                        FinalCpap = stage.Mode == PapTherapyMode.Cpap ? stage.FinalCpap : null,
                        InitialIpap = stage.Mode != PapTherapyMode.Cpap ? stage.InitialIpap : null,
                        InitialEpap = stage.Mode != PapTherapyMode.Cpap ? stage.InitialEpap : null,
                        FinalIpap = stage.Mode != PapTherapyMode.Cpap ? stage.FinalIpap : null,
                        FinalEpap = stage.Mode != PapTherapyMode.Cpap ? stage.FinalEpap : null
                    },
                    BackupRate = stage.Mode == PapTherapyMode.BipapSt ? stage.BackupRate : null,
                    TransitionReason = index == 0 ? null : ResolveTransitionReason(stage)
                }).ToList()
                : [],
            BodyPositions = new HashSet<string>(_bodyPositions),
            SnoringLevels = new HashSet<string>(_snoringLevels),
            Events = new HashSet<string>(_events),
            Arrhythmias = new HashSet<string>(_arrhythmias),
            Effects = new HashSet<string>(_effects),
            MiscOptions = new HashSet<string>(_miscOptions),
            MaskCourse = ShowTitrationControls
                ? _maskStages.Select((stage, index) => new MaskSetupStage
                {
                    MaskType = stage.MaskType,
                    MaskSize = stage.MaskSize,
                    ChinStrap = stage.ChinStrap,
                    TransitionReason = index == 0 ? null : ResolveMaskTransitionReason(stage)
                }).ToList()
                : []
        };

    private static string? ResolveTransitionReason(TherapyStageState stage)
    {
        if (stage.TransitionReason == "Other")
            return string.IsNullOrWhiteSpace(stage.OtherTransitionReason)
                ? null
                : stage.OtherTransitionReason.Trim();

        return string.IsNullOrWhiteSpace(stage.TransitionReason)
            ? null
            : stage.TransitionReason;
    }

    private static string? ResolveMaskTransitionReason(MaskSetupStageState stage)
    {
        if (stage.TransitionReason == "Other")
            return string.IsNullOrWhiteSpace(stage.OtherTransitionReason)
                ? null
                : stage.OtherTransitionReason.Trim();

        return string.IsNullOrWhiteSpace(stage.TransitionReason)
            ? null
            : stage.TransitionReason;
    }

    private async Task CopyToClipboard()
    {
        if (string.IsNullOrEmpty(_narrativeText)) return;

        try
        {
            await JsRuntime.InvokeVoidAsync("navigator.clipboard.writeText", _narrativeText);
            _statusMessage = "Copied to clipboard!";
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to copy to clipboard");
            _statusMessage = "Copy failed — please select and copy manually.";
        }
    }

    private async Task InsertIntoEditor()
    {
        if (string.IsNullOrEmpty(_narrativeText)) return;

        try
        {
            await JsRuntime.InvokeVoidAsync("eval",
                $"window.parent.postMessage({{ type: 'sleepNote:done', content: {System.Text.Json.JsonSerializer.Serialize(_narrativeText)} }}, window.location.origin)");
            _statusMessage = "Inserted into editor!";
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to send narrative to editor");
            _statusMessage = "Insert failed — try copying manually.";
        }
    }

    private async Task CancelSleepNote()
    {
        try
        {
            await JsRuntime.InvokeVoidAsync("eval",
                "window.parent.postMessage({ type: 'sleepNote:cancel' }, window.location.origin)");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to send cancel message");
        }
    }

    private void ResetForm()
    {
        _studyType = StudyType.Polysomnogram;
        _therapyStages.Clear();
        _maskStages.Clear();
        _bodyPositions.Clear();
        _snoringLevels.Clear();
        _events.Clear();
        _arrhythmias.Clear();
        _effects.Clear();
        _miscOptions.Clear();
        _showMaskManager = false;
        _focusMaskManager = false;
        _restoreMaskManagerFocus = false;
        _newMaskType = string.Empty;
        _newMaskSize = string.Empty;
        _narrativeText = string.Empty;
        _statusMessage = "Form reset.";
    }

    private void OpenMaskManager()
    {
        _newMaskType = string.Empty;
        _newMaskSize = string.Empty;
        _showMaskManager = true;
        _focusMaskManager = true;
        _restoreMaskManagerFocus = false;
    }

    private void CloseMaskManager()
    {
        _showMaskManager = false;
        _focusMaskManager = false;
        _restoreMaskManagerFocus = true;
        _newMaskType = string.Empty;
        _newMaskSize = string.Empty;
    }

    private void HandleMaskManagerKeyDown(KeyboardEventArgs args)
    {
        if (args.Key == "Escape")
            CloseMaskManager();
    }

    private void AddMaskType()
    {
        if (string.IsNullOrWhiteSpace(_newMaskType)) return;
        SleepNoteService.AddMaskType(_newMaskType);
        _newMaskType = string.Empty;
        LoadConfiguration();
    }

    private async Task RemoveMaskType(string maskType)
    {
        var confirmed = await JsRuntime.InvokeAsync<bool>(
            "confirm",
            $"Remove '{maskType}' from every mask type dropdown?");
        if (!confirmed) return;

        SleepNoteService.RemoveMaskType(maskType);
        LoadConfiguration();
    }

    private void AddMaskSize()
    {
        if (string.IsNullOrWhiteSpace(_newMaskSize)) return;
        SleepNoteService.AddMaskSize(_newMaskSize);
        _newMaskSize = string.Empty;
        LoadConfiguration();
    }

    private async Task RemoveMaskSize(string maskSize)
    {
        var confirmed = await JsRuntime.InvokeAsync<bool>(
            "confirm",
            $"Remove '{maskSize}' from every mask size dropdown?");
        if (!confirmed) return;

        SleepNoteService.RemoveMaskSize(maskSize);
        LoadConfiguration();
    }

    private async Task ResetConfigToDefaults()
    {
        var confirmed = await JsRuntime.InvokeAsync<bool>(
            "confirm",
            "Reset all mask types and sizes to their defaults?");
        if (!confirmed) return;

        SleepNoteService.ResetConfigToDefaults();
        LoadConfiguration();
        _statusMessage = "Configuration reset to defaults.";
    }

    private sealed class TherapyStageState
    {
        public PapTherapyMode Mode { get; set; }
        public int InitialCpap { get; set; }
        public int FinalCpap { get; set; }
        public int InitialIpap { get; set; }
        public int InitialEpap { get; set; }
        public int FinalIpap { get; set; }
        public int FinalEpap { get; set; }
        public int BackupRate { get; set; }
        public string TransitionReason { get; set; } = string.Empty;
        public string OtherTransitionReason { get; set; } = string.Empty;
    }

    private sealed class MaskSetupStageState
    {
        public string MaskType { get; set; } = string.Empty;
        public string MaskSize { get; set; } = string.Empty;
        public bool ChinStrap { get; set; }
        public string TransitionReason { get; set; } = string.Empty;
        public string OtherTransitionReason { get; set; } = string.Empty;
    }
}
