using System.Xml.Linq;
using SleepEditWeb.Models;

namespace SleepEditWeb.Infrastructure.ProtocolXml;

public interface IProtocolXmlDeserializer
{
    ProtocolDocument Deserialize(string xml);
}

public sealed class ProtocolXmlDeserializer : IProtocolXmlDeserializer
{
    private readonly IProtocolXmlMapper _mapper;

    public ProtocolXmlDeserializer(IProtocolXmlMapper mapper)
    {
        _mapper = mapper;
    }

    public ProtocolDocument Deserialize(string xml)
    {
        if (string.IsNullOrWhiteSpace(xml))
        {
            throw new ArgumentException("XML content is required.", nameof(xml));
        }

        var document = XDocument.Parse(xml);
        var root = document.Root;
        if (root == null || !root.Name.LocalName.Equals(ProtocolXmlMapper.ProtocolElementName, StringComparison.Ordinal))
        {
            throw new FormatException("XML root element must be Protocol.");
        }

        return _mapper.FromElement(root);
    }
}
