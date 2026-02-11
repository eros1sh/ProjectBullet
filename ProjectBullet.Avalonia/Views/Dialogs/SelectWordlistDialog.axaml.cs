using ProjectBullet.Core.Entities;
using ProjectBullet.Core.Repositories;
using ProjectBullet.Avalonia.Helpers;
using ProjectBullet.Avalonia.Services;
using ProjectBullet.Avalonia.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace ProjectBullet.Avalonia.Views.Dialogs
{
    public partial class SelectWordlistDialog : UserControl
    {
        private readonly object caller;
        private readonly SelectWordlistDialogViewModel vm;

        public SelectWordlistDialog()
        {
            InitializeComponent();
        }

        public SelectWordlistDialog(object caller)
        {
            this.caller = caller;

            vm = new SelectWordlistDialogViewModel();
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
            if (wordlistDataGrid.SelectedItem is WordlistEntity entity)
            {
                vm.HoveredWordlist = entity;
            }
        }

        private void ListItemDoubleClick(object sender, TappedEventArgs e) => ConfirmSelection();
        private void Accept(object sender, RoutedEventArgs e) => ConfirmSelection();

        private void ConfirmSelection()
        {
            if (vm.HoveredWordlist is null)
            {
                ShowNoWordlistSelectedError();
                return;
            }

            if (caller is MultiRunJobOptionsDialog page)
            {
                page.SelectWordlist(vm.HoveredWordlist);
            }

            if (this.VisualRoot is MainDialog dialog) dialog.Close();
        }

        private void ShowNoWordlistSelectedError() => Alert.Error("No wordlist selected", "Please select a wordlist first!");
    }

    public class SelectWordlistDialogViewModel : ViewModelBase
    {
        private readonly WordlistsViewModel wordlistsViewModel;
        private readonly IWordlistRepository wordlistRepo;

        private ObservableCollection<WordlistEntity> wordlistsCollection;
        public ObservableCollection<WordlistEntity> WordlistsCollection
        {
            get => wordlistsCollection;
            set
            {
                wordlistsCollection = value;
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

                if (wordlistsViewModel is not null)
                {
                    wordlistsViewModel.SearchString = value;
                }

                OnPropertyChanged();
                FilterCollection();
            }
        }

        private WordlistEntity hoveredWordlist;
        public WordlistEntity HoveredWordlist
        {
            get => hoveredWordlist;
            set
            {
                hoveredWordlist = value;
                OnPropertyChanged();

                try
                {
                    LinesPreview = string.Join(Environment.NewLine, File.ReadLines(hoveredWordlist.FileName).Take(10));
                }
                catch
                {
                    LinesPreview = "Could not load the preview...";
                }
            }
        }

        private string linesPreview = "Select a wordlist to display a preview of its first 10 lines";
        public string LinesPreview
        {
            get => linesPreview;
            set
            {
                linesPreview = value;
                OnPropertyChanged();
            }
        }

        public SelectWordlistDialogViewModel()
        {
            wordlistRepo = SP.GetService<IWordlistRepository>();
            CreateCollection();

            wordlistsViewModel = SP.GetService<ViewModelsService>().Wordlists;

            if (wordlistsViewModel is not null)
            {
                SearchString = wordlistsViewModel.SearchString;
            }
        }

        private List<WordlistEntity> allWordlists;

        private void CreateCollection()
        {
            var entities = wordlistRepo.GetAll().ToList();
            allWordlists = entities;
            WordlistsCollection = new ObservableCollection<WordlistEntity>(entities);
        }

        private void FilterCollection()
        {
            if (allWordlists == null) return;

            var filtered = allWordlists.Where(w => w.Name.Contains(SearchString, StringComparison.OrdinalIgnoreCase));
            WordlistsCollection = new ObservableCollection<WordlistEntity>(filtered);
        }
    }
}
