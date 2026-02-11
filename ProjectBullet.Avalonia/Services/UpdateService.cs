using Newtonsoft.Json.Linq;
using ProjectBullet.Core.Services;
using ProjectBullet.Avalonia.Helpers;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace ProjectBullet.Avalonia.Services
{
    public class UpdateService : IDisposable
    {
        private readonly string versionFile = "version.txt";
        private readonly Timer timer;

        public Version CurrentVersion { get; private set; } = new(0, 0, 3);
        public Version RemoteVersion { get; private set; } = new(0, 0, 3);
        public bool IsUpdateAvailable => RemoteVersion > CurrentVersion;
        public string CurrentVersionType => CurrentVersion.Major == 0
            ? (CurrentVersion.Minor == 0 ? "Alpha" : "Beta")
            : "Release";

        public event Action UpdateAvailable;
        public event Action AutoUpdateRequested;

        public UpdateService()
        {
            try
            {
                var content = File.ReadLines(versionFile).First();
                var version = Version.Parse(content);

                if (version > CurrentVersion)
                {
                    CurrentVersion = version;
                }
            }
            catch
            {
                File.WriteAllText(versionFile, CurrentVersion.ToString());
            }

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
                    using HttpClient client = new();
                    client.BaseAddress = new Uri("https://api.github.com/repos/eros1sh/ProjectBullet/");
                    client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:84.0) Gecko/20100101 Firefox/84.0");
                    var response = await client.GetAsync("releases/latest");

                    var json = await response.Content.ReadAsStringAsync();
                    var release = JToken.Parse(json);
                    var releaseName = release["tag_name"].ToString();

                    RemoteVersion = Version.Parse(releaseName);

                    if (IsUpdateAvailable)
                    {
                        UpdateAvailable?.Invoke();

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
