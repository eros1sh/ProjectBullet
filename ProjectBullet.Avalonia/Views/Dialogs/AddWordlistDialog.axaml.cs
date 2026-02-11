using ProjectBullet.Core.Entities;
using ProjectBullet.Avalonia.Helpers;
using ProjectBullet.Avalonia.Views.Pages;
using RuriLib.Models.Environment;
using RuriLib.Models.Proxies;
using RuriLib.Services;
using System;
using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;

namespace ProjectBullet.Avalonia.Views.Dialogs
{
    public partial class AddWordlistDialog : UserControl
    {
        private readonly object caller;
        private readonly EnvironmentSettings env;

        public AddWordlistDialog()
        {
            InitializeComponent();
        }

        public AddWordlistDialog(object caller)
        {
            this.caller = caller;
            InitializeComponent();

            env = SP.GetService<RuriLibSettingsService>().Environment;

            typeCombobox.ItemsSource = env.WordlistTypes.Select(t => t.Name);
            typeCombobox.SelectedIndex = 0;
        }

        private async void SearchInFolder(object sender, PointerPressedEventArgs e)
        {
            var topLevel = TopLevel.GetTopLevel(this);
            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Select wordlist file",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("Wordlist files") { Patterns = new[] { "*.txt" } }
                }
            });

            if (files.Count > 0)
            {
                var filePath = files[0].Path.LocalPath;
                locationTextbox.Text = filePath;
                nameTextbox.Text = Path.GetFileNameWithoutExtension(filePath);

                // Set the recognized wordlist type
                try
                {
                    var first = File.ReadLines(filePath).First();
                    typeCombobox.SelectedItem = env.RecognizeWordlistType(first);
                }
                catch { }
            }
        }

        private void Accept(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(nameTextbox.Text))
            {
                Alert.Error("Invalid name", "The name cannot be blank");
                return;
            }

            var path = locationTextbox.Text;
            var cwd = Directory.GetCurrentDirectory();

            // Make the path relative if inside the CWD
            if (path.StartsWith(cwd))
            {
                path = path[(cwd.Length + 1)..];
            }

            var entity = new WordlistEntity
            {
                Name = nameTextbox.Text,
                FileName = path.Replace("\\", "/"),
                Type = typeCombobox.SelectedItem?.ToString() ?? "Default",
                Purpose = purposeTextbox.Text,
                Total = File.ReadLines(path).Count()
            };

            if (caller is Wordlists page)
            {
                page.AddWordlist(entity);
            }
            else if (caller is MultiRunJobOptionsDialog dialog)
            {
                dialog.AddWordlist(entity);
            }

            if (this.VisualRoot is MainDialog dlg) dlg.Close();
        }
    }
}
