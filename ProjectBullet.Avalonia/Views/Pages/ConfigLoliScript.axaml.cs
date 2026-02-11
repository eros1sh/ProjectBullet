using AvaloniaEdit.Highlighting;
using AvaloniaEdit.Highlighting.Xshd;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using ProjectBullet.Core.Repositories;
using ProjectBullet.Core.Services;
using ProjectBullet.Avalonia.Helpers;
using RuriLib.Models.Configs;
using System;
using System.Xml;

namespace ProjectBullet.Avalonia.Views.Pages
{
    /// <summary>
    /// Interaction logic for ConfigLoliScript.axaml
    /// </summary>
    public partial class ConfigLoliScript : UserControl
    {
        private readonly ConfigService configService;
        private readonly IConfigRepository configRepo; // TODO: This should not be here

        public ConfigLoliScript()
        {
            InitializeComponent();
            configService = SP.GetService<ConfigService>();
            configRepo = SP.GetService<IConfigRepository>();

            HighlightSyntax();
        }

        public void UpdateViewModel()
        {
            try
            {
                if (configService.SelectedConfig.Mode != ConfigMode.Legacy)
                {
                    throw new Exception("This page is only available for legacy configs");
                }

                editor.Text = configService.SelectedConfig.LoliScript;
            }
            catch (Exception ex)
            {
                // On fail, prompt it to the user and go back to the configs page
                Alert.Exception(ex);
                SP.GetService<MainWindow>().NavigateTo(MainWindowPage.Configs);
            }
        }

        /// <summary>
        /// Call this when changing page via the dropdown menu otherwise it
        /// will not trigger the LostFocus event on the editor.
        /// </summary>
        public void OnPageChanged() => configService.SelectedConfig.LoliScript = editor.Text;

        private void EditorLostFocus(object sender, RoutedEventArgs e)
            => configService.SelectedConfig.LoliScript = editor.Text;

        private void HighlightSyntax()
        {
            using var reader = XmlReader.Create("Highlighting/LoliScript.xshd");
            editor.SyntaxHighlighting = HighlightingLoader.Load(reader, HighlightingManager.Instance);
        }

        private async void PageKeyDown(object sender, KeyEventArgs e)
        {
            // Save on CTRL+S
            if (e.KeyModifiers.HasFlag(KeyModifiers.Control) && e.Key == Key.S)
            {
                configService.SelectedConfig.LoliScript = editor.Text;
                await configRepo.SaveAsync(configService.SelectedConfig);
                Alert.Success("Saved", $"{configService.SelectedConfig.Metadata.Name} was saved successfully!");
            }
        }
    }
}
