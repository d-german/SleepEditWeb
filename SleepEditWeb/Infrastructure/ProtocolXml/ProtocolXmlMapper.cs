using System.Xml.Linq;
using SleepEditWeb.Models;

namespace SleepEditWeb.Infrastructure.ProtocolXml;

public interface IProtocolXmlMapper
{
    XElement ToElement(ProtocolDocument document);

    ProtocolDocument FromElement(XElement root);
}

public sealed class ProtocolXmlMapper : IProtocolXmlMapper
{
    public const string ProtocolElementName = "Protocol";
    public const string SectionElementName = "Section";
    public const string SubSectionElementName = "SubSection";
    public const string IdElementName = "Id";
    public const string LinkIdElementName = "LinkId";
    public const string LinkTextElementName = "LinkText";
    public const string TextElementName = "text";
    public const string SubTextElementName = "SubText";

    public XElement ToElement(ProtocolDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var root = new XElement(ProtocolElementName);
        AddFields(root, document.Id, document.LinkId, document.LinkText, document.Text, []);

        foreach (var section in document.Sections)
        {
            root.Add(WriteNode(section, isSection: true));
        }

        return root;
    }

    public ProtocolDocument FromElement(XElement root)
    {
        ArgumentNullException.ThrowIfNull(root);

        var sections = root
            .Elements(SectionElementName)
            .Select(section => ReadNode(section, ProtocolNodeKind.Section))
            .ToList();

        return new ProtocolDocument
        {
            Id = ReadInt(root, IdElementName),
            LinkId = ReadInt(root, LinkIdElementName),
            LinkText = ReadString(root, LinkTextElementName),
            Text = ReadString(root, TextElementName),
            Sections = sections
        };
    }

    private static XElement WriteNode(ProtocolNodeModel node, bool isSection)
    {
        var elementName = isSection ? SectionElementName : SubSectionElementName;
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
        element.Add(new XElement(IdElementName, id));
        element.Add(new XElement(LinkIdElementName, linkId));
        element.Add(new XElement(LinkTextElementName, linkText ?? string.Empty));
        element.Add(new XElement(TextElementName, text ?? string.Empty));

        foreach (var item in subText.Where(value => !string.IsNullOrWhiteSpace(value)))
        {
            element.Add(new XElement(SubTextElementName, item));
        }
    }

    private static ProtocolNodeModel ReadNode(XElement element, ProtocolNodeKind kind)
    {
        var children = element
            .Elements(SubSectionElementName)
            .Select(child => ReadNode(child, ProtocolNodeKind.SubSection))
            .ToList();

        var subText = element
            .Elements(SubTextElementName)
            .Select(item => item.Value)
            .ToList();

        return new ProtocolNodeModel
        {
            Id = ReadInt(element, IdElementName),
            LinkId = ReadInt(element, LinkIdElementName),
            LinkText = ReadString(element, LinkTextElementName),
            Text = ReadString(element, TextElementName),
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
