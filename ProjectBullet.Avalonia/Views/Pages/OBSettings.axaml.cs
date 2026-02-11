using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using ProjectBullet.Core.Models.Settings;
using ProjectBullet.Avalonia.Helpers;
using ProjectBullet.Avalonia.Services;
using ProjectBullet.Avalonia.ViewModels;
using ProjectBullet.Avalonia.Views.Dialogs;
using System;
using System.Linq;

namespace ProjectBullet.Avalonia.Views.Pages
{
    /// <summary>
    /// Interaction logic for OBSettings.axaml
    /// </summary>
    public partial class OBSettings : UserControl
    {
        private readonly OBSettingsViewModel vm;

        public OBSettings()
        {
            vm = SP.GetService<ViewModelsService>().OBSettings;
            DataContext = vm;

            InitializeComponent();

            configSectionOnLoadCombobox.ItemsSource = Enum.GetValues(typeof(ConfigSection)).Cast<ConfigSection>();
        }

        private async void Save(object sender, RoutedEventArgs e) => await vm.Save();
        private void Reset(object sender, RoutedEventArgs e) => vm.Reset();
        private void ResetCustomization(object sender, RoutedEventArgs e) => vm.ResetCustomization();

        private void AddProxyCheckTarget(object sender, RoutedEventArgs e) => vm.AddProxyCheckTarget();
        private void RemoveProxyCheckTarget(object sender, RoutedEventArgs e)
            => vm.RemoveProxyCheckTarget((ProxyCheckTarget)(sender as Button).Tag);

        private void AddCustomSnippet(object sender, RoutedEventArgs e) => vm.AddCustomSnippet();
        private void RemoveCustomSnippet(object sender, RoutedEventArgs e)
            => vm.RemoveCustomSnippet((CustomSnippet)(sender as Button).Tag);

        private void AddRemoteConfigsEndpoint(object sender, RoutedEventArgs e) => vm.AddRemoteConfigsEndpoint();
        private void RemoveRemoteConfigsEndpoint(object sender, RoutedEventArgs e)
            => vm.RemoveRemoteConfigsEndpoint((RemoteConfigsEndpoint)(sender as Button).Tag);

        private async void SetAppLockPassword(object sender, RoutedEventArgs e)
        {
            await new MainDialog(
                new AppLockSetupDialog(hash => vm.SetAppLockPasswordHash(hash)),
                "Set App Lock Password").ShowDialog(TopLevel.GetTopLevel(this) as Window);
        }

        private async void CopyWebhookUrl(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(vm.TelegramWebhookUrl))
            {
                if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
                    && desktop.MainWindow?.Clipboard is { } clipboard)
                {
                    await clipboard.SetTextAsync(vm.TelegramWebhookUrl);
                }
                Alert.Info("Copied", "Webhook URL copied to clipboard.");
            }
        }

        private async void SetupWebhook(object sender, RoutedEventArgs e) => await vm.SetupWebhookAsync();
        private async void DeleteWebhook(object sender, RoutedEventArgs e) => await vm.DeleteWebhookAsync();

        private async void OpenOB2Migration(object sender, RoutedEventArgs e)
        {
            await new MainDialog(new OB2MigrationDialog(), "Migrate from OpenBullet 2", 550, 600).ShowDialog(TopLevel.GetTopLevel(this) as Window);
        }

        private async void ChooseBackgroundImage(object sender, RoutedEventArgs e)
        {
            var topLevel = TopLevel.GetTopLevel(this);
            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Select background image",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("Images") { Patterns = new[] { "*.jpg", "*.jpeg", "*.png", "*.bmp" } }
                }
            });

            if (files.Any())
            {
                var path = files[0].Path.LocalPath;
                if (!string.IsNullOrEmpty(path))
                {
                    try
                    {
                        vm.SetBackgroundImage(path);
                    }
                    catch (Exception ex)
                    {
                        Alert.Exception(ex);
                    }
                }
            }
        }
    }
}
