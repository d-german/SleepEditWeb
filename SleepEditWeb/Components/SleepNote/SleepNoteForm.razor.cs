using Microsoft.AspNetCore.Components;
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
    private TitrationMode _titrationMode = TitrationMode.None;

    // Checkbox groups
    private readonly HashSet<string> _bodyPositions = [];
    private readonly HashSet<string> _snoringLevels = [];
    private readonly HashSet<string> _events = [];
    private readonly HashSet<string> _effects = [];
    private readonly HashSet<string> _miscOptions = [];

    // Pressure settings
    private int _initialCpap = 4;
    private int _finalCpap = 4;
    private int _initialIpap = 4;
    private int _initialEpap = 4;
    private int _finalIpap = 4;
    private int _finalEpap = 4;

    // Treatment accessories
    private string _maskType = string.Empty;
    private string _maskSize = string.Empty;
    private bool _chinStrap;
    private bool _heatedHumidifier;

    // Patient machine
    private bool _patientHasMachine;
    private int _pressureVerifiedAt = 4;
    private int _pressureChangedTo = 4;

    // Config management
    private string _newMaskType = string.Empty;
    private string _newMaskSize = string.Empty;

    // Computed properties for conditional visibility
    private bool ShowTitrationControls =>
        _studyType is StudyType.CpapBipapTitration or StudyType.SplitNight;

    private bool ShowCpapPanel =>
        ShowTitrationControls && _titrationMode == TitrationMode.Cpap;

    private bool ShowBipapPanel =>
        ShowTitrationControls && _titrationMode == TitrationMode.Bipap;

    protected override void OnInitialized()
    {
        LoadConfiguration();
    }

    private void LoadConfiguration()
    {
        try
        {
            _config = SleepNoteService.GetConfiguration();
            if (_config.MaskTypes.Count > 0)
                _maskType = _config.MaskTypes[0];
            if (_config.MaskSizes.Count > 0)
                _maskSize = _config.MaskSizes[0];
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
            _titrationMode = TitrationMode.None;
        else if (_titrationMode == TitrationMode.None)
            _titrationMode = TitrationMode.Cpap;
    }

    private void OnTitrationModeChanged(TitrationMode mode)
    {
        _titrationMode = mode;
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
            TitrationMode = _titrationMode,
            BodyPositions = new HashSet<string>(_bodyPositions),
            SnoringLevels = new HashSet<string>(_snoringLevels),
            Events = new HashSet<string>(_events),
            Effects = new HashSet<string>(_effects),
            MiscOptions = new HashSet<string>(_miscOptions),
            Pressures = new PressureSettings
            {
                InitialCpap = _initialCpap,
                FinalCpap = _finalCpap,
                InitialIpap = _initialIpap,
                InitialEpap = _initialEpap,
                FinalIpap = _finalIpap,
                FinalEpap = _finalEpap
            },
            MaskType = _maskType,
            MaskSize = _maskSize,
            ChinStrap = _chinStrap,
            HeatedHumidifier = _heatedHumidifier,
            PatientHasMachine = _patientHasMachine,
            PressureVerifiedAt = _pressureVerifiedAt,
            PressureChangedTo = _pressureChangedTo
        };

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
        _titrationMode = TitrationMode.None;
        _bodyPositions.Clear();
        _snoringLevels.Clear();
        _events.Clear();
        _effects.Clear();
        _miscOptions.Clear();
        _initialCpap = _finalCpap = 4;
        _initialIpap = _initialEpap = _finalIpap = _finalEpap = 4;
        _maskType = _config.MaskTypes.Count > 0 ? _config.MaskTypes[0] : string.Empty;
        _maskSize = _config.MaskSizes.Count > 0 ? _config.MaskSizes[0] : string.Empty;
        _chinStrap = false;
        _heatedHumidifier = false;
        _patientHasMachine = false;
        _pressureVerifiedAt = _pressureChangedTo = 4;
        _narrativeText = string.Empty;
        _statusMessage = "Form reset.";
    }

    private void AddMaskType()
    {
        if (string.IsNullOrWhiteSpace(_newMaskType)) return;
        SleepNoteService.AddMaskType(_newMaskType);
        _newMaskType = string.Empty;
        LoadConfiguration();
    }

    private void RemoveMaskType(string maskType)
    {
        SleepNoteService.RemoveMaskType(maskType);
        LoadConfiguration();
        if (_maskType == maskType && _config.MaskTypes.Count > 0)
            _maskType = _config.MaskTypes[0];
    }

    private void AddMaskSize()
    {
        if (string.IsNullOrWhiteSpace(_newMaskSize)) return;
        SleepNoteService.AddMaskSize(_newMaskSize);
        _newMaskSize = string.Empty;
        LoadConfiguration();
    }

    private void RemoveMaskSize(string maskSize)
    {
        SleepNoteService.RemoveMaskSize(maskSize);
        LoadConfiguration();
        if (_maskSize == maskSize && _config.MaskSizes.Count > 0)
            _maskSize = _config.MaskSizes[0];
    }
}
