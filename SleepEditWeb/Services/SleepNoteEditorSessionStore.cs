using System.Text.Json;
using Microsoft.AspNetCore.Http;
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

    public SleepNoteEditorSessionStore(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public SleepNoteEditorSnapshot Load()
    {
        var session = GetSession();
        if (session == null)
        {
            return CreateDefaultSnapshot();
        }

        var serialized = session.GetString(SnapshotKey);
        return Deserialize(serialized) ?? CreateDefaultSnapshot();
    }

    public void Save(SleepNoteEditorSnapshot snapshot)
    {
        var session = GetSession();
        if (session == null)
        {
            return;
        }

        var serialized = JsonSerializer.Serialize(snapshot);
        session.SetString(SnapshotKey, serialized);
    }

    public void SaveDocument(string content)
    {
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
