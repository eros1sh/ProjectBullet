using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using ProjectBullet.Avalonia.Helpers;
using ProjectBullet.Avalonia.Services;
using ProjectBullet.Avalonia.ViewModels;
using RuriLib.Logging;
using System;
using System.Collections.Generic;

namespace ProjectBullet.Avalonia.Views.Pages.Shared
{
    /// <summary>
    /// Interaction logic for Debugger.axaml
    /// </summary>
    public partial class Debugger : UserControl
    {
        private readonly DebuggerViewModel vm;
        private readonly List<string> logLines = new();

        public Debugger()
        {
            vm = SP.GetService<ViewModelsService>().Debugger;
            DataContext = vm;

            vm.NewLogEntry += NewLogEntry;
            vm.LogCleared += ClearLog;

            InitializeComponent();
            tabControl.SelectedIndex = 0;
        }

        private void ShowLog(object sender, RoutedEventArgs e) => tabControl.SelectedIndex = 0;
        private void ShowVariables(object sender, RoutedEventArgs e) => tabControl.SelectedIndex = 1;
        private void ShowHTML(object sender, RoutedEventArgs e) => tabControl.SelectedIndex = 2;

        private async void Start(object sender, RoutedEventArgs e)
        {
            if (!vm.PersistLog)
            {
                logRTB.Text = string.Empty;
                logLines.Clear();
            }

            try
            {
                await vm.RunAsync();
            }
            catch (Exception ex)
            {
                Alert.Exception(ex);
            }
        }

        private void TakeStep(object sender, RoutedEventArgs e) => vm.TakeStep();

        private void Stop(object sender, RoutedEventArgs e) => vm.Stop();

        private void NewLogEntry(object sender, BotLoggerEntry entry)
        {
            Dispatcher.UIThread.Post(() =>
            {
                // Append the log message
                logLines.Add(entry.Message);
                logRTB.Text = string.Join(Environment.NewLine, logLines);

                // Scroll to the bottom of the log
                try
                {
                    logScrollViewer.ScrollToEnd();
                }
                catch
                {

                }

                // Recreate the variables list
                var variableLines = new List<string>();
                foreach (var variable in vm.Variables)
                {
                    variableLines.Add($"{variable.Name} ({variable.Type}) = {variable.AsString()}");
                }
                variablesRTB.Text = string.Join(Environment.NewLine, variableLines);

                // Update the HTML view
                if (entry.CanViewAsHtml)
                {
                    htmlViewer.HTML = entry.Message;
                }
            });
        }

        private void ClearLog(object sender, EventArgs e)
        {
            Dispatcher.UIThread.Post(() =>
            {
                logRTB.Text = string.Empty;
                logLines.Clear();
                variablesRTB.Text = string.Empty;
                htmlViewer.HTML = string.Empty;
            });
        }

        #region Search
        private void Search(object sender, RoutedEventArgs e)
        {
            // Check for empty search
            if (string.IsNullOrWhiteSpace(vm.SearchString))
            {
                return;
            }

            var text = logRTB.Text ?? string.Empty;
            var startIndex = 0;
            var indices = new List<int>();

            while (true)
            {
                var index = text.IndexOf(vm.SearchString, startIndex, StringComparison.InvariantCultureIgnoreCase);
                if (index == -1) break;

                startIndex = index + vm.SearchString.Length;
                indices.Add(startIndex);
            }

            vm.Indices = indices.ToArray();
        }

        private void PreviousMatch(object sender, RoutedEventArgs e)
        {
            // If no matches, do nothing
            if (vm.Indices.Length == 0)
            {
                return;
            }

            // If we need to loop around
            if (vm.CurrentMatchIndex == 0)
            {
                vm.CurrentMatchIndex = vm.Indices.Length - 1;
            }
            else
            {
                vm.CurrentMatchIndex--;
            }
        }

        private void NextMatch(object sender, RoutedEventArgs e)
        {
            // If no matches, do nothing
            if (vm.Indices.Length == 0)
            {
                return;
            }

            // If we need to loop around
            if (vm.CurrentMatchIndex == vm.Indices.Length - 1)
            {
                vm.CurrentMatchIndex = 0;
            }
            else
            {
                vm.CurrentMatchIndex++;
            }
        }
        #endregion
    }
}
