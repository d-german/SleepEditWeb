namespace SleepEditWeb.Web.ProtocolEditor;

public interface IProtocolEditorRequestValidator
{
    bool IsPayloadMissing<TRequest>(TRequest? request)
        where TRequest : class;

    string? ValidateUploadedFile(IFormFile? file, long maxImportXmlBytes);
}

public sealed class ProtocolEditorRequestValidator : IProtocolEditorRequestValidator
{
    public bool IsPayloadMissing<TRequest>(TRequest? request)
        where TRequest : class
    {
        return request == null;
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
