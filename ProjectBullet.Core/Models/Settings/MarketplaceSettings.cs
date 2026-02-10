namespace ProjectBullet.Core.Models.Settings;

public class MarketplaceSettings
{
    public string AuthToken { get; set; } = string.Empty;
    public bool IsRegisteredUser { get; set; } = false;
    public string CachedUsername { get; set; } = string.Empty;
    public int CachedUserId { get; set; } = 0;
}
