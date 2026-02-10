using ProjectBullet.Native.Helpers;
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
        }

        private void OpenLicense(object sender, RoutedEventArgs e) => new MainDialog(new LicenseDialog(), "License", true).ShowDialog();

        private void OpenRepository(object sender, RoutedEventArgs e) => Url.Open("https://github.com/eros1sh/ProjectBullet");

        private void OpenGitHub(object sender, RoutedEventArgs e) => Url.Open("https://github.com/eros1sh");

        private void OpenTelegram(object sender, RoutedEventArgs e) => Url.Open("https://t.me/eros_sh");
    }
}
