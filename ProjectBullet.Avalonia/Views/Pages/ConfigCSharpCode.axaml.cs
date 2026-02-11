using AvaloniaEdit;
using AvaloniaEdit.Highlighting;
using AvaloniaEdit.Highlighting.Xshd;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using ProjectBullet.Core.Services;
using ProjectBullet.Avalonia.Helpers;
using ProjectBullet.Avalonia.ViewModels;
using RuriLib.Helpers.Transpilers;
using RuriLib.Models.Configs;
using System;
using System.Linq;
using System.Xml;

namespace ProjectBullet.Avalonia.Views.Pages
{
    /// <summary>
    /// Interaction logic for ConfigCSharpCode.axaml
    /// </summary>
    public partial class ConfigCSharpCode : UserControl
    {
        private readonly ConfigCSharpCodeViewModel vm;
        private readonly ConfigService configService;
        private readonly ProjectBulletSettingsService obSettingsService;
        private Config Config => configService.SelectedConfig;

        public ConfigCSharpCode()
        {
            vm = new ConfigCSharpCodeViewModel();
            DataContext = vm;

            InitializeComponent();
            configService = SP.GetService<ConfigService>();
            obSettingsService = SP.GetService<ProjectBulletSettingsService>();

            HighlightSyntax(editor);
            HighlightSyntax(startupEditor);
        }

        public void UpdateViewModel()
        {
            try
            {
                // Transpile if not in CSharp mode
                if (Config != null && Config.Mode != ConfigMode.CSharp)
                {
                    Config.CSharpScript = Config.Mode == ConfigMode.Stack
                            ? Stack2CSharpTranspiler.Transpile(Config.Stack, Config.Settings)
                            : Loli2CSharpTranspiler.Transpile(Config.LoliCodeScript, Config.Settings);

                    Config.StartupCSharpScript = Loli2CSharpTranspiler.Transpile(
                        Config.StartupLoliCodeScript, Config.Settings);

                    editor.Text = Config.CSharpScript;
                    startupEditor.Text = Config.StartupCSharpScript;

                    if (configService.SelectedConfig.StartupCSharpScript is not null &&
                        configService.SelectedConfig.StartupCSharpScript.Length > 0)
                    {
                        startupEditorContainer.IsVisible = true;
                    }
                }
            }
            catch (Exception ex)
            {
                // On fail, prompt it to the user and go back to the configs page
                Alert.Exception(ex);
                SP.GetService<MainWindow>().NavigateTo(MainWindowPage.Configs);
            }
        }

        private void HighlightSyntax(TextEditor textEditor)
        {
            using var reader = XmlReader.Create("Highlighting/LoliCode.xshd");
            textEditor.SyntaxHighlighting = HighlightingLoader.Load(reader, HighlightingManager.Instance);
        }

        private void ToggleUsings(object sender, RoutedEventArgs e) => usingsContainer.IsVisible = !usingsContainer.IsVisible;

        private void ToggleStartup(object sender, RoutedEventArgs e) => startupEditorContainer.IsVisible = !startupEditorContainer.IsVisible;
    }

    public class ConfigCSharpCodeViewModel : ViewModelBase
    {
        private readonly ConfigService configService;
        private readonly ProjectBulletSettingsService obSettingsService;
        private Config Config => configService.SelectedConfig;

        public ConfigCSharpCodeViewModel()
        {
            configService = SP.GetService<ConfigService>();
            obSettingsService = SP.GetService<ProjectBulletSettingsService>();
        }

        public bool WordWrap => obSettingsService.Settings.CustomizationSettings.WordWrap;

        public string UsingsString
        {
            get => string.Join(Environment.NewLine, Config.Settings.ScriptSettings.CustomUsings);
            set
            {
                Config.Settings.ScriptSettings.CustomUsings = value.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).ToList();
                OnPropertyChanged();
            }
        }
    }
}
