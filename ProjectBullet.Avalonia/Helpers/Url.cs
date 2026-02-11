using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ProjectBullet.Avalonia.Helpers
{
    public static class Url
    {
        public static void Open(string url)
        {
            // SECURITY FIX: Validate URL scheme to prevent command injection
            if (string.IsNullOrWhiteSpace(url)) return;

            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return;
            if (uri.Scheme != "http" && uri.Scheme != "https" && uri.Scheme != "mailto") return;

            var safeUrl = uri.AbsoluteUri;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Process.Start(new ProcessStartInfo(safeUrl) { UseShellExecute = true });
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Process.Start("xdg-open", safeUrl);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Process.Start("open", safeUrl);
            }
        }
    }
}
