using Avalonia.Controls;
using Avalonia.Interactivity;
using ProjectBullet.Avalonia.Helpers;
using ProjectBullet.Avalonia.Services;
using ProjectBullet.Avalonia.Views.Dialogs;

namespace ProjectBullet.Avalonia.Views.Pages
{
    public partial class About : UserControl
    {
        public About()
        {
            InitializeComponent();
            LoadVersionInfo();
        }

        private void LoadVersionInfo()
        {
            try
            {
                var updateService = SP.GetService<UpdateService>();
                currentVersionText.Text = updateService.CurrentVersion.ToString();
                versionTypeText.Text = $"({updateService.CurrentVersionType})";

                if (updateService.RemoteVersion > new System.Version(0, 0, 3))
                {
                    if (updateService.IsUpdateAvailable)
                    {
                        latestVersionPanel.IsVisible = true;
                        latestVersionText.Text = updateService.RemoteVersion.ToString();
                    }
                    else
                    {
                        upToDateText.IsVisible = true;
                    }
                }

                updateService.UpdateAvailable += () =>
                {
                    global::Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    {
                        latestVersionPanel.IsVisible = true;
                        latestVersionText.Text = updateService.RemoteVersion.ToString();
                        upToDateText.IsVisible = false;
                    });
                };
            }
            catch { }
        }

        private async void OpenLicense(object sender, RoutedEventArgs e)
        {
            var dialog = new MainDialog(new LicenseDialog(), "License", true);
            await dialog.ShowDialog(TopLevel.GetTopLevel(this) as Window);
        }

        private void OpenRepository(object sender, RoutedEventArgs e)
            => Url.Open("https://github.com/eros1sh/ProjectBullet");

        private void OpenGitHub(object sender, RoutedEventArgs e)
            => Url.Open("https://github.com/eros1sh");

        private void OpenTelegram(object sender, RoutedEventArgs e)
            => Url.Open("https://t.me/eros_sh");
    }
}
