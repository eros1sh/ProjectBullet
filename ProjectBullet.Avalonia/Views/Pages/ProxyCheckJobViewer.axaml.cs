using ProjectBullet.Core.Services;
using ProjectBullet.Avalonia.Extensions;
using ProjectBullet.Avalonia.Helpers;
using ProjectBullet.Avalonia.ViewModels;
using ProjectBullet.Avalonia.Views.Dialogs;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using System;

namespace ProjectBullet.Avalonia.Views.Pages
{
    /// <summary>
    /// Interaction logic for ProxyCheckJobViewer.axaml
    /// </summary>
    public partial class ProxyCheckJobViewer : UserControl
    {
        private readonly ProjectBulletSettingsService obSettingsService;
        private ProxyCheckJobViewerViewModel vm;

        public ProxyCheckJobViewer()
        {
            obSettingsService = SP.GetService<ProjectBulletSettingsService>();
            InitializeComponent();
        }

        public void BindViewModel(ProxyCheckJobViewModel jobVM)
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

            vm = new ProxyCheckJobViewerViewModel(jobVM);
            vm.NewMessage += OnResultMessage;
            DataContext = vm;
        }

        private async void Start(object sender, RoutedEventArgs e)
        {
            try
            {
                Dispatcher.UIThread.Post(() => jobLog.Clear());
                jobLog.BufferSize = obSettingsService.Settings.GeneralSettings.LogBufferSize;
                await vm.Start();
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
                await vm.Stop();
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
                await vm.Pause();
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
                await vm.Resume();
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
                await vm.Abort();
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
