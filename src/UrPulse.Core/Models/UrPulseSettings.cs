namespace UrPulse.Core.Models;

/// <summary>
/// Single global system + alerting configuration for all monitored apps.
/// Clients must follow these timings — project-local overrides are not authoritative.
/// </summary>
public class UrPulseSettings
{
    /// <summary>How often monitored apps should POST heartbeats (Core is source of truth).</summary>
    public int GlobalHeartbeatIntervalSeconds { get; set; } = 10;

    /// <summary>Silence longer than this marks an app Offline.</summary>
    public int GlobalOfflineThresholdSeconds { get; set; } = 20;

    /// <summary>How often PulseEngine re-evaluates live heartbeats.</summary>
    public int GlobalScanIntervalSeconds { get; set; } = 5;

    /// <summary>Seconds to remain Offline before Telegram/Twilio/beep escalation fires.</summary>
    public int GlobalEscalationDelaySeconds { get; set; } = 30;

    public bool EnableAlerts { get; set; } = true;
    public bool LocalAudioAlerts { get; set; } = true;
    public TelegramSettings Telegram { get; set; } = new();
    public TwilioSettings Twilio { get; set; } = new();
}

public class TelegramSettings
{
    public string BotToken { get; set; } = string.Empty;
    public string ChatId { get; set; } = string.Empty;
}

public class TwilioSettings
{
    public string AccountSid { get; set; } = string.Empty;
    public string AuthToken { get; set; } = string.Empty;
    public string FromNumber { get; set; } = string.Empty;
    public string ToNumber { get; set; } = string.Empty;
    public string VoiceMessage { get; set; } = string.Empty;
}
