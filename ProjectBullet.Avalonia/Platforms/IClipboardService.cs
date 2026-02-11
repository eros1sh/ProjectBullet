using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input.Platform;
using System.Threading.Tasks;

namespace ProjectBullet.Avalonia.Platforms
{
    public interface IClipboardService
    {
        Task SetTextAsync(string text);
        Task<string> GetTextAsync();
    }

    public class ClipboardService : IClipboardService
    {
        private IClipboard GetClipboard()
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                return desktop.MainWindow?.Clipboard;
            }
            return null;
        }

        public async Task SetTextAsync(string text)
        {
            var clipboard = GetClipboard();
            if (clipboard != null)
            {
                await clipboard.SetTextAsync(text);
            }
        }

        public async Task<string> GetTextAsync()
        {
            var clipboard = GetClipboard();
            if (clipboard != null)
            {
                return await clipboard.GetTextAsync();
            }
            return string.Empty;
        }
    }
}
