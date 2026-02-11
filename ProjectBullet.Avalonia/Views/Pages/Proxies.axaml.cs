using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using ProjectBullet.Core.Entities;
using ProjectBullet.Avalonia.DTOs;
using ProjectBullet.Avalonia.Extensions;
using ProjectBullet.Avalonia.Helpers;
using ProjectBullet.Avalonia.Services;
using ProjectBullet.Avalonia.ViewModels;
using ProjectBullet.Avalonia.Views.Dialogs;
using RuriLib.Models.Proxies;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ProjectBullet.Avalonia.Views.Pages
{
    /// <summary>
    /// Interaction logic for Proxies.axaml
    /// </summary>
    public partial class Proxies : UserControl
    {
        private readonly ProxiesViewModel vm;

        private IEnumerable<ProxyEntity> SelectedProxies => proxiesListView.SelectedItems.Cast<ProxyEntity>().ToList();

        public Proxies()
        {
            vm = SP.GetService<ViewModelsService>().Proxies;
            DataContext = vm;
            _ = vm.InitializeAsync();

            InitializeComponent();
        }

        private async void AddGroup(object sender, RoutedEventArgs e)
            => await new MainDialog(new AddProxyGroupDialog(this), "Add proxy group").ShowDialog(TopLevel.GetTopLevel(this) as Window);

        private async void EditGroup(object sender, RoutedEventArgs e)
        {
            if (!vm.GroupIsValid)
            {
                ShowInvalidGroupError();
                return;
            }

            await new MainDialog(new AddProxyGroupDialog(this, vm.SelectedGroup), "Edit proxy group").ShowDialog(TopLevel.GetTopLevel(this) as Window);
        }

        private async void DeleteGroup(object sender, RoutedEventArgs e)
        {
            if (!vm.GroupIsValid)
            {
                ShowInvalidGroupError();
                return;
            }

            try
            {
                await vm.DeleteSelectedGroupAsync();
            }
            catch (Exception ex)
            {
                Alert.Exception(ex);
            }
        }

        private async void DeleteNotWorking(object sender, RoutedEventArgs e)
        {
            try
            {
                await vm.DeleteNotWorkingAsync();
                Alert.Success("Done", "Successfully deleted the not working proxies from the group");
            }
            catch (Exception ex)
            {
                Alert.Exception(ex);
            }
        }

        private async void DeleteUntested(object sender, RoutedEventArgs e)
        {
            try
            {
                await vm.DeleteUntestedAsync();
                Alert.Success("Done", "Successfully deleted the untested proxies from the group");
            }
            catch (Exception ex)
            {
                Alert.Exception(ex);
            }
        }

        private async void Import(object sender, RoutedEventArgs e)
        {
            if (!vm.GroupIsValid)
            {
                ShowInvalidGroupError();
                return;
            }

            await new MainDialog(new ImportProxiesDialog(this), "Import proxies").ShowDialog(TopLevel.GetTopLevel(this) as Window);
        }

        public async void AddGroup(ProxyGroupEntity entity)
        {
            try
            {
                await vm.AddGroupAsync(entity);
            }
            catch (Exception ex)
            {
                Alert.Exception(ex);
            }
        }
        public async void EditGroup(ProxyGroupEntity entity)
        {
            try
            {
                await vm.EditGroupAsync(entity);
            }
            catch (Exception ex)
            {
                Alert.Exception(ex);
            }
        }

        public void UpdateViewModel() => vm.UpdateViewModel();

        private async void ExportSelected(object sender, RoutedEventArgs e)
        {
            var topLevel = TopLevel.GetTopLevel(this);
            var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Export proxies",
                FileTypeChoices = new[]
                {
                    new FilePickerFileType("Text File") { Patterns = new[] { "*.txt" } }
                }
            });

            if (file is not null)
            {
                var path = file.Path.LocalPath;
                if (SelectedProxies.Any())
                {
                    SelectedProxies.SaveToFile(path, p => p.ToString());
                }
                else
                {
                    Alert.Error("Uh-oh", "No proxies selected");
                }
            }
        }

        private void CopySelectedProxies(object sender, RoutedEventArgs e)
            => SelectedProxies.CopyToClipboard(p => $"{p.Host}:{p.Port}");

        private void CopySelectedProxiesFull(object sender, RoutedEventArgs e)
            => SelectedProxies.CopyToClipboard(p => p.ToString());

        public async void AddProxies(ProxiesForImportDto dto)
        {
            try
            {
                await vm.AddProxiesAsync(dto);
            }
            catch (Exception ex)
            {
                Alert.Exception(ex);
            }
        }

        private async void DeleteSelected(object sender, RoutedEventArgs e)
        {
            try
            {
                await vm.DeleteAsync(SelectedProxies);
                Alert.Success("Done", "Successfully deleted the selected proxies from the group");
            }
            catch (Exception ex)
            {
                Alert.Exception(ex);
            }
        }

        private void ShowInvalidGroupError()
            => Alert.Error("Invalid group", "Please select or create a valid group first!");
    }
}
