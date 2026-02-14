using System.Text.Json;
using Microsoft.AspNetCore.Http;
using SleepEditWeb.Models;

namespace SleepEditWeb.Services;

public interface IProtocolEditorSessionStore
{
    ProtocolEditorSnapshot Load();

    void Save(ProtocolEditorSnapshot snapshot);

    void Reset();
}

public sealed class ProtocolEditorSessionStore : IProtocolEditorSessionStore
{
    private const string SnapshotKey = "ProtocolEditor.Snapshot";

    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IProtocolStarterService _starterService;

    public ProtocolEditorSessionStore(
        IHttpContextAccessor httpContextAccessor,
        IProtocolStarterService starterService)
    {
        _httpContextAccessor = httpContextAccessor;
        _starterService = starterService;
    }

    public ProtocolEditorSnapshot Load()
    {
        var session = GetSession();
        if (session == null)
        {
            return CreateDefaultSnapshot();
        }

        var serialized = session.GetString(SnapshotKey);
        return Deserialize(serialized) ?? CreateDefaultSnapshot();
    }

    public void Save(ProtocolEditorSnapshot snapshot)
    {
        var session = GetSession();
        if (session == null)
        {
            return;
        }

        var serialized = JsonSerializer.Serialize(snapshot);
        session.SetString(SnapshotKey, serialized);
    }

    public void Reset()
    {
        Save(CreateDefaultSnapshot());
    }

    private ISession? GetSession()
    {
        return _httpContextAccessor.HttpContext?.Session;
    }

    private ProtocolEditorSnapshot CreateDefaultSnapshot()
    {
        return new ProtocolEditorSnapshot
        {
            Document = _starterService.Create(),
            UndoHistory = [],
            RedoHistory = [],
            LastUpdatedUtc = DateTimeOffset.UtcNow
        };
    }

    private static ProtocolEditorSnapshot? Deserialize(string? serialized)
    {
        if (string.IsNullOrWhiteSpace(serialized))
        {
            return null;
        }

        return JsonSerializer.Deserialize<ProtocolEditorSnapshot>(serialized);
    }
}
