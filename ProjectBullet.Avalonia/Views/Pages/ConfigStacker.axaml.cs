using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using ProjectBullet.Core.Repositories;
using ProjectBullet.Core.Services;
using ProjectBullet.Avalonia.Controls;
using ProjectBullet.Avalonia.Helpers;
using ProjectBullet.Avalonia.Services;
using ProjectBullet.Avalonia.ViewModels;
using ProjectBullet.Avalonia.Views.Dialogs;
using RuriLib.Models.Blocks;
using RuriLib.Models.Blocks.Custom;
using RuriLib.Models.Configs;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ProjectBullet.Avalonia.Views.Pages
{
    /// <summary>
    /// Interaction logic for ConfigStacker.axaml
    /// </summary>
    public partial class ConfigStacker : UserControl
    {
        private readonly ConfigService configService;
        private readonly IConfigRepository configRepo;
        private readonly ConfigStackerViewModel vm;

        public ConfigStacker()
        {
            configService = SP.GetService<ConfigService>();
            configRepo = SP.GetService<IConfigRepository>();
            vm = SP.GetService<ViewModelsService>().ConfigStacker;
            vm.SelectionChanged += SelectionChanged;
            DataContext = vm;

            InitializeComponent();
        }

        public void UpdateViewModel()
        {
            try
            {
                // Try to change the mode to Stack
                configService.SelectedConfig.ChangeMode(ConfigMode.Stack);
            }
            catch (Exception ex)
            {
                // On fail, prompt it to the user and go back to the configs page
                Alert.Exception(ex);
                SP.GetService<MainWindow>().NavigateTo(MainWindowPage.Configs);
            }

            vm.SelectBlock(null, false);
            vm.UpdateViewModel();
        }

        public void CreateBlock(BlockDescriptor descriptor) => vm.CreateBlock(descriptor);

        private async void AddBlock(object sender, RoutedEventArgs e)
            => await new MainDialog(new AddBlockDialog(this), "Add block").ShowDialog(TopLevel.GetTopLevel(this) as Window);

        private void RemoveBlock(object sender, RoutedEventArgs e) => vm.RemoveSelected();
        private void MoveBlockUp(object sender, RoutedEventArgs e) => vm.MoveSelectedUp();
        private void MoveBlockDown(object sender, RoutedEventArgs e) => vm.MoveSelectedDown();
        private void CloneBlock(object sender, RoutedEventArgs e) => vm.CloneSelected();
        private void EnableDisableBlock(object sender, RoutedEventArgs e) => vm.EnableDisableSelected();
        private void Undo(object sender, RoutedEventArgs e) => vm.Undo();

        private void SelectBlockPointer(object sender, PointerPressedEventArgs e) => SelectBlock(sender);
        private void SelectBlock(object sender, RoutedEventArgs e) => SelectBlock(sender);
        private void SelectBlock(object sender)
        {
            var keyModifiers = KeyModifiers.None;
            // In Avalonia we check via the visual tree or TopLevel
            // For simplicity, we pass false for ctrl/shift here
            var block = (BlockViewModel)(sender as Control).Tag;
            vm.SelectBlock(block, false, false);
        }

        private void SelectionChanged(IEnumerable<BlockViewModel> selected)
        {
            var first = selected.FirstOrDefault();

            if (first is null)
            {
                blockInfo.Content = null;
            }
            else
            {
                UserControl content = first.Block switch
                {
                    AutoBlockInstance => new AutoBlockSettingsViewer(first),
                    ParseBlockInstance => new ParseBlockSettingsViewer(first),
                    ScriptBlockInstance => new ScriptBlockSettingsViewer(first),
                    HttpRequestBlockInstance => new HttpRequestBlockSettingsViewer(first),
                    KeycheckBlockInstance => new KeycheckBlockSettingsViewer(first),
                    LoliCodeBlockInstance => new LoliCodeBlockSettingsViewer(first),
                    _ => null
                };

                blockInfo.Content = content;
            }
        }

        private async void PageKeyDown(object sender, KeyEventArgs e)
        {
            // Save on CTRL+S
            if (e.KeyModifiers.HasFlag(KeyModifiers.Control) && e.Key == Key.S)
            {
                await configRepo.SaveAsync(configService.SelectedConfig);
                Alert.Success("Saved", $"{configService.SelectedConfig.Metadata.Name} was saved successfully!");
            }
        }
    }
}
