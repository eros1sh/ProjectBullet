using ProjectBullet.Core.Services;
using ProjectBullet.Avalonia.Helpers;
using ProjectBullet.Avalonia.Services;
using ProjectBullet.Avalonia.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace ProjectBullet.Avalonia.Views.Dialogs
{
    public partial class SelectConfigDialog : UserControl
    {
        private readonly object caller;
        private readonly SelectConfigDialogViewModel vm;

        public SelectConfigDialog()
        {
            InitializeComponent();
        }

        public SelectConfigDialog(object caller)
        {
            this.caller = caller;

            vm = new SelectConfigDialogViewModel();
            DataContext = vm;

            InitializeComponent();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                vm.SearchString = filterTextbox.Text;
            }
            base.OnKeyDown(e);
        }

        private void Search(object sender, RoutedEventArgs e) => vm.SearchString = filterTextbox.Text;

        private void ItemHovered(object sender, SelectionChangedEventArgs e)
        {
            if (configsDataGrid.SelectedItem is ConfigViewModel config)
            {
                vm.HoveredConfig = config;
            }
        }

        private void ListItemDoubleClick(object sender, TappedEventArgs e) => ConfirmSelection();
        private void Accept(object sender, RoutedEventArgs e) => ConfirmSelection();

        private void ConfirmSelection()
        {
            if (vm.HoveredConfig is null)
            {
                ShowNoConfigSelectedError();
                return;
            }

            if (caller is MultiRunJobOptionsDialog page)
            {
                page.SelectConfig(vm.HoveredConfig);
            }

            if (this.VisualRoot is MainDialog dialog) dialog.Close();
        }

        private void ShowNoConfigSelectedError() => Alert.Error("No config selected", "Please select a config first!");
    }

    public class SelectConfigDialogViewModel : ViewModelBase
    {
        private readonly ConfigsViewModel configsViewModel;
        private readonly ConfigService configService;

        private ObservableCollection<ConfigViewModel> configsCollection;
        public ObservableCollection<ConfigViewModel> ConfigsCollection
        {
            get => configsCollection;
            set
            {
                configsCollection = value;
                OnPropertyChanged();
            }
        }

        private string searchString = string.Empty;
        public string SearchString
        {
            get => searchString;
            set
            {
                searchString = value;

                if (configsViewModel is not null)
                {
                    configsViewModel.SearchString = value;
                }

                OnPropertyChanged();
                FilterCollection();
            }
        }

        private ConfigViewModel hoveredConfig;
        public ConfigViewModel HoveredConfig
        {
            get => hoveredConfig;
            set
            {
                hoveredConfig = value;

                OnPropertyChanged();
                OnPropertyChanged(nameof(IsConfigHovered));
            }
        }

        public bool IsConfigHovered => HoveredConfig != null;

        public SelectConfigDialogViewModel()
        {
            configService = SP.GetService<ConfigService>();
            CreateCollection();

            configsViewModel = SP.GetService<ViewModelsService>().Configs;

            if (configsViewModel is not null)
            {
                SearchString = configsViewModel.SearchString;
            }
        }

        private void CreateCollection()
        {
            var viewModels = configService.Configs.Select(c => new ConfigViewModel(c));
            allConfigs = viewModels.ToList();
            ConfigsCollection = new ObservableCollection<ConfigViewModel>(allConfigs);
        }

        private List<ConfigViewModel> allConfigs;

        private void FilterCollection()
        {
            if (allConfigs == null) return;

            var filtered = allConfigs.Where(c => c.Config.Metadata.Name.Contains(searchString, StringComparison.OrdinalIgnoreCase));
            ConfigsCollection = new ObservableCollection<ConfigViewModel>(filtered);
        }
    }
}
