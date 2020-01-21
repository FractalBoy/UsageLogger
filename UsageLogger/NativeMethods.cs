using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace UsageLogger
{
    class NativeMethods
    {
        public const uint WINEVENT_OUTOFCONTEXT = 0;
        public const uint EVENT_SYSTEM_FOREGROUND = 3;

        public delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

        [DllImport("user32.dll")]
        public static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc, WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        public static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int GetWindowThreadProcessId(IntPtr handle, out uint processId);

        public static string GetWindowProgramName(IntPtr handle)
        {
            if (handle == IntPtr.Zero)
            {
                return null;
            }

            GetWindowThreadProcessId(handle, out uint pid);

            var process = Process.GetProcessById((int)pid);
            string exePath = process.MainModule.FileName;
            return FileVersionInfo.GetVersionInfo(exePath).FileDescription;
        }
    }
}
