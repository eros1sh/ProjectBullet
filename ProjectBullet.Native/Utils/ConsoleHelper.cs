using System.Runtime.InteropServices;

namespace ProjectBullet.Native.Utils
{
    public static class ConsoleHelper
    {
        [DllImport("Kernel32")]
        static internal extern void AllocConsole();

        [DllImport("Kernel32")]
        static internal extern void FreeConsole();
    }
}
