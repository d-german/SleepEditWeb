using System.Xml;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SleepEditWeb.Models;

namespace SleepEditWeb.Services;

public interface IProtocolStarterService
{
    ProtocolDocument Create();
}

public sealed class ProtocolStarterService : IProtocolStarterService
{
    private readonly IProtocolXmlService _xmlService;
    private readonly ProtocolEditorStartupOptions _startupOptions;
    private readonly ILogger<ProtocolStarterService> _logger;

    public ProtocolStarterService(
        IProtocolXmlService xmlService,
        IOptions<ProtocolEditorStartupOptions> startupOptions,
        ILogger<ProtocolStarterService> logger)
    {
        _xmlService = xmlService;
        _startupOptions = startupOptions.Value;
        _logger = logger;
    }

    public ProtocolDocument Create()
    {
        var configuredDocument = TryCreateFromConfiguredProtocolFile();
        if (configuredDocument != null)
        {
            return configuredDocument;
        }

        var nextId = 1;
        var sections = BuildSections(ref nextId);
        WireReferenceLinks(sections);

        return new ProtocolDocument
        {
            Id = 0,
            LinkId = -1,
            LinkText = string.Empty,
            Text = "Saint Luke's Protocol",
            Sections = sections
        };
    }

    private ProtocolDocument? TryCreateFromConfiguredProtocolFile()
    {
        foreach (var protocolPath in GetStartupCandidatePaths())
        {
            if (!File.Exists(protocolPath))
            {
                _logger.LogWarning("Protocol startup file not found at configured path: {Path}", protocolPath);
                continue;
            }

            try
            {
                var xml = File.ReadAllText(protocolPath);
                return _xmlService.Deserialize(xml);
            }
            catch (Exception ex) when (
                ex is IOException or
                UnauthorizedAccessException or
                XmlException or
                FormatException or
                ArgumentException)
            {
                _logger.LogWarning(ex, "Failed to load protocol startup file from: {Path}", protocolPath);
            }
        }

        return null;
    }

    private IEnumerable<string> GetStartupCandidatePaths()
    {
        if (!string.IsNullOrWhiteSpace(_startupOptions.DefaultProtocolPath))
        {
            yield return _startupOptions.DefaultProtocolPath;
        }

        if (!string.IsNullOrWhiteSpace(_startupOptions.StartupProtocolPath) &&
            !_startupOptions.StartupProtocolPath.Equals(_startupOptions.DefaultProtocolPath, StringComparison.OrdinalIgnoreCase))
        {
            yield return _startupOptions.StartupProtocolPath;
        }
    }

    private static List<ProtocolNodeModel> BuildSections(ref int nextId)
    {
        var diagnostic = CreateNode(ref nextId, ProtocolNodeKind.Section, "Diagnostic Polysomnogram:", [
            CreateNode(ref nextId, ProtocolNodeKind.SubSection, "Monitor SpO2 and EKG for Emergency Guideline Interventions", [
                CreateNode(ref nextId, ProtocolNodeKind.SubSection, "SpO2 drops below 50%-GOTO BiPAP Titration", [
                    CreateNode(ref nextId, ProtocolNodeKind.SubSection, "PaCO2 > 52"),
                    CreateNode(ref nextId, ProtocolNodeKind.SubSection, "PaCO2 < 52"),
                    CreateNode(ref nextId, ProtocolNodeKind.SubSection, "Recurrent SpO2 desaturation to 70% or less for any 15 minute period", subText: ["edit"])
                ])
            ]),
            CreateNode(ref nextId, ProtocolNodeKind.SubSection, "Monitor SpO2 for baseline changes below 86% (document 30 minutes)"),
            CreateNode(ref nextId, ProtocolNodeKind.SubSection, "PaO2 < 55"),
            CreateNode(ref nextId, ProtocolNodeKind.SubSection, "GOAL: Supine/REM sleep obtained"),
            CreateNode(ref nextId, ProtocolNodeKind.SubSection, "GOAL: All goals complete")
        ]);

        return
        [
            diagnostic,
            CreateSectionWithDefaultItem(ref nextId, "Split Night Polysomnogram:"),
            CreateSectionWithDefaultItem(ref nextId, "CPAP Titration Polysomnogram:"),
            CreateSectionWithDefaultItem(ref nextId, "BiPAP Titration Polysomnogram:"),
            CreateSectionWithDefaultItem(ref nextId, "Supplemental Oxygen:"),
            CreateSectionWithDefaultItem(ref nextId, "Respiratory Event Determination:"),
            CreateSectionWithDefaultItem(ref nextId, "Post-Op Polysomnogram:"),
            CreateSectionWithDefaultItem(ref nextId, "Treatment Intolerance:"),
            CreateSectionWithDefaultItem(ref nextId, "Oral Appliance Protocol:"),
            CreateSectionWithDefaultItem(ref nextId, "Ventilator:"),
            CreateSectionWithDefaultItem(ref nextId, "CPAP/BIPAP Failure:"),
            CreateSectionWithDefaultItem(ref nextId, "End of Study:")
        ];
    }

    private static ProtocolNodeModel CreateSectionWithDefaultItem(ref int nextId, string sectionName)
    {
        return CreateNode(ref nextId, ProtocolNodeKind.Section, sectionName, [
            CreateNode(ref nextId, ProtocolNodeKind.SubSection, "Review criteria and document findings.")
        ]);
    }

    private static ProtocolNodeModel CreateNode(
        ref int nextId,
        ProtocolNodeKind kind,
        string text,
        List<ProtocolNodeModel>? children = null,
        List<string>? subText = null)
    {
        return new ProtocolNodeModel
        {
            Id = nextId++,
            LinkId = -1,
            LinkText = string.Empty,
            Text = text,
            Kind = kind,
            Children = children ?? [],
            SubText = subText ?? []
        };
    }

    private static void WireReferenceLinks(List<ProtocolNodeModel> sections)
    {
        var diagnostic = sections.FirstOrDefault(item => item.Text.Equals("Diagnostic Polysomnogram:", StringComparison.Ordinal));
        var biPap = sections.FirstOrDefault(item => item.Text.Equals("BiPAP Titration Polysomnogram:", StringComparison.Ordinal));

        if (diagnostic == null || biPap == null)
        {
            return;
        }

        var trigger = FindByText(diagnostic.Children, "SpO2 drops below 50%-GOTO BiPAP Titration");
        if (trigger == null)
        {
            return;
        }

        trigger.LinkId = biPap.Id;
        trigger.LinkText = biPap.Text;
    }

    private static ProtocolNodeModel? FindByText(IEnumerable<ProtocolNodeModel> nodes, string text)
    {
        foreach (var node in nodes)
        {
            if (node.Text.Equals(text, StringComparison.Ordinal))
            {
                return node;
            }

            var found = FindByText(node.Children, text);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }
}
