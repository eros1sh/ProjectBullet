using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ProjectBullet.Avalonia.Extensions
{
    public static class IEnumerableExtensions
    {
        public static void SaveToFile<T>(this IEnumerable<T> items, string fileName, Func<T, string> mapping)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentNullException(nameof(fileName), "The filename must not be empty");
            }

            File.WriteAllLines(fileName, items.Select(i => mapping(i)));
        }

        public static async Task CopyToClipboardAsync<T>(this IEnumerable<T> items, Func<T, string> mapping)
        {
            var text = string.Join(Environment.NewLine, items.Select(i => mapping(i)));

            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
                && desktop.MainWindow?.Clipboard is { } clipboard)
            {
                await clipboard.SetTextAsync(text);
            }
        }

        // Synchronous fallback for compatibility
        public static void CopyToClipboard<T>(this IEnumerable<T> items, Func<T, string> mapping)
        {
            items.CopyToClipboardAsync(mapping).GetAwaiter().GetResult();
        }
    }
}
