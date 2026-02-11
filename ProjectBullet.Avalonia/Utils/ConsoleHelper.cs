using System.Runtime.InteropServices;

namespace ProjectBullet.Avalonia.Utils
{
    public static class ConsoleHelper
    {
        public static void AllocConsole()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                AllocConsoleWindows();
            }
            // On Linux/macOS, console is always available via terminal
        }

        public static void FreeConsole()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                FreeConsoleWindows();
            }
        }

        [DllImport("Kernel32", EntryPoint = "AllocConsole")]
        private static extern void AllocConsoleWindows();

        [DllImport("Kernel32", EntryPoint = "FreeConsole")]
        private static extern void FreeConsoleWindows();
    }
}
