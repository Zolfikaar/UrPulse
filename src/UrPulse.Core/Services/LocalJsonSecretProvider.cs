using Microsoft.Extensions.Configuration;
using UrPulse.Core.Models;

namespace UrPulse.Core.Services
{
    public class LocalJsonSecretProvider : ISecretProvider
    {
        private readonly IConfiguration _configuration;

        public LocalJsonSecretProvider(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public Task<AlertSettings?> GetAlertSettingsAsync(string appId)
        {
            // قراءة القسم الخاص بالتطبيق المحدد من ملف appsettings.json ديناميكياً
            // الهيكلية المتوقعة في ملف الـ JSON ستكون: UrVaultSimulation:Apps:appId
            var section = _configuration.GetSection($"UrVaultSimulation:Apps:{appId}");

            if (!section.Exists())
            {
                return Task.FromResult<AlertSettings?>(null);
            }

            var settings = section.Get<AlertSettings>();
            return Task.FromResult(settings);
        }
    }
}
