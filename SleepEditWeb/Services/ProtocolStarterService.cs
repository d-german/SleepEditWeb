using SleepEditWeb.Infrastructure.ProtocolPersistence;
using SleepEditWeb.Models;

namespace SleepEditWeb.Services;

public interface IProtocolStarterService
{
    ProtocolDocument Create();
    ProtocolDocument Create(Guid protocolId);
    ProtocolDocument CreateSeedDocument();
}

public sealed class ProtocolStarterService : IProtocolStarterService
{
    private readonly IProtocolRepository _repository;
    private readonly ILogger<ProtocolStarterService> _logger;

    public ProtocolStarterService(
        IProtocolRepository repository,
        ILogger<ProtocolStarterService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public ProtocolDocument Create()
    {
        _logger.LogInformation("Protocol starter create requested.");

        var current = TryLoadFromRepository();
        if (current != null)
        {
            _logger.LogInformation("Protocol starter loaded document from database.");
            return current;
        }

        return CreateSeedDocument();
    }

    public ProtocolDocument Create(Guid protocolId)
    {
        _logger.LogInformation("Protocol starter create requested for protocol {ProtocolId}.", protocolId);

        try
        {
            var version = _repository.GetProtocol(protocolId);
            if (version != null)
            {
                _logger.LogInformation("Protocol starter loaded document for protocol {ProtocolId}.", protocolId);
                return version.Document;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load protocol {ProtocolId} from repository. Falling back to seed.", protocolId);
        }

        _logger.LogWarning("Protocol {ProtocolId} not found. Returning seed document.", protocolId);
        return CreateSeedDocument();
    }

    public ProtocolDocument CreateSeedDocument()
    {
        var nextId = 1;
        var sections = BuildSections(ref nextId);
        WireReferenceLinks(sections);
        _logger.LogInformation("Protocol starter created seed document. SectionCount: {SectionCount}", sections.Count);

        return new ProtocolDocument
        {
            Id = 0,
            LinkId = -1,
            LinkText = string.Empty,
            Text = "Saint Luke's Protocol",
            Sections = sections
        };
    }

    private ProtocolDocument? TryLoadFromRepository()
    {
        try
        {
            var version = _repository.GetDefaultProtocol();
            return version?.Document;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load protocol from repository. Falling back to seed.");
            return null;
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
