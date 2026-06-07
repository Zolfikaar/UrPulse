using System.Diagnostics;
using System.Net.Http.Json;

namespace UrPulse.Client;

public class UrPulseClient : IDisposable
{
    private readonly string _twilioSid;
    private readonly string _twilioToken;
    private readonly string _twilioFrom;
    private readonly string _targetPhone;

    private readonly HttpClient _httpClient;
    private readonly string _serverUrl;
    private readonly string _appId;
    private readonly string _serviceName;
    private readonly TimeSpan _interval;
    private Timer? _timer;
    private readonly Process _currentProcess;

    // حدود العتبة لتقييم حالة التطبيق
    private readonly double _maxMemoryHealthyMb;

    // الإعدادات الخاصة بالتنبيهات
    private readonly string _telegramBotToken;
    private readonly string _telegramChatId;

    public UrPulseClient(
        string serverUrl,
        string appId,
        string serviceName = "General",
        int intervalSeconds = 10,
        double maxMemoryHealthyMb = 100.0,  // الافتراضي: 100 ميجابايت كحد أقصى للحالة السليمة
        string telegramBotToken = "",
        string telegramChatId = "", 
        string twilioSid = "",
        string twilioToken = "",
        string twilioFrom = "",
        string targetPhone = "")
    {
        _httpClient = new HttpClient();
        _serverUrl = serverUrl.TrimEnd('/');
        _appId = appId;
        _serviceName = serviceName;
        _interval = TimeSpan.FromSeconds(intervalSeconds);
        _currentProcess = Process.GetCurrentProcess();
        _maxMemoryHealthyMb = maxMemoryHealthyMb;
        _telegramBotToken = telegramBotToken;
        _telegramChatId = telegramChatId;

        _twilioSid = twilioSid;
        _twilioToken = twilioToken;
        _twilioFrom = twilioFrom;
        _targetPhone = targetPhone;
    }

    public void Start()
    {
        if (_timer != null) return;
        _timer = new Timer(async _ => await SendHeartbeatAsync(), null, TimeSpan.Zero, _interval);
        Console.WriteLine($"[Ur Pulse Client] 🚀 Started monitoring for {_appId}:{_serviceName} (RAM Limit: {_maxMemoryHealthyMb}MB)...");
    }

    // دالة هندسة تقييم الحالة بناءً على الموارد الحالية
    private string EvaluateStatus(double currentMemoryMb)
    {
        // إذا تجاوز استهلاك الذاكرة الحد المسموح، نرسل حالة متدهورة (Degraded) لإنذار السيرفر
        if (currentMemoryMb > _maxMemoryHealthyMb)
        {
            return "Degraded";
        }

        return "Healthy";
    }

    private async Task SendHeartbeatAsync()
    {
        try
        {

            _currentProcess.Refresh();

            // حساب استهلاك الذاكرة الحالي
            double memoryUsedMb = _currentProcess.PrivateMemorySize64 / (1024.0 * 1024.0);
            TimeSpan cpuTime = _currentProcess.TotalProcessorTime;

            // تقييم الحالة ديناميكياً
            string currentStatus = EvaluateStatus(memoryUsedMb);

            var pulse = new
            {
                AppId = _appId,
                ServiceName = _serviceName,
                Status = currentStatus, // الحالة الديناميكية الجديدة
                Metadata = new Dictionary<string, string>
                {
                    { "MachineName", Environment.MachineName },
                    { "OS", Environment.OSVersion.ToString() },
                    { "MemoryUsage_MB", memoryUsedMb.ToString("F2") },
                    { "MemoryLimit_MB", _maxMemoryHealthyMb.ToString("F2") },
                    { "TotalCpuTime_Sec", cpuTime.TotalSeconds.ToString("F2") },
                    { "DotNetVersion", Environment.Version.ToString() }
                },
                Alerts = new
                {
                    EnableAlerts = true,
                    EscalationThresholdSeconds = 60,
                    EnableLoudAudioAlert = true,
                    EnableTelegramAlert = true,
                    TelegramBotToken = _telegramBotToken, // إرسال التوكن الحقيقي
                    TelegramChatId = _telegramChatId,     // إرسال الشات آيدي الحقيقي

                    // تمرير بيانات تويليو (سنضع قيمها حية في الـ Sample للتجربة)
                    EnableVoiceCallAlert = true,
                    TwilioAccountSid = "",
                    TwilioAuthToken = "",
                    TwilioFromNumber = "",
                    TargetPhoneNumber = "",
                    CustomVoiceMessage = "Attention! Vector Kanban authentication service has failed. Please check." // أو تركها فارغة للوضع التلقائي
                },

                TwilioAccountSid = _twilioSid,
                TwilioAuthToken = _twilioToken,
                TwilioFromNumber = _twilioFrom,
                TargetPhoneNumber = _targetPhone
            };

            var response = await _httpClient.PostAsJsonAsync($"{_serverUrl}/api/pulse/heartbeat", pulse);

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"[Ur Pulse Client] ⚠️ Failed to send heartbeat. Status: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Ur Pulse Client] ❌ Error sending heartbeat: {ex.Message}");
        }
    }

    public void Stop()
    {
        _timer?.Change(Timeout.Infinite, 0);
        _timer?.Dispose();
        _timer = null;
    }

    public void Dispose()
    {
        Stop();
        _httpClient.Dispose();
        _currentProcess.Dispose();
    }
}