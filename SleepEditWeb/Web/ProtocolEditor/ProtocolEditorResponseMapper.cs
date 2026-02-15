using SleepEditWeb.Models;

namespace SleepEditWeb.Web.ProtocolEditor;

public interface IProtocolEditorResponseMapper
{
    object ToStateResponse(ProtocolEditorSnapshot snapshot);

    object ToSavedPathResponse(ProtocolEditorSnapshot snapshot, string savedPath);

    object ToDefaultPathResponse(ProtocolEditorSnapshot snapshot, string defaultPath);

    object ToLoadedPathResponse(ProtocolEditorSnapshot snapshot, string loadedPath);
}

public sealed class ProtocolEditorResponseMapper : IProtocolEditorResponseMapper
{
    public object ToStateResponse(ProtocolEditorSnapshot snapshot)
    {
        return new
        {
            document = snapshot.Document,
            undoCount = snapshot.UndoHistory.Count,
            redoCount = snapshot.RedoHistory.Count,
            lastUpdatedUtc = snapshot.LastUpdatedUtc
        };
    }

    public object ToSavedPathResponse(ProtocolEditorSnapshot snapshot, string savedPath)
    {
        return new
        {
            document = snapshot.Document,
            undoCount = snapshot.UndoHistory.Count,
            redoCount = snapshot.RedoHistory.Count,
            lastUpdatedUtc = snapshot.LastUpdatedUtc,
            savedPath
        };
    }

    public object ToDefaultPathResponse(ProtocolEditorSnapshot snapshot, string defaultPath)
    {
        return new
        {
            document = snapshot.Document,
            undoCount = snapshot.UndoHistory.Count,
            redoCount = snapshot.RedoHistory.Count,
            lastUpdatedUtc = snapshot.LastUpdatedUtc,
            defaultPath
        };
    }

    public object ToLoadedPathResponse(ProtocolEditorSnapshot snapshot, string loadedPath)
    {
        return new
        {
            document = snapshot.Document,
            undoCount = snapshot.UndoHistory.Count,
            redoCount = snapshot.RedoHistory.Count,
            lastUpdatedUtc = snapshot.LastUpdatedUtc,
            loadedPath
        };
    }
}
