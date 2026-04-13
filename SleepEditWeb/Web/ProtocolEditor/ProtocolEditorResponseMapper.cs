using SleepEditWeb.Models;

namespace SleepEditWeb.Web.ProtocolEditor;

public interface IProtocolEditorResponseMapper
{
    object ToStateResponse(ProtocolEditorSnapshot snapshot);
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
}
