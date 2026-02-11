using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Newtonsoft.Json;
using ProjectBullet.Core.Models.Jobs;
using ProjectBullet.Core.Repositories;
using ProjectBullet.Avalonia.Helpers;
using ProjectBullet.Avalonia.Services;
using ProjectBullet.Avalonia.ViewModels;
using ProjectBullet.Avalonia.Views.Dialogs;
using System;

namespace ProjectBullet.Avalonia.Views.Pages
{
    /// <summary>
    /// Interaction logic for Jobs.axaml
    /// </summary>
    public partial class Jobs : UserControl
    {
        private readonly MainWindow mainWindow;
        private readonly IJobRepository jobRepo;
        private readonly JobsViewModel vm;

        public Jobs()
        {
            mainWindow = SP.GetService<MainWindow>();
            jobRepo = SP.GetService<IJobRepository>();
            vm = SP.GetService<ViewModelsService>().Jobs;
            DataContext = vm;

            InitializeComponent();
        }

        private async void NewJob(object sender, RoutedEventArgs e)
            => await new MainDialog(new CreateJobDialog(this), "Select job type").ShowDialog(TopLevel.GetTopLevel(this) as Window);

        private void RemoveAll(object sender, RoutedEventArgs e)
        {
            try
            {
                vm.RemoveAll();
            }
            catch (Exception ex)
            {
                Alert.Exception(ex);
            }
        }

        private void EditJob(object sender, RoutedEventArgs e) => EditJob((JobViewModel)(sender as Button).Tag);

        public async void EditJob(JobViewModel jobVM)
        {
            var entity = await jobRepo.GetAsync(jobVM.Id);
            var jsonSettings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto };
            var jobOptions = JsonConvert.DeserializeObject<JobOptionsWrapper>(entity.JobOptions, jsonSettings).Options;
            Action<JobOptions> onAccept = async options =>
            {
                jobVM = await vm.EditJobAsync(entity, options);
                mainWindow.DisplayJob(jobVM);
            };

            UserControl page = jobVM switch
            {
                MultiRunJobViewModel => new MultiRunJobOptionsDialog(jobOptions as MultiRunJobOptions, onAccept),
                ProxyCheckJobViewModel => new ProxyCheckJobOptionsDialog(jobOptions as ProxyCheckJobOptions, onAccept),
                _ => throw new NotImplementedException()
            };

            await new MainDialog(page, $"Edit job #{entity.Id}", 800, 600).ShowDialog(TopLevel.GetTopLevel(this) as Window);
        }

        private async void CloneJob(object sender, RoutedEventArgs e)
        {
            var jobVM = (JobViewModel)(sender as Button).Tag;
            var entity = await jobRepo.GetAsync(jobVM.Id);
            var jsonSettings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto };
            var oldOptions = JsonConvert.DeserializeObject<JobOptionsWrapper>(entity.JobOptions, jsonSettings).Options;
            var newOptions = JobOptionsFactory.CloneExistant(oldOptions);

            Action<JobOptions> onAccept = async options =>
            {
                var cloned = await vm.CloneJobAsync(entity.JobType, options);
                mainWindow.DisplayJob(cloned);
            };

            UserControl page = jobVM switch
            {
                MultiRunJobViewModel => new MultiRunJobOptionsDialog(newOptions as MultiRunJobOptions, onAccept),
                ProxyCheckJobViewModel => new ProxyCheckJobOptionsDialog(newOptions as ProxyCheckJobOptions, onAccept),
                _ => throw new NotImplementedException()
            };

            await new MainDialog(page, $"Clone job #{entity.Id}", 800, 600).ShowDialog(TopLevel.GetTopLevel(this) as Window);
        }

        private async void RemoveJob(object sender, RoutedEventArgs e)
        {
            try
            {
                await vm.RemoveJobAsync((JobViewModel)(sender as Button).Tag);
            }
            catch (Exception ex)
            {
                Alert.Exception(ex);
            }
        }

        public async void CreateJob(JobOptions options) => await vm.CreateJobAsync(options);

        private void ViewJob(object sender, PointerPressedEventArgs e)
            => SP.GetService<MainWindow>().DisplayJob((JobViewModel)(sender as WrapPanel).Tag);
    }
}
