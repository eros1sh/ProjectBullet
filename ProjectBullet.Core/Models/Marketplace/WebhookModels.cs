using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ProjectBullet.Core.Models.Marketplace;

public class WebhookSetupResponse
{
    [JsonPropertyName("webhook_url")]
    public string WebhookUrl { get; set; } = string.Empty;

    [JsonPropertyName("secret")]
    public string Secret { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}

public class WebhookConfigResponse
{
    [JsonPropertyName("webhook_url")]
    public string WebhookUrl { get; set; } = string.Empty;

    [JsonPropertyName("secret")]
    public string Secret { get; set; } = string.Empty;

    [JsonPropertyName("bot_token")]
    public string BotToken { get; set; } = string.Empty;

    [JsonPropertyName("created_at")]
    public string CreatedAt { get; set; } = string.Empty;
}

public class WebhookPollResponse
{
    [JsonPropertyName("messages")]
    public List<WebhookMessage> Messages { get; set; } = new();
}

public class WebhookMessage
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("update_data")]
    public string UpdateData { get; set; } = string.Empty;

    [JsonPropertyName("received_at")]
    public string ReceivedAt { get; set; } = string.Empty;
}

public class WebhookAckResponse
{
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}

public class WebhookDeleteResponse
{
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}
