using Avalonia.Controls;
using Avalonia.Interactivity;
using ProjectBullet.Avalonia.Services;
using ProjectBullet.Avalonia.ViewModels;
using ProjectBullet.Avalonia.Views.Dialogs;
using RuriLib.Models.Configs.Settings;
using RuriLib.Models.Data.Resources.Options;
using RuriLib.Models.Data.Rules;
using System;
using System.Linq;

namespace ProjectBullet.Avalonia.Views.Pages
{
    /// <summary>
    /// Interaction logic for ConfigSettings.axaml
    /// </summary>
    public partial class ConfigSettings : UserControl
    {
        private readonly ConfigSettingsViewModel vm;

        public ConfigSettings()
        {
            vm = SP.GetService<ViewModelsService>().ConfigSettings;
            DataContext = vm;

            InitializeComponent();
            SetMultiLineTextBoxContents();
        }

        public void UpdateViewModel() => vm.UpdateViewModel();

        private void BlockedUrlsChanged(object sender, TextChangedEventArgs e)
            => vm.BlockedUrls = blockedUrlsTextBox.Text.Split(Environment.NewLine).ToList();

        private void AddCustomInput(object sender, RoutedEventArgs e) => vm.AddCustomInput();
        private void RemoveCustomInput(object sender, RoutedEventArgs e)
            => vm.RemoveCustomInput((CustomInput)(sender as Button).Tag);

        private void AddLinesFromFileResource(object sender, RoutedEventArgs e) => vm.AddLinesFromFileResource();
        private void AddRandomLinesFromFileResource(object sender, RoutedEventArgs e) => vm.AddRandomLinesFromFileResource();
        private void RemoveResource(object sender, RoutedEventArgs e)
            => vm.RemoveResource((ConfigResourceOptions)(sender as Button).Tag);

        private void AddSimpleDataRule(object sender, RoutedEventArgs e) => vm.AddSimpleDataRule();
        private void AddRegexDataRule(object sender, RoutedEventArgs e) => vm.AddRegexDataRule();
        private void RemoveDataRule(object sender, RoutedEventArgs e)
            => vm.RemoveDataRule((DataRule)(sender as Button).Tag);

        private void SetMultiLineTextBoxContents()
        {
            blockedUrlsTextBox.Text = string.Join(Environment.NewLine, vm.BlockedUrls);
        }

        private async void TestDataRules(object sender, RoutedEventArgs e)
            => await new MainDialog(new TestDataRulesDialog(vm.TestDataForRules, vm.TestWordlistTypeForRules, vm.DataRulesCollection), "Test Results").ShowDialog(TopLevel.GetTopLevel(this) as Window);
    }
}
