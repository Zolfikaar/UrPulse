using System.Collections.Concurrent;
using System.Data;
using System.Net.Http.Json;
using System.Threading.Tasks;
using UrPulse.Core.Models;

namespace UrPulse.Core.Services;

public class PulseEngine : IDisposable
{
    private readonly ConcurrentDictionary<string, HeartbeatPulse> _activePulses = new();
    private readonly Timer _monitorTimer;
    private readonly TimeSpan _offlineThreshold = TimeSpan.FromSeconds(15);
    private readonly HttpClient _http;

    public PulseEngine()
    {
        _http = new HttpClient();
        // الفحص مستمر كل 5 ثوانٍ
        _monitorTimer = new Timer(CheckAppHealth, null, TimeSpan.Zero, TimeSpan.FromSeconds(5));
    }

    public void ProcessPulse(HeartbeatPulse pulse)
    {
        string key = $"{pulse.AppId}:{pulse.ServiceName}".ToLower();
        pulse.Timestamp = DateTime.UtcNow;

        if (_activePulses.TryGetValue(key, out var existingPulse))
        {
            if (existingPulse.Status == "Offline")
            {
                Console.WriteLine($"\n[Ur Pulse] 🟢 App '{pulse.AppId}' ({pulse.ServiceName}) is BACK ONLINE. Escalation cleared.");
            }
            // نسخ حالة التصعيد القديمة لو عاد التطبيق قبل التصعيد
            pulse.OfflineSince = null;
            pulse.EscalationTriggered = false;
        }

        _activePulses[key] = pulse;
    }

    private void CheckAppHealth(object? state)
    {
        var now = DateTime.UtcNow;

        foreach (var item in _activePulses)
        {
            var pulse = item.Value;

            // 1. المرحلة الأولى: رصد الانقطاع الأولي (Offline)
            if (pulse.Status != "Offline" && (now - pulse.Timestamp) > _offlineThreshold)
            {
                pulse.Status = "Offline";
                pulse.OfflineSince = now; // تسجيل بداية وقت الموت
                pulse.EscalationTriggered = false;

                Console.WriteLine($"\n[Ur Pulse] 🚨 OFFLINE DETECTED: '{pulse.AppId}' ({pulse.ServiceName}) went silent.");
            }

            // 2. المرحلة الثانية: منطق التصعيد الحاد (Escalation) بعد دقيقة
            if (pulse.Status == "Offline" && pulse.OfflineSince.HasValue && !pulse.EscalationTriggered)
            {
                var timeSpentOffline = now - pulse.OfflineSince.Value;

                if (pulse.Alerts.EnableAlerts && timeSpentOffline.TotalSeconds >= pulse.Alerts.EscalationThresholdSeconds)
                {
                    pulse.EscalationTriggered = true; // علامة لمنع تكرار الإنذار

                    TriggerCriticalEscalation(pulse);
                }
            }
        }
    }

    // إطلاق الإجراءات الحادة والتنبيهات المزعجة
    private async Task TriggerCriticalEscalation(HeartbeatPulse pulse)
    {
        Console.WriteLine("\n=======================================================");
        Console.WriteLine($"🔥 [CRITICAL ESCALATION] !!! CRISIS ALERT !!!");
        Console.WriteLine($"🔥 App '{pulse.AppId}' ({pulse.ServiceName}) has been OFFLINE for over {pulse.Alerts.EscalationThresholdSeconds} seconds!");

        if (pulse.Alerts.EnableLoudAudioAlert)
        {
            // إطلاق صوت إنذار من النظام عبر السيرفر !
            Task.Run(() => {
                for (int i = 0; i < 5; i++)
                {
                    Console.Beep(1500, 600); // تردد مزعج (1500Hz) لمدة تزيد عن نصف ثانية
                    Thread.Sleep(200);
                }
            });
            Console.WriteLine("🔊 [LOUD AUDIO] Playing fire-alarm beep style notification on target device...");
        }

        if (!string.IsNullOrEmpty(pulse.Alerts.WebhookUrl))
        {
            Console.WriteLine($"🌐 [Webhook] Sending urgent payload to external network: {pulse.Alerts.WebhookUrl}");
        }

        //  إرسال التنبيه الفوري إلى تليغرام المستخدم!
        if (pulse.Alerts.EnableTelegramAlert && !string.IsNullOrEmpty(pulse.Alerts.TelegramBotToken) && !string.IsNullOrEmpty(pulse.Alerts.TelegramChatId))
        {
            // صياغة رسالة إنذار دراماتيكية مدعومة بالإيموجي لتلفت الانتباه فوراً
            string message = $"🚨 *CRITICAL ALERT | Ur Pulse* 🚨\n\n" +
                             $"🔥 *Project:* `{pulse.AppId}`\n" +
                             $"⚙️ *Service:* `{pulse.ServiceName}`\n" +
                             $"⚠️ *Status:* `OFFLINE` 🔴\n" +
                             $"⏱️ *Downtime:* Over {pulse.Alerts.EscalationThresholdSeconds} seconds!\n" +
                             $"📅 *Time:* {DateTime.UtcNow.ToLocalTime()}\n\n" +
                             $"📢 _Action Required: Please check the system infrastructure immediately!_";
            // استدعاء دالة الإرسال في الخلفية لضمان عدم حصر السيرفر
            _= Task.Run(() => SendTelegramMessageAsync(pulse.Alerts.TelegramBotToken, pulse.Alerts.TelegramChatId, message));

        }

        // 3. إطلاق لفل البريميوم: الاتصال بهاتف المستخدم عبر Twilio
        if (pulse.Alerts.EnableVoiceCallAlert &&
            !string.IsNullOrEmpty(pulse.Alerts.TwilioAccountSid) &&
            !string.IsNullOrEmpty(pulse.Alerts.TwilioAuthToken))
        {
            // صياغة الرسالة الصوتية التي سينطقها الروبوت للعميل عند فتح الخط
            string voiceMessage = $"Urgent alert from Ur Pulse system! Your project {pulse.AppId}, service {pulse.ServiceName}, is offline. Please review your core infrastructure immediately.";

            // إطلاق المكالمة في الخلفية دون حصر أداء السيرفر
            _ = Task.Run(() => MakeVoiceCallAsync(pulse.Alerts, voiceMessage, pulse));
        }
        Console.WriteLine("=======================================================");
    }

    // الدالة المسؤولة عن مخاطبة Telegram API برمجياً
    private async Task SendTelegramMessageAsync(string token, string chatId, string message)
    {
        try
        {
            string url = $"https://api.telegram.org/bot{token}/sendMessage";
            var payload = new
            {
                chat_id = chatId,
                text = message,
                parse_mode = "Markdown" // لتنسيق النصوص وجعل الخط عريضاً ومثيراً للاهتمام
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

    private async Task MakeVoiceCallAsync(AlertSettings alerts, string message, HeartbeatPulse pulse)
    {
        try
        {
            string url = $"https://api.twilio.com/2010-04-01/Accounts/{alerts.TwilioAccountSid}/Calls.json";

            // إذا وضع المستخدم رسالة مخصصة نستخدمها، وإلا نستخدم الرسالة الافتراضية بالإنجليزية
            string voiceMessage = !string.IsNullOrWhiteSpace(pulse.Alerts.CustomVoiceMessage)
                ? pulse.Alerts.CustomVoiceMessage
                : $"Urgent alert from Ur Pulse! Your project {pulse.AppId}, service {pulse.ServiceName}, is offline.";
            // استخدام لغة TwiML (مستند XML بسيط يفهمه سيرفر تويليو ليقرأ النص)
            string twiml = $"<Response><Say language=\"ar-XA\" voice=\"Polly.Zeina\">{voiceMessage}</Say></Response>";
            // إعداد بيانات الطلب (Form URL Encoded) كما يتوقعها بروتوكول تويليو
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

            // تشفير الـ Account SID والـ Auth Token باستخدام Basic Authentication
            string credentials = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{alerts.TwilioAccountSid}:{alerts.TwilioAuthToken}"));
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

    public IEnumerable<HeartbeatPulse> GetAllStatuses()
    {
        return _activePulses.Values;
    }

    public void Dispose()
    {
        _monitorTimer.Dispose();
        _http.Dispose();
    }
}