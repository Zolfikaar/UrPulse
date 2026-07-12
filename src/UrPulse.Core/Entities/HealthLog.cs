using System.ComponentModel.DataAnnotations;

namespace UrPulse.Core.Entities;

public class HealthLog
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public string AppId { get; set; } = string.Empty;

    [Required]
    public string ServiceName { get; set; } = "General";

    [Required]
    public string Status { get; set; } = "Healthy";

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    // سنخزن بيانات الـ RAM والـ CPU هنا كنص JSON لمرونة القراءة مستقبلاً
    public string HardwareMetricsJson { get; set; } = string.Empty;
}