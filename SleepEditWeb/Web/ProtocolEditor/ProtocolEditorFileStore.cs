using System.Text;
using Microsoft.AspNetCore.Http;

namespace SleepEditWeb.Web.ProtocolEditor;

public interface IProtocolEditorFileStore
{
    bool Exists(string path);

    string ReadAllText(string path);

    void WriteAllText(string path, string xmlContent);

    Task<string> ReadUploadedXmlAsync(IFormFile file);
}

public sealed class ProtocolEditorFileStore : IProtocolEditorFileStore
{
    public bool Exists(string path)
    {
        return File.Exists(path);
    }

    public string ReadAllText(string path)
    {
        return File.ReadAllText(path, Encoding.UTF8);
    }

    public void WriteAllText(string path, string xmlContent)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(path, xmlContent, Encoding.UTF8);
    }

    public async Task<string> ReadUploadedXmlAsync(IFormFile file)
    {
        using var stream = file.OpenReadStream();
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        return await reader.ReadToEndAsync();
    }
}
