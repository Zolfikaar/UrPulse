using System.Text.Json;
using Microsoft.Extensions.Configuration;
using UrPulse.Core.Models;

namespace UrPulse.Core.Services;

public class LocalJsonSecretProvider : ISecretProvider
{
    private readonly IConfiguration _configuration;
    private readonly string _configFilePath;

    public LocalJsonSecretProvider(IConfiguration configuration)
    {
        _configuration = configuration;
        // تحديد مسار ملف الإعدادات الحقيقي على القرص الصلب
        _configFilePath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");

        // إذا لم يكن الملف في BaseDirectory (بيئة التطوير)، نبحث عنه في مجلد المشروع الرئيسي
        if (!File.Exists(_configFilePath))
        {
            _configFilePath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");
        }
    }

    public Task<AlertSettings?> GetAlertSettingsAsync(string appId)
    {
        var section = _configuration.GetSection($"UrVaultSimulation:Apps:{appId}");
        if (!section.Exists())
        {
            return Task.FromResult<AlertSettings?>(null);
        }

        var settings = section.Get<AlertSettings>();
        return Task.FromResult(settings);
    }

    public async Task<bool> SaveAlertSettingsAsync(string appId, AlertSettings settings)
    {
        try
        {
            // 1. قراءة الملف بالكامل كنص
            if (!File.Exists(_configFilePath)) return false;
            var jsonString = await File.ReadAllTextAsync(_configFilePath);

            // 2. تحويله إلى JsonDocument للتلاعب به مرئياً
            using var jsonDoc = JsonDocument.Parse(jsonString);
            var root = jsonDoc.RootElement;

            // تحويل الـ JsonDocument إلى Dictionary لسهولة التعديل
            var rootDict = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonString) ?? new();

            // 3. الوصول إلى هيكلية UrVaultSimulation -> Apps
            if (!rootDict.ContainsKey("UrVaultSimulation"))
            {
                rootDict["UrVaultSimulation"] = new Dictionary<string, object>();
            }

            var vaultSim = JsonSerializer.Deserialize<Dictionary<string, object>>(rootDict["UrVaultSimulation"].ToString()!) ?? new();

            if (!vaultSim.ContainsKey("Apps"))
            {
                vaultSim["Apps"] = new Dictionary<string, AlertSettings>();
            }

            var apps = JsonSerializer.Deserialize<Dictionary<string, AlertSettings>>(vaultSim["Apps"].ToString()!) ?? new();

            // 4. تحديث أو إضافة إعدادات التطبيق المحدد
            apps[appId] = settings;

            // 5. إعادة بناء الهيكل وتعبئته صعوداً
            vaultSim["Apps"] = apps;
            rootDict["UrVaultSimulation"] = vaultSim;

            // 6. حفظ الملف بشكل منسق وجميل (Indented) على القرص
            var options = new JsonSerializerOptions { WriteIndented = true };
            var updatedJson = JsonSerializer.Serialize(rootDict, options);
            await File.WriteAllTextAsync(_configFilePath, updatedJson);

            // إجبار الـ Configuration الحية في السيرفر على إعادة القراءة من الملف فوراً
            if (_configuration is IConfigurationRoot rootConfig)
            {
                rootConfig.Reload();
            }

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"💾 [Vault Security Error] Failed to write secret settings: {ex.Message}");
            return false;
        }
    }
}