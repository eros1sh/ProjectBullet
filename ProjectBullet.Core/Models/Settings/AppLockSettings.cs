namespace ProjectBullet.Core.Models.Settings;

public class AppLockSettings
{
    public bool Enabled { get; set; } = false;
    public string PasswordHash { get; set; } = string.Empty;
    public int AutoLockMinutes { get; set; } = 0; // 0 = disabled
}
