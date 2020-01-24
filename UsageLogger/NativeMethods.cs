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

        [StructLayout(LayoutKind.Sequential)]
        public struct Rect
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [DllImport("gdi32.dll")]
        public static extern int GetDeviceCaps(IntPtr hdc, int nIndex);
        public enum DeviceCap
        {
            VERTRES = 10,
            DESKTOPVERTRES = 117,

            // http://pinvoke.net/default.aspx/gdi32/GetDeviceCaps.html
        }

        [DllImport("user32.dll")]
        public static extern IntPtr GetWindowRect(IntPtr hWnd, ref Rect rect);

        [DllImport("user32.dll")]
        public static extern IntPtr GetClientRect(IntPtr hWnd, ref Rect rect);

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

            if (pid == 0)
            {
                return null;
            }

            var process = Process.GetProcessById((int)pid);
            string exePath = process.MainModule.FileName;
            return FileVersionInfo.GetVersionInfo(exePath).FileDescription;
        }
    }
}
