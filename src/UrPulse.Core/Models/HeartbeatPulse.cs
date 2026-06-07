namespace UrPulse.Core.Models;

public class HeartbeatPulse
{
    public string AppId { get; set; } = string.Empty;
    public string ServiceName { get; set; } = "General";
    public string Status { get; set; } = "Healthy";
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    // توثيق لحظة الدخول في حالة Offline لحساب وقت التصعيد بدقة
    public DateTime? OfflineSince { get; set; }

    // هل تم إطلاق إنذار التصعيد الحاد؟ لكي لا يتكرر التنبيه المزعج كل 5 ثوانٍ
    public bool EscalationTriggered { get; set; }

    public Dictionary<string, string> Metadata { get; set; } = new();

    // إعدادات التنبيه الخاصة بهذا التطبيق (يتحكم بها المستخدم)
    public AlertSettings Alerts { get; set; } = new();
}

public class AlertSettings
{
    // تشغيل أو إطفاء نظام التنبيهات بالكامل لهذا التطبيق
    public bool EnableAlerts { get; set; } = true;

    // العتبة الزمنية للتصعيد (مثلاً: 60 ثانية بعد الأوفلاين)
    public int EscalationThresholdSeconds { get; set; } = 60;

    // تفعيل التنبيه الصاخب (الصوتي/المزعج)
    public bool EnableLoudAudioAlert { get; set; } = true;

    // رابط الـ Webhook لإرسال التنبيهات (مثل Discord أو Telegram) لو رغب المستخدم
    public string WebhookUrl { get; set; } = string.Empty;

    public bool EnableTelegramAlert { get; set; } = true;
    public string TelegramBotToken { get; set; } = string.Empty;
    public string TelegramChatId { get; set; } = string.Empty;

    // ==========================================
    // إعدادات Twilio  (الخيار المدفوع)
    // ==========================================
    public bool EnableVoiceCallAlert { get; set; } = true;
    public string TwilioAccountSid { get; set; } = string.Empty;
    public string TwilioAuthToken { get; set; } = string.Empty;
    public string TwilioFromNumber { get; set; } = string.Empty; // رقم تويليو الأمريكي
    public string TargetPhoneNumber { get; set; } = string.Empty; // رقم هاتف المستخدم الشخصي
    public string CustomVoiceMessage { get; set; } = string.Empty;
}