using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ProjectBullet.Core.Models.Marketplace;

public class MarketplaceUser
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    [JsonPropertyName("isRegistered")]
    public bool IsRegistered { get; set; }
}

public class AuthResponse
{
    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;

    [JsonPropertyName("user")]
    public MarketplaceUser User { get; set; }
}

public class ItemsResponse
{
    [JsonPropertyName("items")]
    public List<MarketplaceItem> Items { get; set; } = new();

    [JsonPropertyName("page")]
    public int Page { get; set; }

    [JsonPropertyName("total_pages")]
    public int TotalPages { get; set; }

    [JsonPropertyName("total")]
    public int TotalItems { get; set; }
}

public class ItemDetailResponse
{
    [JsonPropertyName("item")]
    public MarketplaceItemDetail Item { get; set; }
}

public class ErrorResponse
{
    [JsonPropertyName("error")]
    public string Error { get; set; } = string.Empty;
}
