using Avalonia.Controls;
using Avalonia.Interactivity;
using System;

namespace ProjectBullet.Avalonia.Views.Dialogs
{
    public partial class ChoiceDialog : UserControl
    {
        private readonly Action<bool> onChoice;

        public ChoiceDialog()
        {
            InitializeComponent();
            onChoice = _ => { };
        }

        public ChoiceDialog(string title, string message, Action<bool> onChoice,
            string yesText = "Yes", string noText = "No")
        {
            this.onChoice = onChoice;
            InitializeComponent();

            titleText.Text = title;
            messageText.Text = message;
            yesButtonText.Text = yesText;
            noButtonText.Text = noText;

            yesButton.Focus();
        }

        private void Yes(object sender, RoutedEventArgs e)
        {
            onChoice(true);
            if (this.VisualRoot is MainDialog dialog)
            {
                dialog.Close(true);
            }
        }

        private void No(object sender, RoutedEventArgs e)
        {
            onChoice(false);
            if (this.VisualRoot is MainDialog dialog)
            {
                dialog.Close(false);
            }
        }
    }
}
