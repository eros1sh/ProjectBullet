using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ProjectBullet.Core.Models.Marketplace;

public class MarketplaceItem
{
    private const string AvatarBaseUrl = "https://projectbullet.eros.sh/uploads/avatars/";

    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("slug")]
    public string Slug { get; set; } = string.Empty;

    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    [JsonPropertyName("username")]
    public string Author { get; set; } = string.Empty;

    [JsonPropertyName("user_id")]
    public int UserId { get; set; }

    [JsonPropertyName("user_avatar")]
    public string UserAvatar { get; set; } = string.Empty;

    [JsonPropertyName("downloads")]
    public int Downloads { get; set; }

    [JsonPropertyName("created_at")]
    public string CreatedAt { get; set; } = string.Empty;

    [JsonPropertyName("updated_at")]
    public string UpdatedAt { get; set; } = string.Empty;

    // Computed display properties
    public string AvatarUrl => !string.IsNullOrEmpty(UserAvatar)
        ? AvatarBaseUrl + UserAvatar
        : string.Empty;

    public string UpdatedAtDisplay => UpdatedAt?.Length >= 10 ? UpdatedAt[..10] : UpdatedAt;
}

public class MarketplaceItemVersion
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    [JsonPropertyName("created_at")]
    public string CreatedAt { get; set; } = string.Empty;
}

public class MarketplaceItemDetail
{
    private const string AvatarBaseUrl = "https://projectbullet.eros.sh/uploads/avatars/";

    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("slug")]
    public string Slug { get; set; } = string.Empty;

    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    [JsonPropertyName("username")]
    public string Author { get; set; } = string.Empty;

    [JsonPropertyName("user_id")]
    public int UserId { get; set; }

    [JsonPropertyName("user_avatar")]
    public string UserAvatar { get; set; } = string.Empty;

    [JsonPropertyName("downloads")]
    public int Downloads { get; set; }

    [JsonPropertyName("has_password")]
    public bool HasPassword { get; set; }

    [JsonPropertyName("created_at")]
    public string CreatedAt { get; set; } = string.Empty;

    [JsonPropertyName("updated_at")]
    public string UpdatedAt { get; set; } = string.Empty;

    [JsonPropertyName("versions")]
    public List<MarketplaceItemVersion> Versions { get; set; } = new();

    public string AvatarUrl => !string.IsNullOrEmpty(UserAvatar)
        ? AvatarBaseUrl + UserAvatar
        : string.Empty;
}
