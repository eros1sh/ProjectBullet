using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using ProjectBullet.Avalonia.Services;
using ProjectBullet.Avalonia.ViewModels;
using System.Linq;

namespace ProjectBullet.Avalonia.Views.Pages
{
    /// <summary>
    /// Interaction logic for Plugins.axaml
    /// </summary>
    public partial class Plugins : UserControl
    {
        private readonly PluginsViewModel vm;

        public Plugins()
        {
            vm = SP.GetService<ViewModelsService>().Plugins;
            DataContext = vm;

            InitializeComponent();
        }

        private async void AddPlugin(object sender, RoutedEventArgs e)
        {
            var topLevel = TopLevel.GetTopLevel(this);
            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Select plugin",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("Plugin Files") { Patterns = new[] { "*.zip" } }
                }
            });

            if (files.Any())
            {
                var path = files[0].Path.LocalPath;
                if (!string.IsNullOrWhiteSpace(path))
                {
                    vm.Add(path);
                }
            }
        }

        private void RemovePlugin(object sender, RoutedEventArgs e) => vm.Delete((PluginInfo)(sender as Button).Tag);
    }
}
