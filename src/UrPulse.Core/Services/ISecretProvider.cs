using UrPulse.Core.Models;

namespace UrPulse.Core.Services
{
    public interface ISecretProvider
    {
        Task<AlertSettings?> GetAlertSettingsAsync(string appId);

        Task<bool> SaveAlertSettingsAsync(string appId, AlertSettings settings);
    }
}
