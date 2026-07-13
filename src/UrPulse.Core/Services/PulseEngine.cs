using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using UrPulse.Core.Data;
using UrPulse.Core.Entities;
using UrPulse.Core.Models;

namespace UrPulse.Core.Services;

public class PulseEngine : IDisposable
{
    private readonly Dictionary<string, HeartbeatPulse> _activePulses = new();
    private readonly object _engineLock = new();
    private readonly Timer _monitorTimer;
    private TimeSpan _offlineThreshold = TimeSpan.FromSeconds(20);
    private TimeSpan _timerInterval = TimeSpan.FromSeconds(5);
    private TimeSpan _escalationDelay = TimeSpan.FromSeconds(30);
    private int _heartbeatIntervalSeconds = 10;
    private readonly HttpClient _http;
    private readonly IUrPulseSettingsProvider _settingsProvider;
    private readonly IServiceScopeFactory _scopeFactory;

    public int GetOfflineThresholdSeconds() => (int)_offlineThreshold.TotalSeconds;
    public int GetTimerIntervalSeconds() => (int)_timerInterval.TotalSeconds;
    public int GetEscalationDelaySeconds() => (int)_escalationDelay.TotalSeconds;
    public int GetHeartbeatIntervalSeconds() => _heartbeatIntervalSeconds;

    public void ApplyRuntimeTuning(
        int heartbeatIntervalSeconds,
        int offlineThresholdSeconds,
        int scanIntervalSeconds,
        int escalationDelaySeconds)
    {
        UpdateHeartbeatIntervalSeconds(heartbeatIntervalSeconds);
        UpdateOfflineThresholdSeconds(offlineThresholdSeconds);
        UpdateTimerIntervalSeconds(scanIntervalSeconds);
        UpdateEscalationDelaySeconds(escalationDelaySeconds);
    }

    public void UpdateHeartbeatIntervalSeconds(int seconds)
    {
        lock (_engineLock)
        {
            _heartbeatIntervalSeconds = Math.Clamp(seconds, 5, 60);
            Console.WriteLine($"⚙️ [System Config] Global Heartbeat Interval updated to: {_heartbeatIntervalSeconds} seconds.");
        }
    }

    public void UpdateOfflineThresholdSeconds(int seconds)
    {
        lock (_engineLock)
        {
            _offlineThreshold = TimeSpan.FromSeconds(Math.Clamp(seconds, 5, 300));
            Console.WriteLine($"⚙️ [System Config] Global Offline Threshold updated to: {(int)_offlineThreshold.TotalSeconds} seconds.");
        }
    }

    public void UpdateTimerIntervalSeconds(int seconds)
    {
        lock (_engineLock)
        {
            _timerInterval = TimeSpan.FromSeconds(Math.Clamp(seconds, 1, 60));
            _monitorTimer.Change(TimeSpan.Zero, _timerInterval);
            Console.WriteLine($"⚙️ [System Config] Monitor scan interval updated to: {(int)_timerInterval.TotalSeconds} seconds.");
        }
    }

    public void UpdateEscalationDelaySeconds(int seconds)
    {
        lock (_engineLock)
        {
            _escalationDelay = TimeSpan.FromSeconds(Math.Clamp(seconds, 0, 600));
            Console.WriteLine($"⚙️ [System Config] Global Escalation Delay updated to: {(int)_escalationDelay.TotalSeconds} seconds.");
        }
    }

    public PulseEngine(IUrPulseSettingsProvider settingsProvider, IServiceScopeFactory scopeFactory)
    {
        _http = new HttpClient();
        _settingsProvider = settingsProvider;
        _scopeFactory = scopeFactory;
        _monitorTimer = new Timer(CheckAppHealth, null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);

        var settings = settingsProvider.GetSettingsAsync().GetAwaiter().GetResult();
        _heartbeatIntervalSeconds = Math.Clamp(settings.GlobalHeartbeatIntervalSeconds, 5, 60);
        _offlineThreshold = TimeSpan.FromSeconds(Math.Clamp(settings.GlobalOfflineThresholdSeconds, 5, 300));
        _timerInterval = TimeSpan.FromSeconds(Math.Clamp(settings.GlobalScanIntervalSeconds, 1, 60));
        _escalationDelay = TimeSpan.FromSeconds(Math.Clamp(settings.GlobalEscalationDelaySeconds, 0, 600));
        _monitorTimer.Change(TimeSpan.Zero, _timerInterval);

        Console.WriteLine(
            $"⚙️ [UrPulseSettings] Loaded — heartbeat {GetHeartbeatIntervalSeconds()}s · " +
            $"offline {GetOfflineThresholdSeconds()}s · scan {GetTimerIntervalSeconds()}s · " +
            $"escalation delay {GetEscalationDelaySeconds()}s.");
    }

    public void ProcessPulse(HeartbeatPulse pulse)
    {
        // Server clock only — never trust client/browser timestamps for liveness.
        var now = DateTime.Now;
        var key = $"{pulse.AppId}:{pulse.ServiceName}".ToLower();

        lock (_engineLock)
        {
            if (_activePulses.TryGetValue(key, out var existing))
            {
                existing.Timestamp = now;
                existing.Status = "Healthy";
                existing.OfflineSince = null;
                existing.EscalationTriggered = false;
                existing.Metadata = pulse.Metadata;
            }
            else
            {
                pulse.Timestamp = now;
                pulse.Status = "Healthy";
                pulse.OfflineSince = null;
                pulse.EscalationTriggered = false;
                _activePulses[key] = pulse;
            }
        }

        _ = Task.Run(() => SaveHealthLogAsync(pulse.AppId, pulse.ServiceName, "Healthy", pulse.Metadata));
    }

    private void CheckAppHealth(object? state)
    {
        var now = DateTime.Now;

        lock (_engineLock)
        {
            foreach (var item in _activePulses)
            {
                var pulse = item.Value;

                // Phase 1: silence → Offline (no alerts yet)
                if (pulse.Status != "Offline" && (now - pulse.Timestamp) > _offlineThreshold)
                {
                    pulse.Status = "Offline";
                    pulse.OfflineSince = now;

                    Console.WriteLine($"\n[Ur Pulse] 🚨 OFFLINE DETECTED: '{pulse.AppId}' ({pulse.ServiceName}) went silent.");

                    _ = Task.Run(() => SaveHealthLogAsync(pulse.AppId, pulse.ServiceName, "Offline", pulse.Metadata));
                }

                // Phase 2: remain Offline for escalation delay → Telegram / Twilio / beep
                if (pulse.Status == "Offline"
                    && !pulse.EscalationTriggered
                    && pulse.OfflineSince.HasValue
                    && (now - pulse.OfflineSince.Value) >= _escalationDelay)
                {
                    pulse.EscalationTriggered = true;
                    Console.WriteLine(
                        $"[Ur Pulse] ⏱️ Escalation delay ({GetEscalationDelaySeconds()}s) elapsed for '{pulse.AppId}'.");
                    TriggerCriticalEscalation(pulse);
                }
            }
        }
    }

    private async Task SaveHealthLogAsync(string appId, string serviceName, string status, Dictionary<string, string> metadata)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<UrPulseDbContext>();

            var logEntry = new HealthLog
            {
                Id = Guid.NewGuid(),
                AppId = appId,
                ServiceName = serviceName,
                Status = status,
                Timestamp = DateTime.UtcNow,
                HardwareMetricsJson = JsonSerializer.Serialize(metadata)
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
            Console.WriteLine($"\n🔥 [CRITICAL ESCALATION] !!! CRISIS ALERT for '{pulse.AppId}' !!!");

            var settings = await _settingsProvider.GetSettingsAsync();
            if (!settings.EnableAlerts) return;

            if (settings.LocalAudioAlerts)
            {
                for (int i = 0; i < 3; i++) { Console.Beep(1500, 500); Thread.Sleep(100); }
            }

            var telegram = settings.Telegram;
            if (!string.IsNullOrWhiteSpace(telegram?.BotToken) && !string.IsNullOrWhiteSpace(telegram.ChatId))
            {
                string message = $"🚨 *CRITICAL ALERT | Ur Pulse* 🚨\n\n" +
                                 $"🔥 *Project:* `{pulse.AppId}`\n" +
                                 $"⚙️ *Service:* `{pulse.ServiceName}`\n" +
                                 $"⚠️ *Status:* `OFFLINE` 🔴";
                _ = SendTelegramMessageAsync(telegram.BotToken, telegram.ChatId, message);
            }

            var twilio = settings.Twilio;
            if (!string.IsNullOrWhiteSpace(twilio?.AccountSid) && !string.IsNullOrWhiteSpace(twilio.AuthToken))
            {
                string voiceMessage = !string.IsNullOrWhiteSpace(twilio.VoiceMessage)
                    ? twilio.VoiceMessage
                    : $"Urgent alert from Ur Pulse! Your project {pulse.AppId} is offline.";

                _ = MakeVoiceCallAsync(twilio, voiceMessage);
            }
        });
    }

    private async Task SendTelegramMessageAsync(string token, string chatId, string message)
    {
        try
        {
            string url = $"https://api.telegram.org/bot{token}/sendMessage";
            var payload = new { chat_id = chatId, text = message, parse_mode = "Markdown" };

            var response = await _http.PostAsJsonAsync(url, payload);
            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("🌐 [Telegram Bot] Critical notification sent successfully.");
            }
            else
            {
                Console.WriteLine($"🌐 [Telegram Bot] ⚠️ Failed. Status: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"🌐 [Telegram Bot] ❌ Error: {ex.Message}");
        }
    }

    private async Task MakeVoiceCallAsync(TwilioSettings twilio, string message)
    {
        try
        {
            string url = $"https://api.twilio.com/2010-04-01/Accounts/{twilio.AccountSid}/Calls.json";
            string twiml = $"<Response><Say language=\"ar-XA\" voice=\"Polly.Zeina\">{message}</Say></Response>";

            var requestData = new Dictionary<string, string>
            {
                { "To", twilio.ToNumber },
                { "From", twilio.FromNumber },
                { "Twiml", twiml }
            };

            var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = new FormUrlEncodedContent(requestData) };
            string credentials = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{twilio.AccountSid}:{twilio.AuthToken}"));
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);

            var response = await _http.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("📞 [Twilio VoIP] Initiating phone call...");
            }
            else
            {
                string errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"📞 [Twilio VoIP] ⚠️ Failed. Status: {response.StatusCode}, Details: {errorContent}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"📞 [Twilio VoIP] ❌ Error: {ex.Message}");
        }
    }

    public IEnumerable<HeartbeatPulse> GetAllStatuses()
    {
        lock (_engineLock)
        {
            return _activePulses.Values.ToList();
        }
    }

    public void Dispose()
    {
        _monitorTimer.Dispose();
        _http.Dispose();
    }
}
