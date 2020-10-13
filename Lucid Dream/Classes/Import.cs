using System;
using System.Runtime.InteropServices;
using static Lucid_Dream.MainWindow;

namespace Lucid_Dream.Classes
{
    static class Import
    {
        [DllImport("user32.dll", SetLastError = true)]
        public static extern void GetLastInputInfo(ref LASTINPUTINFO plii);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetWindowPlacement(IntPtr hWnd, ref WINDOWPLACEMENT lpwndpl);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true)]
        public static extern void LockWorkStation();

        [DllImport("gdi32.dll")]
        public static extern void DeleteObject([In] IntPtr hObject);
    }
}
