using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace DotNetPackages
{
    public class ConsoleUtils
    {
        private enum StdHandle { Stdin = -10, Stdout = -11 };

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetStdHandle(StdHandle std);

        [DllImport("kernel32.dll")]
        static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

        [DllImport("kernel32.dll")]
        static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

        public static void EnableANSI()
        {
            try
            {
                EnableANSIInternal();
            }
            catch
            {
                // ansi already enabled, or non-conventional console
            }
        }

        private static void EnableANSIInternal()
        {
            const uint ENABLE_VIRTUAL_TERMINAL_PROCESSING = 0x0004;
            const uint DISABLE_NEWLINE_AUTO_RETURN = 0x0008;
            const uint ENABLE_VIRTUAL_TERMINAL_INPUT = 0x0200;

            IntPtr inConsoleHandle = GetStdHandle(StdHandle.Stdin);
            IntPtr outConsoleHandle = GetStdHandle(StdHandle.Stdout);

            if (!GetConsoleMode(inConsoleHandle, out uint inConsoleMode))
                Fail();

            if (!GetConsoleMode(outConsoleHandle, out uint outConsoleMode))
                Fail();

            inConsoleMode |= ENABLE_VIRTUAL_TERMINAL_INPUT;
            outConsoleMode |= ENABLE_VIRTUAL_TERMINAL_PROCESSING | DISABLE_NEWLINE_AUTO_RETURN;

            if (!SetConsoleMode(inConsoleHandle, inConsoleMode) || !SetConsoleMode(outConsoleHandle, outConsoleMode))
                Fail();
        }

        private static void Fail()
        {
            // The exception will include the last win32 error
            throw new Win32Exception();
        }
    }
}
