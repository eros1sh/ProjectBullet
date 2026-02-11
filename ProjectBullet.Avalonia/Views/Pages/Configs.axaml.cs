using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using ProjectBullet.Core.Models.Settings;
using ProjectBullet.Core.Repositories;
using ProjectBullet.Core.Services;
using ProjectBullet.Avalonia.DTOs;
using ProjectBullet.Avalonia.Helpers;
using ProjectBullet.Avalonia.Services;
using ProjectBullet.Avalonia.ViewModels;
using ProjectBullet.Avalonia.Views.Dialogs;
using RuriLib.Helpers;
using RuriLib.Models.Configs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace ProjectBullet.Avalonia.Views.Pages
{
    /// <summary>
    /// Interaction logic for Configs.axaml
    /// </summary>
    public partial class Configs : UserControl
    {
        private readonly ProjectBulletSettingsService obSettingsService;
        private readonly ConfigService configService;
        private readonly ConfigsViewModel vm;
        private readonly VolatileSettingsService volatileSettings;

        private ConfigViewModel HoveredItem => configsDataGrid.SelectedItem as ConfigViewModel;

        public Configs()
        {
            obSettingsService = SP.GetService<ProjectBulletSettingsService>();
            configService = SP.GetService<ConfigService>();
            volatileSettings = SP.GetService<VolatileSettingsService>();
            vm = SP.GetService<ViewModelsService>().Configs;
            DataContext = vm;

            InitializeComponent();
        }

        // This is needed otherwise if properties of a config are updated by another page this page will not
        // get notified and will show the old values.
        public void UpdateViewModel()
        {
            vm.SelectedConfig?.UpdateViewModel();
        }

        private async void Create(object sender, RoutedEventArgs e)
            => await new MainDialog(new CreateConfigDialog(this), "Create config").ShowDialog(TopLevel.GetTopLevel(this) as Window);

        public async void CreateConfig(ConfigForCreationDto dto) => await vm.CreateAsync(dto);

        private void Edit(object sender, RoutedEventArgs e) => EditConfig();

        private async void Save(object sender, RoutedEventArgs e)
        {
            if (vm.SelectedConfig is null)
            {
                ShowNoConfigSelectedError();
                return;
            }

            try
            {
                await vm.Save(vm.SelectedConfig);
                Alert.Success("Success", $"{vm.SelectedConfig.Config.Metadata.Name} was saved successfully!");
            }
            catch (Exception ex)
            {
                Alert.Exception(ex);
            }
        }

        private async void Delete(object sender, RoutedEventArgs e)
        {
            if (HoveredItem is null)
            {
                ShowNoConfigSelectedError();
                return;
            }

            if (await Alert.ChoiceAsync("Are you sure?", $"Do you really want to delete {HoveredItem.Name}"))
            {
                vm.Delete(HoveredItem);
            }
        }

        // TODO: Check if current config is not saved and prompt warning
        private async void Rescan(object sender, RoutedEventArgs e) => await vm.RescanAsync();

        private void OpenFolder(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = Path.Combine(Directory.GetCurrentDirectory(), "UserData", "Configs"),
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                // This happens on access denied
                Alert.Exception(ex);
            }
        }

        private void ShowNoConfigSelectedError() => Alert.Error("No config selected", "Please select a config first!");

        private void NavigateToConfigSection()
        {
            var mode = vm.SelectedConfig.Config.Mode;
            var page = obSettingsService.Settings.GeneralSettings.ConfigSectionOnLoad switch
            {
                ConfigSection.Metadata => MainWindowPage.ConfigMetadata,
                ConfigSection.Readme => MainWindowPage.ConfigReadme,
                ConfigSection.Stacker => mode switch
                {
                    ConfigMode.LoliCode or ConfigMode.Stack => MainWindowPage.ConfigStacker,
                    ConfigMode.CSharp => MainWindowPage.ConfigCSharpCode,
                    ConfigMode.Legacy => MainWindowPage.ConfigLoliScript,
                    _ => MainWindowPage.ConfigMetadata
                },
                ConfigSection.LoliCode => mode switch
                {
                    ConfigMode.LoliCode or ConfigMode.Stack => MainWindowPage.ConfigLoliCode,
                    ConfigMode.CSharp => MainWindowPage.ConfigCSharpCode,
                    ConfigMode.Legacy => MainWindowPage.ConfigLoliScript,
                    _ => MainWindowPage.ConfigMetadata
                },
                ConfigSection.Settings => MainWindowPage.ConfigSettings,
                ConfigSection.CSharpCode => mode switch
                {
                    ConfigMode.LoliCode or ConfigMode.Stack or ConfigMode.CSharp  => MainWindowPage.ConfigLoliCode,
                    ConfigMode.Legacy => MainWindowPage.ConfigLoliScript,
                    _ => MainWindowPage.ConfigMetadata
                },
                ConfigSection.LoliScript => mode switch
                {
                    ConfigMode.LoliCode or ConfigMode.Stack => MainWindowPage.ConfigLoliCode,
                    ConfigMode.CSharp => MainWindowPage.ConfigCSharpCode,
                    ConfigMode.Legacy => MainWindowPage.ConfigLoliScript,
                    _ => MainWindowPage.ConfigMetadata
                },
                _ => throw new NotImplementedException(),
            };

            SP.GetService<MainWindow>().NavigateTo(page);
        }

        private void UpdateSearch(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                vm.SearchString = filterTextbox.Text;
            }
        }

        private void Search(object sender, RoutedEventArgs e) => vm.SearchString = filterTextbox.Text;

        private void ItemHovered(object sender, SelectionChangedEventArgs e)
        {
            if (configsDataGrid.SelectedItem is ConfigViewModel cvm)
            {
                vm.HoveredConfig = cvm;
            }
        }

        private void ListItemDoubleClick(object sender, TappedEventArgs e) => EditConfig();

        private async void EditConfig()
        {
            if (HoveredItem is null)
            {
                ShowNoConfigSelectedError();
                return;
            }

            if (HoveredItem.Config.IsRemote)
            {
                Alert.Error("Remote", "You cannot edit remote configs!");
                return;
            }

            // Check if the config was saved
            if (obSettingsService.Settings.GeneralSettings.WarnConfigNotSaved
                && configService.SelectedConfig != null
                && configService.SelectedConfig.HasUnsavedChanges()
                && !await Alert.ChoiceAsync("Config not saved", $"The currently selected config ({configService.SelectedConfig.Metadata.Name}) has unsaved changes," +
                    $" are you sure you want to edit another config?"))
            {
                return;
            }

            vm.SelectedConfig = HoveredItem;
            SP.GetService<ViewModelsService>().Debugger.ClearLog();
            NavigateToConfigSection();
        }

        private async void ExportEncrypted(object sender, RoutedEventArgs e)
        {
            if (vm.SelectedConfig is null)
            {
                ShowNoConfigSelectedError();
                return;
            }

            var config = vm.SelectedConfig.Config;
            await new MainDialog(new PasswordDialog("Enter password to encrypt config:", password =>
            {
                ExportEncryptedAsync(config, password);
            }), "Export Encrypted").ShowDialog(TopLevel.GetTopLevel(this) as Window);
        }

        private async void ExportEncryptedAsync(Config config, string password)
        {
            try
            {
                var topLevel = TopLevel.GetTopLevel(this);
                var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
                {
                    Title = "Export Encrypted Config",
                    DefaultExtension = "pbc",
                    SuggestedFileName = $"{config.Metadata.Name}.pbc",
                    FileTypeChoices = new[]
                    {
                        new FilePickerFileType("Encrypted Config") { Patterns = new[] { "*.pbc" } }
                    }
                });

                if (file is not null)
                {
                    var data = await EncryptedConfigPacker.PackEncryptedAsync(config, password);
                    await using var stream = await file.OpenWriteAsync();
                    await stream.WriteAsync(data);
                    Alert.Success("Export complete", $"Config exported successfully");
                }
            }
            catch (Exception ex)
            {
                Alert.Exception(ex);
            }
        }

        private async void ImportEncrypted(object sender, RoutedEventArgs e)
        {
            var topLevel = TopLevel.GetTopLevel(this);
            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Import Encrypted Config",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("Encrypted Config") { Patterns = new[] { "*.pbc" } }
                }
            });

            if (files.Count > 0)
            {
                var filePath = files[0].Path.LocalPath;
                await new MainDialog(new PasswordDialog("Enter password to decrypt config:", password =>
                {
                    ImportEncryptedAsync(filePath, password);
                }), "Import Encrypted").ShowDialog(TopLevel.GetTopLevel(this) as Window);
            }
        }

        private async void ImportEncryptedAsync(string filePath, string password)
        {
            try
            {
                var data = await File.ReadAllBytesAsync(filePath);
                var config = await EncryptedConfigPacker.UnpackEncryptedAsync(data, password);

                var configRepo = SP.GetService<IConfigRepository>();
                await configRepo.SaveAsync(config);
                configService.Configs.Add(config);
                await vm.RescanAsync();

                Alert.Success("Import complete", $"Config '{config.Metadata.Name}' imported successfully!");
            }
            catch (Exception ex)
            {
                Alert.Exception(ex);
            }
        }

        private void ToggleFavorite(object sender, PointerPressedEventArgs e)
        {
            if (sender is TextBlock tb && tb.DataContext is ConfigViewModel cvm)
            {
                cvm.ToggleFavorite();
            }
        }
    }
}
