using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using ProjectBullet.Core.Repositories;
using ProjectBullet.Core.Services;
using ProjectBullet.Avalonia.Helpers;
using ProjectBullet.Avalonia.Services;
using ProjectBullet.Avalonia.ViewModels;
using ProjectBullet.Avalonia.Views.Dialogs;
using ProjectBullet.Avalonia.Views.Pages.Shared;
using RuriLib.Models.Configs;
using System;
using System.Threading.Tasks;

namespace ProjectBullet.Avalonia.Views.Pages
{
    /// <summary>
    /// Interaction logic for ConfigEditor.axaml
    /// </summary>
    public partial class ConfigEditor : UserControl
    {
        private readonly MainWindow mainWindow;
        private readonly ConfigEditorViewModel vm;
        private readonly Debugger debugger;
        private readonly ConfigStacker stackerPage;
        private readonly ConfigLoliCode loliCodePage;
        private readonly ConfigCSharpCode cSharpPage;
        private readonly ConfigLoliScript loliScriptPage;
        private readonly DispatcherTimer autoSaveTimer;

        public ConfigEditor()
        {
            mainWindow = SP.GetService<MainWindow>();
            vm = new ConfigEditorViewModel();
            DataContext = vm;

            InitializeComponent();

            // Create the pages
            debugger = new();
            stackerPage = new();
            loliCodePage = new();
            cSharpPage = new();
            loliScriptPage = new();

            debuggerFrame.Content = debugger;

            // Auto-save timer (every 2 minutes)
            autoSaveTimer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(2) };
            autoSaveTimer.Tick += async (_, _) =>
            {
                try
                {
                    if (vm.Config != null && !vm.Config.IsRemote)
                    {
                        OnPageChanged();
                        await vm.Save();
                    }
                }
                catch { /* Silent auto-save failure */ }
            };
            autoSaveTimer.Start();
        }

        public void NavigateTo(ConfigEditorSection section)
        {
            switch (section)
            {
                case ConfigEditorSection.Stacker:
                    stackerPage.UpdateViewModel();
                    editorFrame.Content = stackerPage;
                    break;

                case ConfigEditorSection.LoliCode:
                    loliCodePage.UpdateViewModel();
                    editorFrame.Content = loliCodePage;
                    break;

                case ConfigEditorSection.CSharp:
                    cSharpPage.UpdateViewModel();
                    editorFrame.Content = cSharpPage;
                    break;

                case ConfigEditorSection.LoliScript:
                    loliScriptPage.UpdateViewModel();
                    editorFrame.Content = loliScriptPage;
                    break;

                default:
                    break;
            }

            UpdateButtonsVisibility();
        }

        private void UpdateButtonsVisibility()
        {
            if (vm.Config.Mode == ConfigMode.Stack || vm.Config.Mode == ConfigMode.LoliCode)
            {
                stackerButton.IsVisible = editorFrame.Content != stackerPage;
                loliCodeButton.IsVisible = editorFrame.Content != loliCodePage;
                cSharpButton.IsVisible = editorFrame.Content != cSharpPage;
            }
            else // C# only mode OR LoliScript mode
            {
                stackerButton.IsVisible = false;
                loliCodeButton.IsVisible = false;
                cSharpButton.IsVisible = false;
            }
        }

        /// <summary>
        /// Call this when changing page via the dropdown menu otherwise it
        /// will not save the content of the LoliCode editor.
        /// </summary>
        public void OnPageChanged()
        {
            if (editorFrame.Content == loliCodePage)
            {
                loliCodePage.OnPageChanged();
            }
        }

        private void OpenStacker(object sender, RoutedEventArgs e) => mainWindow.NavigateTo(MainWindowPage.ConfigStacker);
        private void OpenLoliCode(object sender, RoutedEventArgs e) => mainWindow.NavigateTo(MainWindowPage.ConfigLoliCode);
        private void OpenCSharpCode(object sender, RoutedEventArgs e) => mainWindow.NavigateTo(MainWindowPage.ConfigCSharpCode);

        private async void Save(object sender, RoutedEventArgs e)
        {
            try
            {
                await vm.Save();
                Alert.Success("Success", $"{vm.Config.Metadata.Name} was saved successfully!");
            }
            catch (Exception ex)
            {
                Alert.Exception(ex);
            }
        }

        private async void Publish(object sender, RoutedEventArgs e)
        {
            try
            {
                // Save config first
                await vm.Save();

                var marketplaceVm = SP.GetService<ViewModelsService>().Marketplace;
                var dialogPage = new MarketplaceUploadDialog(marketplaceVm, vm.Config.Metadata.Name, vm.ConfigFilePath);
                var dialog = new MainDialog(dialogPage, "Publish Config to Marketplace", 500, 450);
                await dialog.ShowDialog(TopLevel.GetTopLevel(this) as Window);
            }
            catch (Exception ex)
            {
                Alert.Exception(ex);
            }
        }
    }

    public enum ConfigEditorSection
    {
        Stacker,
        LoliCode,
        CSharp,
        LoliScript
    }

    public class ConfigEditorViewModel : ViewModelBase
    {
        private readonly IConfigRepository configRepo;
        private readonly ConfigService configService;
        private readonly MarketplaceApiService marketplaceApi;
        public Config Config => configService.SelectedConfig;

        public bool IsMarketplaceRegistered => marketplaceApi?.IsRegisteredUser ?? false;

        public string ConfigFilePath => Config != null
            ? System.IO.Path.Combine("UserData", "Configs", $"{Config.Id}.opk").Replace('\\', '/')
            : string.Empty;

        public ConfigEditorViewModel()
        {
            configRepo = SP.GetService<IConfigRepository>();
            configService = SP.GetService<ConfigService>();
            marketplaceApi = SP.GetService<MarketplaceApiService>();
        }

        public Task Save() => configRepo.SaveAsync(Config);
    }
}
