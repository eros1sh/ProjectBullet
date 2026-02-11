using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ProjectBullet.Avalonia.Views.Dialogs
{
    public partial class CustomInputDialog : UserControl
    {
        private readonly Action<string> onAnswer;

        public CustomInputDialog()
        {
            InitializeComponent();
        }

        public CustomInputDialog(string question, string defaultAnswer, Action<string> onAnswer)
        {
            this.onAnswer = onAnswer;
            InitializeComponent();

            this.question.Text = question;

            var options = new List<string>(defaultAnswer.Split(','));
            answerComboBox.ItemsSource = options;
            if (options.Count > 0)
                answerComboBox.Text = options[0];
            answerComboBox.Focus();
        }

        private void Ok(object sender, RoutedEventArgs e)
        {
            onAnswer(answerComboBox.Text);
            if (this.VisualRoot is MainDialog dialog) dialog.Close();
        }
    }
}
