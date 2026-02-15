using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using SleepEditWeb.Infrastructure.ProtocolPersistence;
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
    private readonly IProtocolRepository _repository;
    private readonly ILogger<ProtocolEditorSessionStore> _logger;

    public ProtocolEditorSessionStore(
        IHttpContextAccessor httpContextAccessor,
        IProtocolStarterService starterService,
        IProtocolRepository repository,
        ILogger<ProtocolEditorSessionStore> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _starterService = starterService;
        _repository = repository;
        _logger = logger;
    }

    public ProtocolEditorSnapshot Load()
    {
        var session = GetSession();
        if (session == null)
        {
            _logger.LogWarning("ProtocolEditorSessionStore.Load returned default snapshot because session was unavailable.");
            return CreateDefaultSnapshot();
        }

        var serialized = session.GetString(SnapshotKey);
        var snapshot = Deserialize(serialized);
        if (snapshot != null)
        {
            _logger.LogDebug("ProtocolEditorSessionStore.Load returned snapshot from session.");
            return snapshot;
        }

        _logger.LogInformation("ProtocolEditorSessionStore.Load returned default snapshot because session state was empty or invalid.");
        return CreateDefaultSnapshot();
    }

    public void Save(ProtocolEditorSnapshot snapshot)
    {
        var session = GetSession();
        if (session == null)
        {
            _logger.LogWarning("ProtocolEditorSessionStore.Save skipped because session was unavailable.");
            return;
        }

        var serialized = JsonSerializer.Serialize(snapshot);
        session.SetString(SnapshotKey, serialized);
        _logger.LogDebug(
            "ProtocolEditorSessionStore.Save persisted snapshot. UndoCount: {UndoCount}, RedoCount: {RedoCount}",
            snapshot.UndoHistory.Count,
            snapshot.RedoHistory.Count);
    }

    public void Reset()
    {
        _logger.LogInformation("ProtocolEditorSessionStore.Reset requested.");
        Save(CreateStarterSnapshot());
    }

    private ISession? GetSession()
    {
        return _httpContextAccessor.HttpContext?.Session;
    }

    private ProtocolEditorSnapshot CreateDefaultSnapshot()
    {
        var repositorySnapshot = TryCreateSnapshotFromRepository();
        if (repositorySnapshot != null)
        {
            return repositorySnapshot;
        }

        return new ProtocolEditorSnapshot
        {
            Document = _starterService.Create(),
            UndoHistory = [],
            RedoHistory = [],
            LastUpdatedUtc = DateTimeOffset.UtcNow
        };
    }

    private ProtocolEditorSnapshot CreateStarterSnapshot()
    {
        return new ProtocolEditorSnapshot
        {
            Document = _starterService.Create(),
            UndoHistory = [],
            RedoHistory = [],
            LastUpdatedUtc = DateTimeOffset.UtcNow
        };
    }

    private ProtocolEditorSnapshot? TryCreateSnapshotFromRepository()
    {
        try
        {
            var latestVersion = _repository.GetLatestVersion();
            if (latestVersion == null)
            {
                return null;
            }

            _logger.LogInformation(
                "ProtocolEditorSessionStore loaded default snapshot from repository version {VersionId}.",
                latestVersion.VersionId);

            var savedUtc = DateTime.SpecifyKind(latestVersion.SavedUtc, DateTimeKind.Utc);
            return new ProtocolEditorSnapshot
            {
                Document = latestVersion.Document,
                UndoHistory = [],
                RedoHistory = [],
                LastUpdatedUtc = new DateTimeOffset(savedUtc)
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Protocol repository unavailable. Falling back to starter snapshot.");
            return null;
        }
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
