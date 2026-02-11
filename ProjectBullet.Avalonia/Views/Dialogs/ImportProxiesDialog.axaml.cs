using ProjectBullet.Avalonia.Extensions;
using ProjectBullet.Avalonia.Helpers;
using ProjectBullet.Avalonia.Views.Pages;
using RuriLib.Models.Proxies;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;

namespace ProjectBullet.Avalonia.Views.Dialogs
{
    public partial class ImportProxiesDialog : UserControl
    {
        private readonly object caller;

        public ImportProxiesDialog()
        {
            InitializeComponent();
        }

        public ImportProxiesDialog(object caller)
        {
            this.caller = caller;
            InitializeComponent();

            proxyTypeCombobox.ItemsSource = Enum.GetNames(typeof(ProxyType));
            proxyTypeCombobox.SelectedIndex = 0;
        }

        private async void SearchInFolder(object sender, PointerPressedEventArgs e)
        {
            var topLevel = TopLevel.GetTopLevel(this);
            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Select proxy file",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("Proxy files") { Patterns = new[] { "*.txt" } }
                }
            });

            if (files.Count > 0)
            {
                locationTextbox.Text = files[0].Path.LocalPath;
            }
        }

        private async void Accept(object sender, RoutedEventArgs e)
        {
            try
            {
                switch (modeTabControl.SelectedIndex)
                {
                    // File
                    case 0:
                        await ReturnLinesAsync(await File.ReadAllTextAsync(locationTextbox.Text).ConfigureAwait(false));
                        break;

                    // Paste
                    case 1:
                        await Dispatcher.UIThread.InvokeAsync(async () =>
                            await ReturnLinesAsync(proxiesBox.Text));
                        break;

                    // Remote
                    case 2:
                        await Dispatcher.UIThread.InvokeAsync(async () =>
                            await ImportFromUrlAsync(urlTextbox.Text));
                        break;
                }
            }
            catch (Exception ex)
            {
                Alert.Exception(ex);
            }
        }

        private async Task ImportFromUrlAsync(string url)
        {
            using var client = new HttpClient();
            using var request = new HttpRequestMessage();

            request.RequestUri = new Uri(url);
            request.Headers.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/83.0.4103.116 Safari/537.36");

            using var response = await client.SendAsync(request);
            var text = await response.Content.ReadAsStringAsync();
            await ReturnLinesAsync(text);
        }

        private async Task ReturnLinesAsync(string text)
        {
            var lines = text.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

            var dto = await Dispatcher.UIThread.InvokeAsync(() => new DTOs.ProxiesForImportDto
            {
                Lines = lines,
                DefaultType = proxyTypeCombobox.SelectedItem.AsEnum<ProxyType>(),
                DefaultUsername = usernameTextbox.Text,
                DefaultPassword = passwordTextbox.Text
            });

            if (caller is Proxies page)
            {
                page.AddProxies(dto);
            }

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (this.VisualRoot is MainDialog dialog) dialog.Close();
            });
        }

        private void SelectFileMode(object sender, PointerPressedEventArgs e)
        {
            fileMode.Foreground = Helpers.Brush.Get("ForegroundMenuSelected");
            pasteMode.Foreground = Helpers.Brush.Get("ForegroundMain");
            remoteMode.Foreground = Helpers.Brush.Get("ForegroundMain");
            modeTabControl.SelectedIndex = 0;
        }

        private void SelectPasteMode(object sender, PointerPressedEventArgs e)
        {
            fileMode.Foreground = Helpers.Brush.Get("ForegroundMain");
            pasteMode.Foreground = Helpers.Brush.Get("ForegroundMenuSelected");
            remoteMode.Foreground = Helpers.Brush.Get("ForegroundMain");
            modeTabControl.SelectedIndex = 1;
        }

        private void SelectRemoteMode(object sender, PointerPressedEventArgs e)
        {
            fileMode.Foreground = Helpers.Brush.Get("ForegroundMain");
            pasteMode.Foreground = Helpers.Brush.Get("ForegroundMain");
            remoteMode.Foreground = Helpers.Brush.Get("ForegroundMenuSelected");
            modeTabControl.SelectedIndex = 2;
        }
    }
}
