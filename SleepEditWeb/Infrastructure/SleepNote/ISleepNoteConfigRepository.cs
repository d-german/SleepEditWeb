using SleepEditWeb.Models;

namespace SleepEditWeb.Infrastructure.SleepNote;

public interface ISleepNoteConfigRepository
{
    SleepNoteConfiguration GetConfiguration();
    void SaveConfiguration(SleepNoteConfiguration config);
    void AddMaskType(string maskType);
    void RemoveMaskType(string maskType);
    void AddMaskSize(string maskSize);
    void RemoveMaskSize(string maskSize);
    void ResetToDefaults();
}
