using Microsoft.AspNetCore.Http;

namespace SleepEditWeb.Web.ProtocolEditor;

public interface IProtocolEditorRequestValidator
{
    bool IsPayloadMissing<TRequest>(TRequest? request)
        where TRequest : class;

    string? ValidateResolvedPath(string resolvedPath, string missingPathMessage);

    string? ValidateUploadedFile(IFormFile? file, long maxImportXmlBytes);
}

public sealed class ProtocolEditorRequestValidator : IProtocolEditorRequestValidator
{
    public bool IsPayloadMissing<TRequest>(TRequest? request)
        where TRequest : class
    {
        return request == null;
    }

    public string? ValidateResolvedPath(string resolvedPath, string missingPathMessage)
    {
        return string.IsNullOrWhiteSpace(resolvedPath) ? missingPathMessage : null;
    }

    public string? ValidateUploadedFile(IFormFile? file, long maxImportXmlBytes)
    {
        if (file == null || file.Length == 0)
        {
            return "No XML file was uploaded.";
        }

        if (file.Length > maxImportXmlBytes)
        {
            return "Uploaded XML file is too large.";
        }

        return null;
    }
}
