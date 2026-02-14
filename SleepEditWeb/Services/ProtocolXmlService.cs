using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using SleepEditWeb.Models;

namespace SleepEditWeb.Services;

public interface IProtocolXmlService
{
    string Serialize(ProtocolDocument document);

    ProtocolDocument Deserialize(string xml);
}

public sealed class ProtocolXmlService : IProtocolXmlService
{
    private const string ProtocolElement = "Protocol";
    private const string SectionElement = "Section";
    private const string SubSectionElement = "SubSection";
    private const string IdElement = "Id";
    private const string LinkIdElement = "LinkId";
    private const string LinkTextElement = "LinkText";
    private const string TextElement = "text";
    private const string SubTextElement = "SubText";
    private readonly ILogger<ProtocolXmlService> _logger;

    public ProtocolXmlService(ILogger<ProtocolXmlService> logger)
    {
        _logger = logger;
    }

    public string Serialize(ProtocolDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        _logger.LogDebug("ProtocolXmlService.Serialize requested. SectionCount: {SectionCount}", document.Sections.Count);

        var root = new XElement(ProtocolElement);
        AddFields(root, document.Id, document.LinkId, document.LinkText, document.Text, []);

        foreach (var section in document.Sections)
        {
            root.Add(WriteNode(section, isSection: true));
        }

        var xmlDocument = new XDocument(new XDeclaration("1.0", "utf-8", null), root);
        var xml = xmlDocument.ToString();
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
        var document = XDocument.Parse(xml);
        var root = document.Root;

        if (root == null || !root.Name.LocalName.Equals(ProtocolElement, StringComparison.Ordinal))
        {
            _logger.LogWarning("ProtocolXmlService.Deserialize failed because root element was invalid.");
            throw new FormatException("XML root element must be Protocol.");
        }

        var sections = root
            .Elements(SectionElement)
            .Select(section => ReadNode(section, ProtocolNodeKind.Section))
            .ToList();

        var result = new ProtocolDocument
        {
            Id = ReadInt(root, IdElement),
            LinkId = ReadInt(root, LinkIdElement),
            LinkText = ReadString(root, LinkTextElement),
            Text = ReadString(root, TextElement),
            Sections = sections
        };

        _logger.LogDebug("ProtocolXmlService.Deserialize completed. SectionCount: {SectionCount}", result.Sections.Count);
        return result;
    }

    private static XElement WriteNode(ProtocolNodeModel node, bool isSection)
    {
        var elementName = isSection ? SectionElement : SubSectionElement;
        var element = new XElement(elementName);
        AddFields(element, node.Id, node.LinkId, node.LinkText, node.Text, node.SubText);

        foreach (var child in node.Children)
        {
            element.Add(WriteNode(child, isSection: false));
        }

        return element;
    }

    private static void AddFields(
        XElement element,
        int id,
        int linkId,
        string linkText,
        string text,
        IReadOnlyList<string> subText)
    {
        element.Add(new XElement(IdElement, id));
        element.Add(new XElement(LinkIdElement, linkId));
        element.Add(new XElement(LinkTextElement, linkText ?? string.Empty));
        element.Add(new XElement(TextElement, text ?? string.Empty));

        foreach (var item in subText.Where(value => !string.IsNullOrWhiteSpace(value)))
        {
            element.Add(new XElement(SubTextElement, item));
        }
    }

    private static ProtocolNodeModel ReadNode(XElement element, ProtocolNodeKind kind)
    {
        var children = element
            .Elements(SubSectionElement)
            .Select(child => ReadNode(child, ProtocolNodeKind.SubSection))
            .ToList();

        var subText = element
            .Elements(SubTextElement)
            .Select(item => item.Value)
            .ToList();

        return new ProtocolNodeModel
        {
            Id = ReadInt(element, IdElement),
            LinkId = ReadInt(element, LinkIdElement),
            LinkText = ReadString(element, LinkTextElement),
            Text = ReadString(element, TextElement),
            Kind = kind,
            SubText = subText,
            Children = children
        };
    }

    private static int ReadInt(XElement element, string name)
    {
        var value = ReadString(element, name);
        return int.TryParse(value, out var parsed) ? parsed : -1;
    }

    private static string ReadString(XElement element, string name)
    {
        return element.Element(name)?.Value ?? string.Empty;
    }
}
