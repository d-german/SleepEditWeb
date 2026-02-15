using Microsoft.Extensions.Logging;
using SleepEditWeb.Infrastructure.ProtocolXml;
using SleepEditWeb.Models;

namespace SleepEditWeb.Services;

public interface IProtocolXmlService
{
    string Serialize(ProtocolDocument document);

    ProtocolDocument Deserialize(string xml);
}

public sealed class ProtocolXmlService : IProtocolXmlService
{
    private readonly IProtocolXmlSerializer _serializer;
    private readonly IProtocolXmlDeserializer _deserializer;
    private readonly ILogger<ProtocolXmlService> _logger;

    public ProtocolXmlService(ILogger<ProtocolXmlService> logger)
        : this(
            new ProtocolXmlSerializer(new ProtocolXmlMapper()),
            new ProtocolXmlDeserializer(new ProtocolXmlMapper()),
            logger)
    {
    }

    public ProtocolXmlService(
        IProtocolXmlSerializer serializer,
        IProtocolXmlDeserializer deserializer,
        ILogger<ProtocolXmlService> logger)
    {
        _serializer = serializer;
        _deserializer = deserializer;
        _logger = logger;
    }

    public string Serialize(ProtocolDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        _logger.LogDebug("ProtocolXmlService.Serialize requested. SectionCount: {SectionCount}", document.Sections.Count);

        var xml = _serializer.Serialize(document);
        _logger.LogDebug("ProtocolXmlService.Serialize completed. XmlLength: {Length}", xml.Length);
        return xml;
    }

    public ProtocolDocument Deserialize(string xml)
    {
        if (string.IsNullOrWhiteSpace(xml))
        {
            _logger.LogWarning("ProtocolXmlService.Deserialize rejected empty XML.");
            throw new ArgumentException("XML content is required.", nameof(xml));
        }

        _logger.LogDebug("ProtocolXmlService.Deserialize requested. XmlLength: {Length}", xml.Length);

        try
        {
            var result = _deserializer.Deserialize(xml);
            _logger.LogDebug("ProtocolXmlService.Deserialize completed. SectionCount: {SectionCount}", result.Sections.Count);
            return result;
        }
        catch (FormatException)
        {
            _logger.LogWarning("ProtocolXmlService.Deserialize failed because root element was invalid.");
            throw;
        }
    }
}
