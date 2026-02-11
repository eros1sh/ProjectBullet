using ProjectBullet.Core.Services;
using ProjectBullet.Avalonia.Views.Pages;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ProjectBullet.Avalonia.Views.Dialogs
{
    public partial class ChangeBotsDialog : UserControl
    {
        private readonly object caller;

        public ChangeBotsDialog()
        {
            InitializeComponent();
        }

        public ChangeBotsDialog(object caller, int oldValue)
        {
            this.caller = caller;

            InitializeComponent();
            bots.Maximum = SP.GetService<JobFactoryService>().BotLimit;
            bots.Value = oldValue;
        }

        private void Accept(object sender, RoutedEventArgs e)
        {
            if (caller is MultiRunJobViewer mr)
            {
                mr.ChangeBots((int)bots.Value);
            }
            else if (caller is ProxyCheckJobViewer pc)
            {
                pc.ChangeBots((int)bots.Value);
            }

            if (this.VisualRoot is MainDialog dialog) dialog.Close();
        }
    }
}
