using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using UrPulse.Core.Data;
using UrPulse.Core.Entities;
using UrPulse.Core.Models;

namespace UrPulse.Core.Services;

public class PulseEngine : IDisposable
{
    private readonly ConcurrentDictionary<string, HeartbeatPulse> _activePulses = new();
    private readonly Timer _monitorTimer;
    private readonly TimeSpan _offlineThreshold = TimeSpan.FromSeconds(15);
    private readonly HttpClient _http;
    private readonly ISecretProvider _secretProvider;
    private readonly IServiceScopeFactory _scopeFactory;

    public PulseEngine(ISecretProvider secretProvider, IServiceScopeFactory scopeFactory)
    {
        _http = new HttpClient();
        _secretProvider = secretProvider;
        _scopeFactory = scopeFactory;
        _monitorTimer = new Timer(CheckAppHealth, null, TimeSpan.Zero, TimeSpan.FromSeconds(5));
    }

    public void ProcessPulse(HeartbeatPulse pulse)
    {
        var now = DateTime.UtcNow;
        pulse.Timestamp = now;
        pulse.Status = "Healthy";

        // تحديث أو إضافة النبضة في الذاكرة المؤقتة
        _activePulses.AddOrUpdate(pulse.AppId, pulse, (key, old) =>
        {
            old.Timestamp = now;
            old.Status = "Healthy";
            old.Metadata = pulse.Metadata;
            return old;
        });

        // 🟢 توثيق النبضة الصالحة في السجل التاريخي لقاعدة البيانات (في الخلفية دون حصر الأداء)
        _ = Task.Run(() => SaveHealthLogAsync(pulse.AppId, pulse.ServiceName, "Healthy", pulse.Metadata));
    }

    private void CheckAppHealth(object? state)
    {
        var now = DateTime.UtcNow;
        foreach (var item in _activePulses)
        {
            var pulse = item.Value;
            // 1. كشف الانقطاع الأولي (بعد 15 ثانية صمت)
            if (pulse.Status != "Offline" && (now - pulse.Timestamp) > _offlineThreshold)
            {
                pulse.Status = "Offline";
                pulse.OfflineSince = now;
                pulse.EscalationTriggered = false;

                Console.WriteLine($"\n[Ur Pulse] 🚨 OFFLINE: '{pulse.AppId}' went silent.");

                // توثيق في قاعدة البيانات
                _ = Task.Run(() => SaveHealthLogAsync(pulse.AppId, pulse.ServiceName, "Offline", pulse.Metadata));
            }

            // 2. شرط التصعيد الحرج (مرور 60 ثانية دون عودة النبض، ولم يتم التصعيد بعد)
            if (pulse.Status == "Offline" && !pulse.EscalationTriggered && pulse.OfflineSince.HasValue)
            {
                // هنا نقرأ الـ Threshold ديناميكياً من الخزنة المحاكية (مثلاً 60 ثانية)
                _ = Task.Run(async () =>
                {
                    var secureAlerts = await _secretProvider.GetAlertSettingsAsync(pulse.AppId);
                    int threshold = secureAlerts?.EscalationThresholdSeconds ?? 60;

                    if ((now - pulse.OfflineSince.Value).TotalSeconds >= threshold)
                    {
                        pulse.EscalationTriggered = true; // نرفع العلم لمنع تكرار التنبيه

                        // استدعاء الدالة التصعيد 
                        TriggerCriticalEscalation(pulse);
                    }
                });
            }
        }
    }

    private async Task SaveHealthLogAsync(string appId, string serviceName, string status, Dictionary<string, string> metadata)
    {
        try
        {
            // إنشاء Scope معزول لاستدعاء الـ DbContext بشكل Thread-safe
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<UrPulseDbContext>();

            var logEntry = new HealthLog
            {
                Id = Guid.NewGuid(),
                AppId = appId,
                ServiceName = serviceName,
                Status = status,
                Timestamp = DateTime.UtcNow,
                HardwareMetricsJson = JsonSerializer.Serialize(metadata) // تحويل الديكشنري لنص JSON مرن
            };

            dbContext.HealthLogs.Add(logEntry);
            await dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"💾 [Database Error] Failed to write health log: {ex.Message}");
        }
    }


    private void TriggerCriticalEscalation(HeartbeatPulse pulse)
    {
        _ = Task.Run(async () =>
        {
            Console.WriteLine($"\n🔥 [CRITICAL ESCALATION] !!! CRISIS ALERT !!!");

            // سحب أسرار التطبيق من محاكي الخزنة (JSON)
            var secureAlerts = await _secretProvider.GetAlertSettingsAsync(pulse.AppId);
            if (secureAlerts == null || !secureAlerts.EnableAlerts) return;

            // 1. الصوت المحلي
            if (secureAlerts.EnableLoudAudioAlert)
            {
                for (int i = 0; i < 3; i++) { Console.Beep(1500, 500); Thread.Sleep(100); }
            }

            // 2. إرسال تليغرام
            if (secureAlerts.EnableTelegramAlert && !string.IsNullOrEmpty(secureAlerts.TelegramBotToken))
            {
                string message = $"🚨 *CRITICAL ALERT | Ur Pulse* 🚨\n\n" +
                                 $"🔥 *Project:* `{pulse.AppId}`\n" +
                                 $"⚙️ *Service:* `{pulse.ServiceName}`\n" +
                                 $"⚠️ *Status:* `OFFLINE` 🔴";
                _ = SendTelegramMessageAsync(secureAlerts.TelegramBotToken, secureAlerts.TelegramChatId, message);
            }

            // 3. اتصال تويليو بالإنجليزية مع الرسالة المخصصة
            if (secureAlerts.EnableVoiceCallAlert && !string.IsNullOrEmpty(secureAlerts.TwilioAccountSid))
            {
                string voiceMessage = !string.IsNullOrWhiteSpace(secureAlerts.CustomVoiceMessage)
                    ? secureAlerts.CustomVoiceMessage
                    : $"Urgent alert from Ur Pulse! Your project {pulse.AppId} is offline.";

                _ = MakeVoiceCallAsync(secureAlerts, voiceMessage);
            }
        });
    }

    private async Task SendTelegramMessageAsync(string token, string chatId, string message)
    {
        try
        {
            string url = $"https://api.telegram.org/bot{token}/sendMessage";
            var payload = new
            {
                chat_id = chatId,
                text = message,
                parse_mode = "Markdown"
            };

            var response = await _http.PostAsJsonAsync(url, payload);
            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("🌐 [Telegram Bot] Critical notification sent successfully to user's phone.");
            }
            else
            {
                Console.WriteLine($"🌐 [Telegram Bot] ⚠️ Failed to send message. Status: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"🌐 [Telegram Bot] ❌ Error calling Telegram API: {ex.Message}");
        }
    }

    private async Task MakeVoiceCallAsync(AlertSettings alerts, string message)
    {
        try
        {
            string url = $"https://api.twilio.com/2010-04-01/Accounts/{alerts.TwilioAccountSid}/Calls.json";
            string twiml = $"<Response><Say language=\"ar-XA\" voice=\"Polly.Zeina\">{message}</Say></Response>";

            var requestData = new Dictionary<string, string>
            {
                { "To", alerts.TargetPhoneNumber },
                { "From", alerts.TwilioFromNumber },
                { "Twiml", twiml }
            };

            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new FormUrlEncodedContent(requestData)
            };

            string credentials = Convert.ToBase64String(
                System.Text.Encoding.ASCII.GetBytes($"{alerts.TwilioAccountSid}:{alerts.TwilioAuthToken}"));
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);

            var response = await _http.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("📞 [Twilio VoIP] Critical Escalation: Initiating phone call to user right now!");
            }
            else
            {
                string errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"📞 [Twilio VoIP] ⚠️ Failed to call. Status: {response.StatusCode}, Details: {errorContent}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"📞 [Twilio VoIP] ❌ Error calling Twilio API: {ex.Message}");
        }
    }

    public IEnumerable<HeartbeatPulse> GetAllStatuses() => _activePulses.Values;

    public void Dispose()
    {
        _monitorTimer.Dispose();
        _http.Dispose();
    }
}
