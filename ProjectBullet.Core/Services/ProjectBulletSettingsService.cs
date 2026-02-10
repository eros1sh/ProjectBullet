using Newtonsoft.Json;
using ProjectBullet.Core.Models.Settings;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace ProjectBullet.Core.Services;

/// <summary>
/// Provides interaction with settings of the ProjectBullet application.
/// </summary>
public class ProjectBulletSettingsService
{
    private string BaseFolder { get; }
    private readonly JsonSerializerSettings jsonSettings;

    /// <summary>
    /// The path of the file where settings are saved.
    /// </summary>
    public string FileName => Path.Combine(BaseFolder, "ProjectBulletSettings.json");

    /// <summary>
    /// The actual settings. After modifying them, call the <see cref="SaveAsync"/> method to persist them.
    /// </summary>
    public ProjectBulletSettings Settings { get; private set; }

    public ProjectBulletSettingsService(string baseFolder)
    {
        BaseFolder = baseFolder;
        Directory.CreateDirectory(baseFolder);

        jsonSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            TypeNameHandling = TypeNameHandling.Auto
        };

        // Migrate from old OpenBulletSettings.json if it exists
        var oldFileName = Path.Combine(BaseFolder, "OpenBulletSettings.json");
        if (!File.Exists(FileName) && File.Exists(oldFileName))
        {
            File.Move(oldFileName, FileName);
        }

        if (File.Exists(FileName))
        {
            Settings = JsonConvert.DeserializeObject<ProjectBulletSettings>(File.ReadAllText(FileName), jsonSettings);
        }
        else
        {
            Recreate();
            SaveAsync().Wait();
        }
    }

    /// <summary>
    /// Saves the <see cref="Settings"/> to disk.
    /// </summary>
    public async Task SaveAsync() => await File.WriteAllTextAsync(FileName, JsonConvert.SerializeObject(Settings, jsonSettings));

    /// <summary>
    /// Restores the default <see cref="Settings"/> (does not save to disk).
    /// </summary>
    public void Recreate() => Settings = new ProjectBulletSettings
    {
        GeneralSettings = new GeneralSettings { ProxyCheckTargets = new List<ProxyCheckTarget> { new ProxyCheckTarget() } },
        RemoteSettings = new RemoteSettings(),
        SecuritySettings = new SecuritySettings().GenerateJwtKey().SetupAdminPassword("admin"),
        CustomizationSettings = new CustomizationSettings()
    };
}
