using ProjectBullet.Native.Helpers;
using ProjectBullet.Native.Services;
using ProjectBullet.Native.Views.Dialogs;
using System.Windows;
using System.Windows.Controls;

namespace ProjectBullet.Native.Views.Pages
{
    /// <summary>
    /// Interaction logic for About.xaml
    /// </summary>
    public partial class About : Page
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
                        latestVersionPanel.Visibility = Visibility.Visible;
                        latestVersionText.Text = updateService.RemoteVersion.ToString();
                    }
                    else
                    {
                        upToDateText.Visibility = Visibility.Visible;
                    }
                }

                updateService.UpdateAvailable += () =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        latestVersionPanel.Visibility = Visibility.Visible;
                        latestVersionText.Text = updateService.RemoteVersion.ToString();
                        upToDateText.Visibility = Visibility.Collapsed;
                    });
                };
            }
            catch { }
        }

        private void OpenLicense(object sender, RoutedEventArgs e) => new MainDialog(new LicenseDialog(), "License", true).ShowDialog();

        private void OpenRepository(object sender, RoutedEventArgs e) => Url.Open("https://github.com/eros1sh/ProjectBullet");

        private void OpenGitHub(object sender, RoutedEventArgs e) => Url.Open("https://github.com/eros1sh");

        private void OpenTelegram(object sender, RoutedEventArgs e) => Url.Open("https://t.me/eros_sh");
    }
}
