using Microsoft.Win32;
using ProjectBullet.Native.ViewModels;
using System;
using System.Windows;
using System.Windows.Controls;

namespace ProjectBullet.Native.Views.Dialogs
{
    public partial class MarketplaceUploadDialog : Page
    {
        private readonly MarketplaceViewModel _vm;

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

        private void BrowseFile(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog
            {
                Filter = "All supported files|*.opk;*.pbc;*.zip|Plugin files (*.opk)|*.opk|Config files (*.pbc)|*.pbc|Zip files (*.zip)|*.zip|All files (*.*)|*.*",
                Title = "Select file to upload"
            };

            if (ofd.ShowDialog() == true)
            {
                filePathBox.Text = ofd.FileName;
            }
        }

        private async void DoUpload(object sender, RoutedEventArgs e)
        {
            errorText.Text = string.Empty;

            var name = nameBox.Text.Trim();
            var category = (categoryCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "config";
            var description = descriptionBox.Text.Trim();
            var version = versionBox.Text.Trim();
            var password = passwordBox.Password;
            var filePath = filePathBox.Text.Trim();

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
                var dialog = Parent as MainDialog;
                if (dialog != null)
                {
                    dialog.DialogResult = true;
                    dialog.Close();
                }
            }
            else
            {
                errorText.Text = error ?? "Upload failed.";
            }
        }
    }
}
