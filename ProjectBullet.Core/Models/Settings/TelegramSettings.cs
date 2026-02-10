using System.Collections.Generic;

namespace ProjectBullet.Core.Models.Settings;

public class TelegramSettings
{
    public bool Enabled { get; set; } = false;
    public string BotToken { get; set; } = string.Empty;
    public long ChatId { get; set; } = 0;
    public List<long> WhitelistedUserIds { get; set; } = new();
    public bool SendHitNotifications { get; set; } = true;
    public bool EnableJobStartStopNotifications { get; set; } = false;
    public bool EnableDailySummary { get; set; } = false;
    public string DailySummaryTime { get; set; } = "09:00";
    public bool EnhancedHitFormat { get; set; } = true;

    // Webhook relay
    public bool UseWebhookRelay { get; set; } = false;
    public string WebhookSecret { get; set; } = string.Empty;
    public string WebhookUrl { get; set; } = string.Empty;
}
