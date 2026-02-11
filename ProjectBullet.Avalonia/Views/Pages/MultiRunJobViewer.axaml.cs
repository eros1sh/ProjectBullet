using ProjectBullet.Core.Services;
using ProjectBullet.Avalonia.Extensions;
using ProjectBullet.Avalonia.Helpers;
using ProjectBullet.Avalonia.Services;
using ProjectBullet.Avalonia.ViewModels;
using ProjectBullet.Avalonia.Views.Dialogs;
using RuriLib.Models.Configs;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ProjectBullet.Avalonia.Views.Pages
{
    /// <summary>
    /// Interaction logic for MultiRunJobViewer.axaml
    /// </summary>
    public partial class MultiRunJobViewer : UserControl
    {
        private readonly MainWindow mainWindow;
        private readonly ProjectBulletSettingsService obSettingsService;
        private MultiRunJobViewerViewModel vm;

        private IEnumerable<HitViewModel> SelectedHits => hitsListView.SelectedItems.Cast<HitViewModel>().ToList();

        public MultiRunJobViewer()
        {
            mainWindow = SP.GetService<MainWindow>();
            obSettingsService = SP.GetService<ProjectBulletSettingsService>();
            InitializeComponent();
        }

        public void BindViewModel(MultiRunJobViewModel jobVM)
        {
            if (vm is not null)
            {
                vm.Dispose();

                try
                {
                    vm.NewMessage -= OnResultMessage;
                }
                catch
                {
                }
            }

            vm = new MultiRunJobViewerViewModel(jobVM);
            vm.NewMessage += OnResultMessage;
            DataContext = vm;

            cpmHitsChart.Series = vm.ChartSeries;
            cpmHitsChart.XAxes = vm.XAxes;
            cpmHitsChart.YAxes = vm.YAxes;
        }

        private async void Start(object sender, RoutedEventArgs e)
        {
            try
            {
                Dispatcher.UIThread.Post(() => jobLog.Clear());
                jobLog.BufferSize = obSettingsService.Settings.GeneralSettings.LogBufferSize;
                await vm.StartAsync();
            }
            catch (Exception ex)
            {
                Alert.Exception(ex);
            }
        }

        private async void Stop(object sender, RoutedEventArgs e)
        {
            try
            {
                await vm.StopAsync();
            }
            catch (Exception ex)
            {
                Alert.Exception(ex);
            }
        }

        private async void Pause(object sender, RoutedEventArgs e)
        {
            try
            {
                await vm.PauseAsync();
            }
            catch (Exception ex)
            {
                Alert.Exception(ex);
            }
        }

        private async void Resume(object sender, RoutedEventArgs e)
        {
            try
            {
                await vm.ResumeAsync();
            }
            catch (Exception ex)
            {
                Alert.Exception(ex);
            }
        }

        private async void Abort(object sender, RoutedEventArgs e)
        {
            try
            {
                await vm.AbortAsync();
            }
            catch (Exception ex)
            {
                Alert.Exception(ex);
            }
        }

        private void SkipWait(object sender, RoutedEventArgs e)
        {
            try
            {
                vm.SkipWait();
            }
            catch (Exception ex)
            {
                Alert.Exception(ex);
            }
        }

        private void ChangeOptions(object sender, RoutedEventArgs e) => mainWindow.EditJob(vm.Job);

        private async void ChangeBots(object sender, PointerPressedEventArgs e)
            => await new MainDialog(new ChangeBotsDialog(this, vm.Job.Bots), "Change bots").ShowDialog(TopLevel.GetTopLevel(this) as Window);

        public async void ChangeBots(int newValue)
        {
            try
            {
                await vm.ChangeBotsAsync(newValue);
            }
            catch (Exception ex)
            {
                Alert.Exception(ex);
            }
        }

        private void CopySelectedHits(object sender, RoutedEventArgs e)
            => SelectedHits.CopyToClipboard(h => h.Data);

        private void CopySelectedProxies(object sender, RoutedEventArgs e)
            => SelectedHits.CopyToClipboard(h => h.Proxy);

        private void CopySelectedHitsCapture(object sender, RoutedEventArgs e)
            => SelectedHits.CopyToClipboard(h => $"{h.Data} | {h.Capture}");

        private void SendToDebugger(object sender, RoutedEventArgs e)
        {
            var hitVM = SelectedHits.FirstOrDefault();

            if (hitVM is not null)
            {
                var debugger = SP.GetService<ViewModelsService>().Debugger;
                debugger.TestData = hitVM.Data;

                if (hitVM.Hit.Proxy is not null)
                {
                    debugger.TestProxy = hitVM.Hit.Proxy.ToString();
                    debugger.ProxyType = hitVM.Hit.Proxy.Type;
                }
            }
        }

        private void SelectAll(object sender, RoutedEventArgs e) => hitsListView.SelectAll();

        private void ShowBotLog(object sender, RoutedEventArgs e)
        {
            var hitVM = SelectedHits.FirstOrDefault();

            if (hitVM is null) return;

            if (hitVM.Hit.Config.Mode == ConfigMode.DLL)
            {
                Alert.Error("Bot log unavailable", "The bot log is not available for pre-compiled configs");
                return;
            }

            new MainDialog(new BotLogDialog(hitVM.Hit.BotLogger), $"Bot log for {hitVM.Data}").Show();
        }

        private void OnResultMessage(object sender, string message, Color color)
            => Dispatcher.UIThread.Post(() =>
            {
                if (obSettingsService.Settings.GeneralSettings.EnableJobLogging)
                {
                    jobLog.Append(message, color);
                }
            });
    }
}
