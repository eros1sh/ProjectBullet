using ProjectBullet.Avalonia.ViewModels;
using RuriLib.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ProjectBullet.Avalonia.Views.Dialogs
{
    public partial class BotLogDialog : UserControl
    {
        private readonly BotLogDialogViewModel vm;
        private string fullLogText = string.Empty;

        public BotLogDialog()
        {
            InitializeComponent();
        }

        public BotLogDialog(IBotLogger logger)
        {
            vm = new BotLogDialogViewModel();
            DataContext = vm;

            InitializeComponent();

            if (logger is null)
            {
                logTextBox.Text = "Bot log was not enabled when this hit was obtained";
                fullLogText = logTextBox.Text;
                return;
            }

            var sb = new StringBuilder();
            foreach (var entry in logger.Entries)
            {
                sb.AppendLine(entry.Message);
            }

            fullLogText = sb.ToString();
            logTextBox.Text = fullLogText;
        }

        #region Search
        private void Search(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(vm.SearchString))
            {
                return;
            }

            var startIndex = 0;
            var indices = new List<int>();
            int index;

            while ((index = fullLogText.IndexOf(vm.SearchString, startIndex, StringComparison.InvariantCultureIgnoreCase)) != -1)
            {
                startIndex = index + vm.SearchString.Length;
                indices.Add(index);
            }

            vm.Indices = indices.ToArray();
        }

        private void PreviousMatch(object sender, RoutedEventArgs e)
        {
            if (vm.Indices.Length == 0) return;

            if (vm.CurrentMatchIndex == 0)
                vm.CurrentMatchIndex = vm.Indices.Length - 1;
            else
                vm.CurrentMatchIndex--;

            ScrollToMatch();
        }

        private void NextMatch(object sender, RoutedEventArgs e)
        {
            if (vm.Indices.Length == 0) return;

            if (vm.CurrentMatchIndex == vm.Indices.Length - 1)
                vm.CurrentMatchIndex = 0;
            else
                vm.CurrentMatchIndex++;

            ScrollToMatch();
        }

        private void ScrollToMatch()
        {
            if (vm.Indices.Length > 0)
            {
                logTextBox.SelectionStart = vm.Indices[vm.CurrentMatchIndex];
                logTextBox.SelectionEnd = vm.Indices[vm.CurrentMatchIndex] + vm.SearchString.Length;
            }
        }
        #endregion
    }

    public class BotLogDialogViewModel : ViewModelBase
    {
        private string searchString = string.Empty;
        public string SearchString
        {
            get => searchString;
            set { searchString = value; OnPropertyChanged(); }
        }

        private int[] indices = Array.Empty<int>();
        public int[] Indices
        {
            get => indices;
            set { indices = value; CurrentMatchIndex = 0; }
        }

        private int currentMatchIndex;
        public int CurrentMatchIndex
        {
            get => currentMatchIndex;
            set
            {
                currentMatchIndex = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(MatchInfo));
            }
        }

        public string MatchInfo => $"{CurrentMatchIndex + 1} of {Indices.Length}";
    }
}
