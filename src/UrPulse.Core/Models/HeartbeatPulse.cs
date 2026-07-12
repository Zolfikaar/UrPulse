namespace UrPulse.Core.Models;

public class HeartbeatPulse
{
    public string AppId { get; set; } = string.Empty;
    public string ServiceName { get; set; } = "General";
    public string Status { get; set; } = "Healthy";
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public DateTime? OfflineSince { get; set; }
    public bool EscalationTriggered { get; set; }

    public Dictionary<string, string> Metadata { get; set; } = new();
}

public class AlertSettings
{
    public bool EnableAlerts { get; set; } = true;
    public int EscalationThresholdSeconds { get; set; } = 60;
    public bool EnableLoudAudioAlert { get; set; } = true;

    public bool EnableTelegramAlert { get; set; } = false;
    public string TelegramBotToken { get; set; } = string.Empty;
    public string TelegramChatId { get; set; } = string.Empty;

    public bool EnableVoiceCallAlert { get; set; } = false;
    public string TwilioAccountSid { get; set; } = string.Empty;
    public string TwilioAuthToken { get; set; } = string.Empty;
    public string TwilioFromNumber { get; set; } = string.Empty;
    public string TargetPhoneNumber { get; set; } = string.Empty;
    public string CustomVoiceMessage { get; set; } = string.Empty;
}
