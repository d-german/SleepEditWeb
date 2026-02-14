using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using SleepEditWeb.Models;

namespace SleepEditWeb.Services;

public interface ISleepNoteEditorSessionStore
{
    SleepNoteEditorSnapshot Load();

    void Save(SleepNoteEditorSnapshot snapshot);

    void SaveDocument(string content);
}

public sealed class SleepNoteEditorSessionStore : ISleepNoteEditorSessionStore
{
    private const string SnapshotKey = "SleepNoteEditor.Snapshot";

    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<SleepNoteEditorSessionStore> _logger;

    public SleepNoteEditorSessionStore(
        IHttpContextAccessor httpContextAccessor,
        ILogger<SleepNoteEditorSessionStore> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public SleepNoteEditorSnapshot Load()
    {
        var session = GetSession();
        if (session == null)
        {
            _logger.LogWarning("SleepNoteEditorSessionStore.Load returned default snapshot because session was unavailable.");
            return CreateDefaultSnapshot();
        }

        var serialized = session.GetString(SnapshotKey);
        var snapshot = Deserialize(serialized);
        if (snapshot != null)
        {
            _logger.LogDebug("SleepNoteEditorSessionStore.Load returned snapshot from session.");
            return snapshot;
        }

        _logger.LogInformation("SleepNoteEditorSessionStore.Load returned default snapshot because session state was empty or invalid.");
        return CreateDefaultSnapshot();
    }

    public void Save(SleepNoteEditorSnapshot snapshot)
    {
        var session = GetSession();
        if (session == null)
        {
            _logger.LogWarning("SleepNoteEditorSessionStore.Save skipped because session was unavailable.");
            return;
        }

        var serialized = JsonSerializer.Serialize(snapshot);
        session.SetString(SnapshotKey, serialized);
        _logger.LogDebug("SleepNoteEditorSessionStore.Save persisted snapshot. SelectedCount: {Count}", snapshot.SelectedMedications.Count);
    }

    public void SaveDocument(string content)
    {
        _logger.LogInformation("SleepNoteEditorSessionStore.SaveDocument requested. ContentLength: {Length}", content?.Length ?? 0);
        var existing = Load();
        Save(new SleepNoteEditorSnapshot
        {
            DocumentContent = content ?? string.Empty,
            LastUpdatedUtc = DateTimeOffset.UtcNow,
            SelectedMedications = existing.SelectedMedications
        });
    }

    private ISession? GetSession()
    {
        return _httpContextAccessor.HttpContext?.Session;
    }

    private static SleepNoteEditorSnapshot? Deserialize(string? serialized)
    {
        if (string.IsNullOrWhiteSpace(serialized))
        {
            return null;
        }

        return JsonSerializer.Deserialize<SleepNoteEditorSnapshot>(serialized);
    }

    private static SleepNoteEditorSnapshot CreateDefaultSnapshot()
    {
        return new SleepNoteEditorSnapshot
        {
            DocumentContent = DefaultTemplate(),
            LastUpdatedUtc = DateTimeOffset.UtcNow,
            SelectedMedications = []
        };
    }

    private static string DefaultTemplate()
    {
        return "Sleep Study Note" + Environment.NewLine +
               "History:" + Environment.NewLine +
               Environment.NewLine +
               "Medications:" + Environment.NewLine +
               "Medications: none documented." + Environment.NewLine +
               Environment.NewLine +
               "Assessment:" + Environment.NewLine;
    }
}
