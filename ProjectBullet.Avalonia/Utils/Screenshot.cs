using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace ProjectBullet.Avalonia.Utils
{
    public static class Screenshot
    {
        public static void Take(int width, int height, int top, int left)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                TakeWindows(width, height, top, left);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                TakeLinux(width, height, top, left);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                TakeMacOS(width, height, top, left);
            }
        }

        private static void TakeWindows(int width, int height, int top, int left)
        {
            // Use PowerShell for screenshot on Windows (no System.Drawing dependency)
            var script = $@"
Add-Type -AssemblyName System.Windows.Forms
$bitmap = New-Object System.Drawing.Bitmap({width}, {height})
$graphics = [System.Drawing.Graphics]::FromImage($bitmap)
$graphics.CopyFromScreen({left}, {top}, 0, 0, $bitmap.Size)
$bitmap.Save('screenshot.jpg', [System.Drawing.Imaging.ImageFormat]::Jpeg)
$graphics.Dispose()
$bitmap.Dispose()
";
            Process.Start(new ProcessStartInfo("powershell", $"-Command \"{script}\"")
            {
                CreateNoWindow = true,
                UseShellExecute = false
            })?.WaitForExit();
        }

        private static void TakeLinux(int width, int height, int top, int left)
        {
            // Use import (ImageMagick) or gnome-screenshot
            try
            {
                Process.Start("import", $"-window root -crop {width}x{height}+{left}+{top} screenshot.jpg")?.WaitForExit();
            }
            catch
            {
                // Fallback to gnome-screenshot
                try
                {
                    Process.Start("gnome-screenshot", $"-a -f screenshot.jpg")?.WaitForExit();
                }
                catch
                {
                    Console.WriteLine("Screenshot not supported on this Linux setup.");
                }
            }
        }

        private static void TakeMacOS(int width, int height, int top, int left)
        {
            // Use screencapture on macOS
            Process.Start("screencapture", $"-R{left},{top},{width},{height} screenshot.jpg")?.WaitForExit();
        }
    }
}
