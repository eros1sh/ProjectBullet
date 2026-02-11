using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using ProjectBullet.Core.Models.Settings;
using ProjectBullet.Core.Services;
using ProjectBullet.Avalonia.Helpers;
using ProjectBullet.Avalonia.Services;
using ProjectBullet.Avalonia.ViewModels;
using ProjectBullet.Avalonia.Views.Pages;
using ProjectBullet.Avalonia.Views.Dialogs;
using RuriLib.Models.Configs;
using RuriLib.Models.Jobs;
using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ProjectBullet.Avalonia
{
    public partial class MainWindow : Window
    {
        private readonly UpdateService updateService;
        private readonly MainWindowViewModel vm;

        private bool hoveringConfigsMenuOption;
        private bool hoveringConfigSubmenu;
        private bool forceClose;

        private readonly TextBlock[] labels;

        private Home homePage;
        private Jobs jobsPage;
        private Monitor monitorPage;
        private MultiRunJobViewer multiRunJobViewerPage;
        private ProxyCheckJobViewer proxyCheckJobViewerPage;
        private Proxies proxiesPage;
        private Wordlists wordlistsPage;
        private Configs configsPage;
        private Views.Pages.ConfigMetadata configMetadataPage;
        private ConfigReadme configReadmePage;
        private ConfigEditor configEditorPage;
        private Views.Pages.ConfigSettings configSettingsPage;
        private Hits hitsPage;
        private OBSettings obSettingsPage;
        private RLSettings rlSettingsPage;
        private Plugins pluginsPage;
        private Marketplace marketplacePage;
        private About aboutPage;

        public UserControl CurrentPage { get; private set; }

        public MainWindow()
        {
            vm = new MainWindowViewModel();
            DataContext = vm;
            Closing += OnWindowClosing;

            InitializeComponent();

            labels = new TextBlock[]
            {
                menuOptionAbout,
                menuOptionConfigs,
                menuOptionConfigSettings,
                menuOptionCSharpCode,
                menuOptionHits,
                menuOptionHome,
                menuOptionJobs,
                menuOptionLoliCode,
                menuOptionLoliScript,
                menuOptionMetadata,
                menuOptionMonitor,
                menuOptionOBSettings,
                menuOptionPlugins,
                menuOptionMarketplace,
                menuOptionProxies,
                menuOptionReadme,
                menuOptionRLSettings,
                menuOptionStacker,
                menuOptionWordlists
            };

            configsPage = new();

            updateService = SP.GetService<UpdateService>();
            Title = $"ProjectBullet - {updateService.CurrentVersion} [{updateService.CurrentVersionType}]";

            updateService.AutoUpdateRequested += OnAutoUpdateRequested;

            var obSettingsService = SP.GetService<ProjectBulletSettingsService>();
            var customization = obSettingsService.Settings.CustomizationSettings;
            SetTheme(customization);
        }

        public void NavigateTo(MainWindowPage page)
        {
            if (CurrentPage == configEditorPage)
            {
                configEditorPage?.OnPageChanged();
            }

            switch (page)
            {
                case MainWindowPage.Home:
                    homePage = new Home();
                    ChangePage(homePage, menuOptionHome);
                    break;

                case MainWindowPage.Jobs:
                    if (jobsPage is null) jobsPage = new();
                    ChangePage(jobsPage, menuOptionJobs);
                    break;

                case MainWindowPage.Monitor:
                    if (monitorPage is null) monitorPage = new();
                    ChangePage(monitorPage, menuOptionMonitor);
                    break;

                case MainWindowPage.Proxies:
                    if (proxiesPage is null) proxiesPage = new();
                    proxiesPage.UpdateViewModel();
                    ChangePage(proxiesPage, menuOptionProxies);
                    break;

                case MainWindowPage.Wordlists:
                    if (wordlistsPage is null) wordlistsPage = new();
                    ChangePage(wordlistsPage, menuOptionWordlists);
                    break;

                case MainWindowPage.Configs:
                    if (configsPage is null) configsPage = new();
                    configsPage.UpdateViewModel();
                    ChangePage(configsPage, menuOptionConfigs);
                    break;

                case MainWindowPage.Hits:
                    if (hitsPage is null) hitsPage = new();
                    hitsPage.UpdateViewModel();
                    ChangePage(hitsPage, menuOptionHits);
                    break;

                case MainWindowPage.Plugins:
                    if (pluginsPage is null) pluginsPage = new();
                    ChangePage(pluginsPage, menuOptionPlugins);
                    break;

                case MainWindowPage.Marketplace:
                    if (marketplacePage is null) marketplacePage = new();
                    ChangePage(marketplacePage, menuOptionMarketplace);
                    break;

                case MainWindowPage.OBSettings:
                    if (obSettingsPage is null) obSettingsPage = new();
                    ChangePage(obSettingsPage, menuOptionOBSettings);
                    break;

                case MainWindowPage.RLSettings:
                    if (rlSettingsPage is null) rlSettingsPage = new();
                    ChangePage(rlSettingsPage, menuOptionRLSettings);
                    break;

                case MainWindowPage.About:
                    if (aboutPage is null) aboutPage = new();
                    ChangePage(aboutPage, menuOptionAbout);
                    break;

                case MainWindowPage.ConfigMetadata:
                    CloseSubmenu();
                    if (configMetadataPage is null) configMetadataPage = new();
                    configMetadataPage.UpdateViewModel();
                    ChangePage(configMetadataPage, menuOptionMetadata);
                    break;

                case MainWindowPage.ConfigReadme:
                    CloseSubmenu();
                    if (configReadmePage is null) configReadmePage = new();
                    configReadmePage.UpdateViewModel();
                    ChangePage(configReadmePage, menuOptionReadme);
                    break;

                case MainWindowPage.ConfigStacker:
                    if (vm.Config.Mode is not ConfigMode.Stack and not ConfigMode.LoliCode)
                        return;
                    CloseSubmenu();
                    if (configEditorPage is null) configEditorPage = new();
                    configEditorPage.NavigateTo(ConfigEditorSection.Stacker);
                    ChangePage(configEditorPage, menuOptionStacker);
                    break;

                case MainWindowPage.ConfigLoliCode:
                    if (vm.Config.Mode is not ConfigMode.Stack and not ConfigMode.LoliCode)
                        return;
                    CloseSubmenu();
                    if (configEditorPage is null) configEditorPage = new();
                    configEditorPage.NavigateTo(ConfigEditorSection.LoliCode);
                    ChangePage(configEditorPage, menuOptionLoliCode);
                    break;

                case MainWindowPage.ConfigSettings:
                    CloseSubmenu();
                    if (configSettingsPage is null) configSettingsPage = new();
                    configSettingsPage.UpdateViewModel();
                    ChangePage(configSettingsPage, menuOptionConfigSettings);
                    break;

                case MainWindowPage.ConfigCSharpCode:
                    if (vm.Config.Mode is not ConfigMode.Stack and not ConfigMode.LoliCode and not ConfigMode.CSharp)
                        return;
                    CloseSubmenu();
                    if (configEditorPage is null) configEditorPage = new();
                    configEditorPage.NavigateTo(ConfigEditorSection.CSharp);
                    ChangePage(configEditorPage, menuOptionCSharpCode);
                    break;

                case MainWindowPage.ConfigLoliScript:
                    if (vm.Config.Mode is not ConfigMode.Legacy)
                        return;
                    CloseSubmenu();
                    if (configEditorPage is null) configEditorPage = new();
                    configEditorPage.NavigateTo(ConfigEditorSection.LoliScript);
                    ChangePage(configEditorPage, menuOptionLoliScript);
                    break;
            }
        }

        public void DisplayJob(JobViewModel jobVM)
        {
            switch (jobVM)
            {
                case MultiRunJobViewModel mrj:
                    if (multiRunJobViewerPage is null) multiRunJobViewerPage = new();
                    multiRunJobViewerPage.BindViewModel(mrj);
                    ChangePage(multiRunJobViewerPage, null);
                    break;

                case ProxyCheckJobViewModel pcj:
                    if (proxyCheckJobViewerPage is null) proxyCheckJobViewerPage = new();
                    proxyCheckJobViewerPage.BindViewModel(pcj);
                    ChangePage(proxyCheckJobViewerPage, null);
                    break;
            }
        }

        public void EditJob(JobViewModel jobVM)
        {
            NavigateTo(MainWindowPage.Jobs);
            jobsPage.EditJob(jobVM);
        }

        private void OpenHomePage(object sender, PointerPressedEventArgs e) => NavigateTo(MainWindowPage.Home);
        private void OpenJobsPage(object sender, PointerPressedEventArgs e) => NavigateTo(MainWindowPage.Jobs);
        private void OpenMonitorPage(object sender, PointerPressedEventArgs e) => NavigateTo(MainWindowPage.Monitor);
        private void OpenProxiesPage(object sender, PointerPressedEventArgs e) => NavigateTo(MainWindowPage.Proxies);
        private void OpenWordlistsPage(object sender, PointerPressedEventArgs e) => NavigateTo(MainWindowPage.Wordlists);
        private void OpenConfigsPage(object sender, PointerPressedEventArgs e) => NavigateTo(MainWindowPage.Configs);
        private void OpenHitsPage(object sender, PointerPressedEventArgs e) => NavigateTo(MainWindowPage.Hits);
        private void OpenPluginsPage(object sender, PointerPressedEventArgs e) => NavigateTo(MainWindowPage.Plugins);
        private void OpenMarketplacePage(object sender, PointerPressedEventArgs e) => NavigateTo(MainWindowPage.Marketplace);
        private void OpenOBSettingsPage(object sender, PointerPressedEventArgs e) => NavigateTo(MainWindowPage.OBSettings);
        private void OpenRLSettingsPage(object sender, PointerPressedEventArgs e) => NavigateTo(MainWindowPage.RLSettings);
        private void OpenAboutPage(object sender, PointerPressedEventArgs e) => NavigateTo(MainWindowPage.About);

        private void OpenMetadataPage(object sender, PointerPressedEventArgs e) => NavigateTo(MainWindowPage.ConfigMetadata);
        private void OpenReadmePage(object sender, PointerPressedEventArgs e) => NavigateTo(MainWindowPage.ConfigReadme);
        private void OpenStackerPage(object sender, PointerPressedEventArgs e) => NavigateTo(MainWindowPage.ConfigStacker);
        private void OpenLoliCodePage(object sender, PointerPressedEventArgs e) => NavigateTo(MainWindowPage.ConfigLoliCode);
        private void OpenConfigSettingsPage(object sender, PointerPressedEventArgs e) => NavigateTo(MainWindowPage.ConfigSettings);
        private void OpenCSharpCodePage(object sender, PointerPressedEventArgs e) => NavigateTo(MainWindowPage.ConfigCSharpCode);
        private void OpenLoliScriptPage(object sender, PointerPressedEventArgs e) => NavigateTo(MainWindowPage.ConfigLoliScript);

        private void ChangePage(UserControl newPage, TextBlock newLabel)
        {
            CurrentPage = newPage;
            ContentArea.Content = newPage;

            foreach (var label in labels)
            {
                label.Classes.Remove("menuActive");
                if (!label.Classes.Contains("menuItem"))
                    label.Classes.Add("menuItem");
            }

            if (newLabel is not null)
            {
                newLabel.Classes.Remove("menuItem");
                if (!newLabel.Classes.Contains("menuActive"))
                    newLabel.Classes.Add("menuActive");
            }
        }

        private void OnAutoUpdateRequested()
        {
            Dispatcher.UIThread.Post(async () =>
            {
                var jobManager = SP.GetService<JobManagerService>();
                if (jobManager.Jobs.Any(j => j.Status == JobStatus.Running))
                    return;

                var dialog = new MainDialog(
                    new AutoUpdateDialog(updateService.CurrentVersion, updateService.RemoteVersion),
                    "Auto Update");
                await dialog.ShowDialog(this);
            });
        }

        private void OpenGitHub(object sender, RoutedEventArgs e)
            => Url.Open("https://github.com/eros1sh");

        private void OpenTelegram(object sender, RoutedEventArgs e)
            => Url.Open("https://t.me/eros_sh");

        #region Dropdown submenu logic
        private void ConfigSubmenuMouseEnter(object sender, PointerEventArgs e)
        {
            if (vm.IsConfigSelected)
            {
                hoveringConfigSubmenu = true;
                configSubmenu.IsVisible = true;
            }
        }

        private async void ConfigSubmenuMouseLeave(object sender, PointerEventArgs e)
        {
            hoveringConfigSubmenu = false;
            await CheckCloseSubmenuAsync();
        }

        private void ConfigsMenuOptionMouseEnter(object sender, PointerEventArgs e)
        {
            if (vm.IsConfigSelected)
            {
                hoveringConfigsMenuOption = true;
                configSubmenu.IsVisible = true;
            }
        }

        private async void ConfigsMenuOptionMouseLeave(object sender, PointerEventArgs e)
        {
            hoveringConfigsMenuOption = false;
            await CheckCloseSubmenuAsync();
        }

        private async Task CheckCloseSubmenuAsync()
        {
            await Task.Delay(50);

            if (!hoveringConfigSubmenu && !hoveringConfigsMenuOption)
            {
                configSubmenu.IsVisible = false;
            }
        }

        private void CloseSubmenu() => configSubmenu.IsVisible = false;
        #endregion

        public void SetTheme(CustomizationSettings customization)
        {
            Brush.SetAppColor("BackgroundMain", customization.BackgroundMain);
            Brush.SetAppColor("BackgroundSecondary", customization.BackgroundSecondary);
            Brush.SetAppColor("BackgroundInput", customization.BackgroundInput);
            Brush.SetAppColor("ForegroundMain", customization.ForegroundMain);
            Brush.SetAppColor("ForegroundInput", customization.ForegroundInput);
            Brush.SetAppColor("ForegroundGood", customization.ForegroundGood);
            Brush.SetAppColor("ForegroundBad", customization.ForegroundBad);
            Brush.SetAppColor("ForegroundCustom", customization.ForegroundCustom);
            Brush.SetAppColor("ForegroundRetry", customization.ForegroundRetry);
            Brush.SetAppColor("ForegroundBanned", customization.ForegroundBanned);
            Brush.SetAppColor("ForegroundToCheck", customization.ForegroundToCheck);
            Brush.SetAppColor("ForegroundMenuSelected", customization.ForegroundMenuSelected);
            Brush.SetAppColor("SuccessButton", customization.SuccessButton);
            Brush.SetAppColor("PrimaryButton", customization.PrimaryButton);
            Brush.SetAppColor("WarningButton", customization.WarningButton);
            Brush.SetAppColor("DangerButton", customization.DangerButton);
            Brush.SetAppColor("ForegroundButton", customization.ForegroundButton);
            Brush.SetAppColor("BackgroundButton", customization.BackgroundButton);

            if (File.Exists(customization.BackgroundImagePath))
            {
                // Background image support in Avalonia
                var bitmap = new global::Avalonia.Media.Imaging.Bitmap(customization.BackgroundImagePath);
                var imageBrush = new global::Avalonia.Media.ImageBrush(bitmap)
                {
                    Opacity = customization.BackgroundOpacity / 100,
                    Stretch = global::Avalonia.Media.Stretch.UniformToFill
                };
                Background = imageBrush;
            }
            else
            {
                Background = Brush.Get("BackgroundMain");
            }
        }

        private async void OnWindowClosing(object sender, WindowClosingEventArgs e)
        {
            if (forceClose)
                return;

            var obSettingsService = SP.GetService<ProjectBulletSettingsService>();
            var jobManagerService = SP.GetService<JobManagerService>();
            var config = SP.GetService<ConfigService>().SelectedConfig;

            var needsConfigWarning = obSettingsService.Settings.GeneralSettings.WarnConfigNotSaved
                && config != null && config.HasUnsavedChanges();
            var needsJobWarning = jobManagerService.Jobs.Any(j => j.Status != JobStatus.Idle);

            if (!needsConfigWarning && !needsJobWarning)
                return;

            // Cancel the close and show async dialog
            e.Cancel = true;

            if (needsConfigWarning)
            {
                var result = await Alert.ChoiceAsync("Config not saved",
                    $"The config you are editing ({config.Metadata.Name}) has unsaved changes, are you sure you want to quit?");
                if (!result) return;
            }

            if (needsJobWarning)
            {
                var result = await Alert.ChoiceAsync("Job(s) running",
                    "One or more jobs are still running, are you sure you want to quit?");
                if (!result) return;
            }

            // User confirmed, force close
            forceClose = true;
            Close();
        }
    }

    public class MainWindowViewModel : ViewModelBase
    {
        private readonly ProjectBulletSettingsService obSettingsService;
        private readonly JobManagerService jobManagerService;
        private readonly ConfigService configService;
        public event Action<Config> ConfigSelected;
        public Config Config => configService.SelectedConfig;

        public bool IsConfigSelected => Config != null;

        public MainWindowViewModel()
        {
            obSettingsService = SP.GetService<ProjectBulletSettingsService>();
            jobManagerService = SP.GetService<JobManagerService>();
            configService = SP.GetService<ConfigService>();
            configService.OnConfigSelected += (sender, config) =>
            {
                OnPropertyChanged(nameof(IsConfigSelected));
                ConfigSelected?.Invoke(config);
            };
        }
    }

    public enum MainWindowPage
    {
        Home,
        Jobs,
        Monitor,
        Proxies,
        Wordlists,
        Configs,
        ConfigMetadata,
        ConfigReadme,
        ConfigStacker,
        ConfigLoliCode,
        ConfigSettings,
        ConfigCSharpCode,
        ConfigLoliScript,
        Hits,
        Plugins,
        Marketplace,
        OBSettings,
        RLSettings,
        About
    }
}
