using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Configuration;
using UrPulse.Core.Models;

namespace UrPulse.Core.Services;

public class LocalJsonSettingsProvider : IUrPulseSettingsProvider
{
    private const string SectionName = "UrPulseSettings";
    private readonly IConfiguration _configuration;
    private readonly string _configFilePath;

    public LocalJsonSettingsProvider(IConfiguration configuration)
    {
        _configuration = configuration;
        _configFilePath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");

        if (!File.Exists(_configFilePath))
        {
            _configFilePath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");
        }
    }

    public Task<UrPulseSettings> GetSettingsAsync()
    {
        var section = _configuration.GetSection(SectionName);
        var settings = section.Exists()
            ? section.Get<UrPulseSettings>() ?? new UrPulseSettings()
            : new UrPulseSettings();

        settings.Telegram ??= new TelegramSettings();
        settings.Twilio ??= new TwilioSettings();
        return Task.FromResult(settings);
    }

    public async Task<bool> SaveSettingsAsync(UrPulseSettings settings)
    {
        try
        {
            if (!File.Exists(_configFilePath)) return false;

            var jsonString = await File.ReadAllTextAsync(_configFilePath);
            var root = JsonNode.Parse(jsonString)?.AsObject()
                ?? throw new InvalidOperationException("Unable to parse appsettings.json.");

            // Drop legacy per-app vault if present
            root.Remove("UrVaultSimulation");

            root[SectionName] = JsonSerializer.SerializeToNode(settings, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = null,
            });

            var updatedJson = root.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(_configFilePath, updatedJson);

            if (_configuration is IConfigurationRoot rootConfig)
            {
                rootConfig.Reload();
            }

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"💾 [Settings] Failed to persist UrPulseSettings: {ex.Message}");
            return false;
        }
    }
}
