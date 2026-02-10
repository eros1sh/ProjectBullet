using Microsoft.Win32;
using ProjectBullet.Core.Models.Settings;
using ProjectBullet.Core.Repositories;
using ProjectBullet.Core.Services;
using ProjectBullet.Native.DTOs;
using ProjectBullet.Native.Helpers;
using ProjectBullet.Native.Services;
using ProjectBullet.Native.ViewModels;
using ProjectBullet.Native.Views.Dialogs;
using RuriLib.Helpers;
using RuriLib.Models.Configs;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;

namespace ProjectBullet.Native.Views.Pages
{
    /// <summary>
    /// Interaction logic for Configs.xaml
    /// </summary>
    public partial class Configs : Page
    {
        private readonly ProjectBulletSettingsService obSettingsService;
        private readonly ConfigService configService;
        private readonly ConfigsViewModel vm;
        private readonly VolatileSettingsService volatileSettings;
        private GridViewColumnHeader listViewSortCol;
        private SortAdorner listViewSortAdorner;

        private ConfigViewModel HoveredItem => (ConfigViewModel)configsListView.SelectedItem;
        
        private string ListViewSortBy
        {
            get => volatileSettings.ListViewSorting["configs"].By;
            set => volatileSettings.ListViewSorting["configs"].By = value;
        }
        
        private ListSortDirection ListViewSortDir
        {
            get => volatileSettings.ListViewSorting["configs"].Direction;
            set => volatileSettings.ListViewSorting["configs"].Direction = value;
        }

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

            if (!string.IsNullOrEmpty(ListViewSortBy))
            {
                configsListView.Items.SortDescriptions.Add(new SortDescription(ListViewSortBy, ListViewSortDir));
            }
        }

        private void Create(object sender, RoutedEventArgs e)
            => new MainDialog(new CreateConfigDialog(this), "Create config").ShowDialog();

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

        private void Delete(object sender, RoutedEventArgs e)
        {
            if (HoveredItem is null)
            {
                ShowNoConfigSelectedError();
                return;
            }

            if (Alert.Choice("Are you sure?", $"Do you really want to delete {HoveredItem.Name}"))
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
                Process.Start("explorer.exe", Path.Combine(Directory.GetCurrentDirectory(), "UserData\\Configs"));
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
            var items = e.AddedItems as IList<object>;

            if (items.Count == 1)
            {
                vm.HoveredConfig = items[0] as ConfigViewModel;
            }
        }

        private void ListItemDoubleClick(object sender, MouseButtonEventArgs e) => EditConfig();

        private void EditConfig()
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
                && !Alert.Choice("Config not saved", $"The currently selected config ({configService.SelectedConfig.Metadata.Name}) has unsaved changes," +
                    $" are you sure you want to edit another config?"))
            {
                return;
            }

            vm.SelectedConfig = HoveredItem;
            SP.GetService<ViewModelsService>().Debugger.ClearLog();
            NavigateToConfigSection();
        }

        private void ExportEncrypted(object sender, RoutedEventArgs e)
        {
            if (vm.SelectedConfig is null)
            {
                ShowNoConfigSelectedError();
                return;
            }

            var config = vm.SelectedConfig.Config;
            new MainDialog(new PasswordDialog("Enter password to encrypt config:", password =>
            {
                ExportEncryptedAsync(config, password);
            }), "Export Encrypted").ShowDialog();
        }

        private async void ExportEncryptedAsync(Config config, string password)
        {
            try
            {
                var sfd = new SaveFileDialog
                {
                    Filter = "Encrypted Config (*.pbc)|*.pbc",
                    FileName = $"{config.Metadata.Name}.pbc"
                };

                if (sfd.ShowDialog() == true)
                {
                    var data = await EncryptedConfigPacker.PackEncryptedAsync(config, password);
                    await File.WriteAllBytesAsync(sfd.FileName, data);
                    Alert.Success("Export complete", $"Config exported to {sfd.FileName}");
                }
            }
            catch (Exception ex)
            {
                Alert.Exception(ex);
            }
        }

        private void ImportEncrypted(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog
            {
                Filter = "Encrypted Config (*.pbc)|*.pbc"
            };

            if (ofd.ShowDialog() == true)
            {
                var filePath = ofd.FileName;
                new MainDialog(new PasswordDialog("Enter password to decrypt config:", password =>
                {
                    ImportEncryptedAsync(filePath, password);
                }), "Import Encrypted").ShowDialog();
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

        private void ToggleFavorite(object sender, MouseButtonEventArgs e)
        {
            if (sender is System.Windows.Controls.TextBlock tb && tb.DataContext is ConfigViewModel cvm)
            {
                cvm.ToggleFavorite();

                // Refresh the view to re-sort favorites
                var view = System.Windows.Data.CollectionViewSource.GetDefaultView(vm.ConfigsCollection);
                view?.Refresh();
            }
        }

        private async void ConfigListViewDrop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;

            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            foreach (var file in files)
            {
                try
                {
                    var ext = Path.GetExtension(file).ToLower();
                    if (ext == ".opk")
                    {
                        using var stream = File.OpenRead(file);
                        var config = await ConfigPacker.UnpackAsync(stream);
                        var configRepo = SP.GetService<IConfigRepository>();
                        await configRepo.SaveAsync(config);
                        configService.Configs.Add(config);
                    }
                    else if (ext == ".pbc")
                    {
                        var filePath = file;
                        new MainDialog(new PasswordDialog("Enter password to decrypt config:", password =>
                        {
                            ImportEncryptedAsync(filePath, password);
                        }), "Import Encrypted").ShowDialog();
                    }
                }
                catch (Exception ex)
                {
                    Alert.Exception(ex);
                }
            }

            await vm.RescanAsync();
        }

        private void ColumnHeaderClicked(object sender, RoutedEventArgs e)
        {
            var column = sender as GridViewColumnHeader;
            ListViewSortBy = column.Tag.ToString();

            if (listViewSortCol != null)
            {
                AdornerLayer.GetAdornerLayer(listViewSortCol).Remove(listViewSortAdorner);
                configsListView.Items.SortDescriptions.Clear();
            }

            ListViewSortDir = ListSortDirection.Ascending;

            if (listViewSortCol == column && listViewSortAdorner.Direction == ListViewSortDir)
            {
                ListViewSortDir = ListSortDirection.Descending;
            }

            listViewSortCol = column;
            listViewSortAdorner = new SortAdorner(listViewSortCol, ListViewSortDir);
            AdornerLayer.GetAdornerLayer(listViewSortCol).Add(listViewSortAdorner);
            configsListView.Items.SortDescriptions.Add(new SortDescription(ListViewSortBy, ListViewSortDir));
        }
    }
}
