using System.Globalization;

namespace SleepEditWeb.Models;

public sealed class ProtocolViewerViewModel
{
    public ProtocolDocument InitialDocument { get; init; } = new();

    public IReadOnlyList<string> InitialTechNames { get; init; } = [];

    public IReadOnlyList<string> InitialMaskStyles { get; init; } = [];

    public IReadOnlyList<string> InitialMaskSizes { get; init; } = [];

    public string InitialStudyDate { get; init; } = DateTime.UtcNow.ToString("M/d/yyyy", CultureInfo.InvariantCulture);
}
