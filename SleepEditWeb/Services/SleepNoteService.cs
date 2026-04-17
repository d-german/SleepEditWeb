using SleepEditWeb.Infrastructure.SleepNote;
using SleepEditWeb.Models;

namespace SleepEditWeb.Services;

public interface ISleepNoteService
{
    SleepNoteGeneratedResult GenerateNote(SleepNoteFormData formData);
    SleepNoteConfiguration GetConfiguration();
    void AddMaskType(string maskType);
    void RemoveMaskType(string maskType);
    void AddMaskSize(string maskSize);
    void RemoveMaskSize(string maskSize);
    void ResetConfigToDefaults();
}

public sealed class SleepNoteService(
    ISleepNoteConfigRepository configRepository,
    ILogger<SleepNoteService> logger) : ISleepNoteService
{
    public SleepNoteGeneratedResult GenerateNote(SleepNoteFormData formData)
    {
        var narrative = SleepNoteNarrativeGenerator.Generate(formData);
        logger.LogInformation("Generated sleep note narrative ({Length} chars)", narrative.Length);

        return new SleepNoteGeneratedResult(narrative, DateTime.UtcNow);
    }

    public SleepNoteConfiguration GetConfiguration() =>
        configRepository.GetConfiguration();

    public void AddMaskType(string maskType) =>
        configRepository.AddMaskType(maskType);

    public void RemoveMaskType(string maskType) =>
        configRepository.RemoveMaskType(maskType);

    public void AddMaskSize(string maskSize) =>
        configRepository.AddMaskSize(maskSize);

    public void RemoveMaskSize(string maskSize) =>
        configRepository.RemoveMaskSize(maskSize);

    public void ResetConfigToDefaults()
    {
        configRepository.ResetToDefaults();
        logger.LogInformation("Sleep note configuration reset to defaults.");
    }
}
