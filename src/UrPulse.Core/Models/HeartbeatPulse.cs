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
