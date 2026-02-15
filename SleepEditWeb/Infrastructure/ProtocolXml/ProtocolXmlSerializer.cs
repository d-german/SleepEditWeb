using System.Xml.Linq;
using SleepEditWeb.Models;

namespace SleepEditWeb.Infrastructure.ProtocolXml;

public interface IProtocolXmlSerializer
{
    string Serialize(ProtocolDocument document);
}

public sealed class ProtocolXmlSerializer : IProtocolXmlSerializer
{
    private readonly IProtocolXmlMapper _mapper;

    public ProtocolXmlSerializer(IProtocolXmlMapper mapper)
    {
        _mapper = mapper;
    }

    public string Serialize(ProtocolDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var root = _mapper.ToElement(document);
        var xmlDocument = new XDocument(new XDeclaration("1.0", "utf-8", null), root);
        return xmlDocument.ToString();
    }
}
