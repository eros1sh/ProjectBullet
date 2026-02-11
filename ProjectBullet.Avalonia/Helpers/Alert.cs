using Avalonia.Controls;
using Avalonia.Threading;
using ProjectBullet.Avalonia.Views.Dialogs;
using System;
using System.Threading.Tasks;

namespace ProjectBullet.Avalonia.Helpers
{
    public static class Alert
    {
        public static void Info(string title, string message) => ShowAlert(AlertType.Info, title, message);
        public static void Success(string title, string message) => ShowAlert(AlertType.Success, title, message);
        public static void Warning(string title, string message) => ShowAlert(AlertType.Warning, title, message);
        public static void Error(string title, string message) => ShowAlert(AlertType.Error, title, message);

        public static bool Choice(string title, string message, string yesText = "Yes", string noText = "No")
        {
            return ChoiceAsync(title, message, yesText, noText).GetAwaiter().GetResult();
        }

        public static async Task<bool> ChoiceAsync(string title, string message, string yesText = "Yes", string noText = "No")
        {
            var choice = false;
            var dialog = new MainDialog(new ChoiceDialog(title, message, b => choice = b, yesText, noText), title);
            await dialog.ShowDialog(GetMainWindow());
            return choice;
        }

        public static string CustomInput(string question, string defaultAnswer)
        {
            return CustomInputAsync(question, defaultAnswer).GetAwaiter().GetResult();
        }

        public static async Task<string> CustomInputAsync(string question, string defaultAnswer)
        {
            var answer = string.Empty;
            var dialog = new MainDialog(new CustomInputDialog(question, defaultAnswer, a => answer = a), "Custom input");
            await dialog.ShowDialog(GetMainWindow());
            return answer;
        }

        private static void ShowAlert(AlertType type, string title, string message)
        {
            _ = ShowAlertAsync(type, title, message);
        }

        private static async Task ShowAlertAsync(AlertType type, string title, string message)
        {
            if (Dispatcher.UIThread.CheckAccess())
            {
                var dialog = new MainDialog(new AlertDialog(type, title, message), title);
                await dialog.ShowDialog(GetMainWindow());
            }
            else
            {
                await Dispatcher.UIThread.InvokeAsync(async () =>
                {
                    var dialog = new MainDialog(new AlertDialog(type, title, message), title);
                    await dialog.ShowDialog(GetMainWindow());
                });
            }
        }

        public static void Exception(Exception ex) => Error(ex.GetType().Name, ex.Message);

        private static Window GetMainWindow()
        {
            if (global::Avalonia.Application.Current?.ApplicationLifetime
                is global::Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
            {
                return desktop.MainWindow;
            }
            return null;
        }
    }
}
