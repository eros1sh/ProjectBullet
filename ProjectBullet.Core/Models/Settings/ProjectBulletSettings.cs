namespace ProjectBullet.Core.Models.Settings;

/// <summary>
/// Settings for the ProjectBullet application.
/// </summary>
public class ProjectBulletSettings
{
    /// <summary>
    /// General settings.
    /// </summary>
    public GeneralSettings GeneralSettings { get; set; } = new();

    /// <summary>
    /// Settings related to remote repositories.
    /// </summary>
    public RemoteSettings RemoteSettings { get; set; } = new();

    /// <summary>
    /// Settings related to security.
    /// </summary>
    public SecuritySettings SecuritySettings { get; set; } = new();

    /// <summary>
    /// Settings related to the appearance of the UI.
    /// </summary>
    public CustomizationSettings CustomizationSettings { get; set; } = new();

    /// <summary>
    /// Settings related to Telegram bot integration.
    /// </summary>
    public TelegramSettings TelegramSettings { get; set; } = new();

    /// <summary>
    /// Settings related to the plugin marketplace.
    /// </summary>
    public MarketplaceSettings MarketplaceSettings { get; set; } = new();

    /// <summary>
    /// Settings related to app lock (PIN/password protection).
    /// </summary>
    public AppLockSettings AppLockSettings { get; set; } = new();
}
