using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Projektanker.Icons.Avalonia;
using System;

namespace ProjectBullet.Avalonia.Views.Dialogs
{
    public partial class AlertDialog : UserControl
    {
        public AlertDialog()
        {
            InitializeComponent();
        }

        public AlertDialog(AlertType type, string title, string message)
        {
            InitializeComponent();

            titleText.Text = title;
            messageText.Text = message;

            icon.Value = type switch
            {
                AlertType.Success => "mdi-check-circle",
                AlertType.Warning => "mdi-alert",
                AlertType.Error => "mdi-close-circle",
                AlertType.Info => "mdi-information",
                _ => throw new NotImplementedException()
            };

            icon.Foreground = type switch
            {
                AlertType.Success => new SolidColorBrush(Colors.YellowGreen),
                AlertType.Warning => new SolidColorBrush(Colors.Orange),
                AlertType.Error => new SolidColorBrush(Colors.Tomato),
                AlertType.Info => new SolidColorBrush(Colors.SkyBlue),
                _ => throw new NotImplementedException()
            };

            okButton.Focus();
        }

        private void Ok(object sender, RoutedEventArgs e)
        {
            if (Parent is MainDialog dialog)
                dialog.Close();
            else if (this.VisualRoot is MainDialog dlg)
                dlg.Close();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (Parent is MainDialog dialog)
                    dialog.Close();
                else if (this.VisualRoot is MainDialog dlg)
                    dlg.Close();
            }
            base.OnKeyDown(e);
        }
    }

    public enum AlertType
    {
        Success,
        Warning,
        Error,
        Info
    }
}
