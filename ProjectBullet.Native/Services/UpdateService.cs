using Newtonsoft.Json.Linq;
using ProjectBullet.Core.Services;
using ProjectBullet.Native.Helpers;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace ProjectBullet.Native.Services
{
    public class UpdateService : IDisposable
    {
        private readonly string versionFile = "version.txt";
        private readonly Timer timer;

        public Version CurrentVersion { get; private set; } = new(0, 0, 1);
        public Version RemoteVersion { get; private set; } = new(0, 0, 1);
        public bool IsUpdateAvailable => RemoteVersion > CurrentVersion;
        public string CurrentVersionType => CurrentVersion.Major == 0
            ? (CurrentVersion.Minor == 0 ? "Alpha" : "Beta")
            : "Release";

        public event Action UpdateAvailable;

        /// <summary>
        /// Fired when auto-update is enabled and a new version is found.
        /// The handler should show the auto-update countdown dialog.
        /// </summary>
        public event Action AutoUpdateRequested;

        public UpdateService()
        {
            // Try to read the current version from disk
            try
            {
                var content = File.ReadLines(versionFile).First();
                var version = Version.Parse(content);

                // If higher than the minimum expected current version, set it
                if (version > CurrentVersion)
                {
                    CurrentVersion = version;
                }
            }
            // If there is no file or the version number is invalid
            catch
            {
                File.WriteAllText(versionFile, CurrentVersion.ToString());
            }

            // Check for updates once a day
            timer = new Timer(new TimerCallback(async _ => await FetchRemoteVersionAsync()),
                    null, 0, (int)TimeSpan.FromDays(1).TotalMilliseconds);
        }

        private async Task FetchRemoteVersionAsync()
        {
            var isDebug = false;

#if DEBUG
            isDebug = true;
#endif

            if (isDebug)
            {
                Console.WriteLine("Skipped updates check in debug mode");
                await Task.Delay(1);
                return;
            }
            else
            {
                try
                {
                    // Query the github api to get a list of the latest releases
                    using HttpClient client = new();
                    client.BaseAddress = new Uri("https://api.github.com/repos/eros1sh/ProjectBullet/");
                    client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:84.0) Gecko/20100101 Firefox/84.0");
                    var response = await client.GetAsync("releases/latest");

                    // Take the first and get its name
                    var json = await response.Content.ReadAsStringAsync();
                    var release = JToken.Parse(json);
                    var releaseName = release["tag_name"].ToString();

                    // Try to parse that name to a Version object
                    RemoteVersion = Version.Parse(releaseName);

                    if (IsUpdateAvailable)
                    {
                        UpdateAvailable?.Invoke();

                        // Check if auto-update is enabled
                        try
                        {
                            var settingsService = SP.GetService<ProjectBulletSettingsService>();
                            if (settingsService.Settings.GeneralSettings.AutoUpdate)
                            {
                                AutoUpdateRequested?.Invoke();
                            }
                        }
                        catch
                        {
                            // Settings service not available yet, skip auto-update
                        }
                    }
                }
                catch
                {
                    Console.WriteLine("Failed to check for updates. I will retry in 1 day.");
                }
            }
        }

        public void Dispose() => timer?.Dispose();
    }
}
