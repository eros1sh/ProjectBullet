using ProjectBullet.Avalonia.ViewModels;
using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;

namespace ProjectBullet.Avalonia.Views.Dialogs
{
    public partial class MarketplaceUploadDialog : UserControl
    {
        private readonly MarketplaceViewModel _vm;

        public MarketplaceUploadDialog()
        {
            InitializeComponent();
        }

        public MarketplaceUploadDialog(MarketplaceViewModel vm)
        {
            _vm = vm;
            InitializeComponent();
        }

        public MarketplaceUploadDialog(MarketplaceViewModel vm, string name, string filePath)
        {
            _vm = vm;
            InitializeComponent();
            nameBox.Text = name;
            filePathBox.Text = filePath;
        }

        private async void BrowseFile(object sender, RoutedEventArgs e)
        {
            var topLevel = TopLevel.GetTopLevel(this);
            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Select file to upload",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("All supported files") { Patterns = new[] { "*.opk", "*.pbc", "*.zip" } },
                    new FilePickerFileType("All files") { Patterns = new[] { "*.*" } }
                }
            });

            if (files.Count > 0)
            {
                filePathBox.Text = files[0].Path.LocalPath;
            }
        }

        private async void DoUpload(object sender, RoutedEventArgs e)
        {
            errorText.Text = string.Empty;

            var name = nameBox.Text?.Trim();
            var category = (categoryCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "config";
            var description = descriptionBox.Text?.Trim();
            var version = versionBox.Text?.Trim();
            var password = passwordBox.Text;
            var filePath = filePathBox.Text?.Trim();

            if (string.IsNullOrEmpty(name))
            {
                errorText.Text = "Please enter a name.";
                return;
            }

            if (string.IsNullOrEmpty(version))
            {
                errorText.Text = "Please enter a version.";
                return;
            }

            if (string.IsNullOrEmpty(filePath))
            {
                errorText.Text = "Please select a file.";
                return;
            }

            var (success, error) = await _vm.UploadItemAsync(name, category, description, version, password, filePath);

            if (success)
            {
                if (this.VisualRoot is MainDialog dialog)
                {
                    dialog.Close(true);
                }
            }
            else
            {
                errorText.Text = error ?? "Upload failed.";
            }
        }
    }
}
