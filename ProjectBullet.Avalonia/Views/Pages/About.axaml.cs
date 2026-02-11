using Avalonia.Controls;
using Avalonia.Interactivity;
using ProjectBullet.Avalonia.Helpers;
using ProjectBullet.Avalonia.Views.Dialogs;

namespace ProjectBullet.Avalonia.Views.Pages
{
    public partial class About : UserControl
    {
        public About()
        {
            InitializeComponent();
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
