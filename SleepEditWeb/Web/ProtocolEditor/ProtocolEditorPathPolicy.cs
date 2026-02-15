using Microsoft.Extensions.Options;
using SleepEditWeb.Models;

namespace SleepEditWeb.Web.ProtocolEditor;

public interface IProtocolEditorPathPolicy
{
    string ResolveSavePath();

    string ResolveDefaultPath();

    string ResolveImportPath(string? requestedPath);

    string ResolveUploadSavePath(string? uploadedFileName);
}

public sealed class ProtocolEditorPathPolicy : IProtocolEditorPathPolicy
{
    private readonly ProtocolEditorStartupOptions _startupOptions;

    public ProtocolEditorPathPolicy(IOptions<ProtocolEditorStartupOptions> startupOptions)
    {
        _startupOptions = startupOptions.Value;
    }

    public string ResolveSavePath()
    {
        if (!string.IsNullOrWhiteSpace(_startupOptions.SaveProtocolPath))
        {
            return _startupOptions.SaveProtocolPath;
        }

        if (!string.IsNullOrWhiteSpace(_startupOptions.StartupProtocolPath))
        {
            return _startupOptions.StartupProtocolPath;
        }

        if (!string.IsNullOrWhiteSpace(_startupOptions.DefaultProtocolPath))
        {
            return _startupOptions.DefaultProtocolPath;
        }

        return Path.Combine(AppContext.BaseDirectory, "Data", "protocols", "default-protocol.xml");
    }

    public string ResolveDefaultPath()
    {
        if (!string.IsNullOrWhiteSpace(_startupOptions.DefaultProtocolPath))
        {
            return _startupOptions.DefaultProtocolPath;
        }

        if (!string.IsNullOrWhiteSpace(_startupOptions.StartupProtocolPath))
        {
            return _startupOptions.StartupProtocolPath;
        }

        if (!string.IsNullOrWhiteSpace(_startupOptions.SaveProtocolPath))
        {
            return _startupOptions.SaveProtocolPath;
        }

        return Path.Combine(AppContext.BaseDirectory, "Data", "protocols", "default-protocol.xml");
    }

    public string ResolveImportPath(string? requestedPath)
    {
        if (!string.IsNullOrWhiteSpace(requestedPath))
        {
            return requestedPath;
        }

        return ResolveSavePath();
    }

    public string ResolveUploadSavePath(string? uploadedFileName)
    {
        if (!string.IsNullOrWhiteSpace(_startupOptions.SaveProtocolPath))
        {
            return _startupOptions.SaveProtocolPath;
        }

        var safeFileName = string.IsNullOrWhiteSpace(uploadedFileName)
            ? "protocol-upload.xml"
            : Path.GetFileName(uploadedFileName);

        if (!safeFileName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
        {
            safeFileName = $"{safeFileName}.xml";
        }

        var fallbackDirectory = Path.Combine(AppContext.BaseDirectory, "Data", "protocols");
        return Path.Combine(fallbackDirectory, safeFileName);
    }
}
