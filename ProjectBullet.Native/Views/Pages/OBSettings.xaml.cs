using Microsoft.Win32;
using ProjectBullet.Core.Models.Settings;
using ProjectBullet.Native.Helpers;
using ProjectBullet.Native.Services;
using ProjectBullet.Native.ViewModels;
using ProjectBullet.Native.Views.Dialogs;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace ProjectBullet.Native.Views.Pages
{
    /// <summary>
    /// Interaction logic for OBSettings.xaml
    /// </summary>
    public partial class OBSettings : Page
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

        private void SetAppLockPassword(object sender, RoutedEventArgs e)
        {
            new MainDialog(
                new AppLockSetupDialog(hash => vm.SetAppLockPasswordHash(hash)),
                "Set App Lock Password").ShowDialog();
        }

        private void CopyWebhookUrl(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(vm.TelegramWebhookUrl))
            {
                Clipboard.SetText(vm.TelegramWebhookUrl);
                Alert.Info("Copied", "Webhook URL copied to clipboard.");
            }
        }

        private async void SetupWebhook(object sender, RoutedEventArgs e) => await vm.SetupWebhookAsync();
        private async void DeleteWebhook(object sender, RoutedEventArgs e) => await vm.DeleteWebhookAsync();

        private void ChooseBackgroundImage(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog
            {
                Filter = "Images | *.jpg;*.jpeg;*.png;*.bmp",
                FilterIndex = 1
            };

            ofd.ShowDialog();

            if (!string.IsNullOrEmpty(ofd.FileName))
            {
                try
                {
                    vm.SetBackgroundImage(ofd.FileName);
                }
                catch (Exception ex)
                {
                    Alert.Exception(ex);
                }
            }
        }
    }
}
