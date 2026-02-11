using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using ProjectBullet.Avalonia.Helpers;
using ProjectBullet.Avalonia.Services;
using ProjectBullet.Avalonia.ViewModels;
using System;

namespace ProjectBullet.Avalonia.Views.Pages
{
    /// <summary>
    /// Interaction logic for ConfigMetadata.axaml
    /// </summary>
    public partial class ConfigMetadata : UserControl
    {
        private readonly ConfigMetadataViewModel vm;

        public ConfigMetadata()
        {
            vm = SP.GetService<ViewModelsService>().ConfigMetadata;
            DataContext = vm;

            InitializeComponent();
        }

        public void UpdateViewModel() => vm.UpdateViewModel();

        private async void OpenIcon(object sender, RoutedEventArgs e)
        {
            var topLevel = TopLevel.GetTopLevel(this);
            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Select Icon",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("Images") { Patterns = new[] { "*.ico", "*.jpg", "*.jpeg", "*.png", "*.bmp" } }
                }
            });

            if (files.Count > 0)
            {
                try
                {
                    vm.SetIconFromFile(files[0].Path.LocalPath);
                }
                catch (Exception ex)
                {
                    Alert.Exception(ex);
                }
            }
        }

        private async void DownloadIcon(object sender, RoutedEventArgs e)
        {
            try
            {
                await vm.SetIconFromUrlAsync(urlTextbox.Text);
            }
            catch (Exception ex)
            {
                Alert.Exception(ex);
            }
        }
    }
}
