using UrPulse.Core.Models;

namespace UrPulse.Core.Services;

public interface IUrPulseSettingsProvider
{
    Task<UrPulseSettings> GetSettingsAsync();
    Task<bool> SaveSettingsAsync(UrPulseSettings settings);
}
