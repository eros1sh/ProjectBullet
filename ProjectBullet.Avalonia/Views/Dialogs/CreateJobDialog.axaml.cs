using ProjectBullet.Core.Models.Jobs;
using ProjectBullet.Avalonia.Views.Pages;
using System;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ProjectBullet.Avalonia.Views.Dialogs
{
    public partial class CreateJobDialog : UserControl
    {
        private readonly object caller;

        public CreateJobDialog()
        {
            InitializeComponent();
        }

        public CreateJobDialog(object caller)
        {
            this.caller = caller;

            InitializeComponent();
        }

        private void CreateMultiRunJob(object sender, RoutedEventArgs e) => CreateJob(JobType.MultiRun);
        private void CreateProxyCheckJob(object sender, RoutedEventArgs e) => CreateJob(JobType.ProxyCheck);

        private async void CreateJob(JobType type)
        {
            Action<JobOptions> onAccept = options =>
            {
                if (caller is Jobs page)
                {
                    page.CreateJob(options);
                }
            };

            var owner = this.VisualRoot as Window;

            switch (type)
            {
                case JobType.MultiRun:
                    await new MainDialog(new MultiRunJobOptionsDialog(null, onAccept), "Create Multi Run Job", 800, 600).ShowDialog(owner);
                    break;

                case JobType.ProxyCheck:
                    await new MainDialog(new ProxyCheckJobOptionsDialog(null, onAccept), "Create Proxy Check Job").ShowDialog(owner);
                    break;
            }

            if (this.VisualRoot is MainDialog dialog) dialog.Close();
        }
    }
}
