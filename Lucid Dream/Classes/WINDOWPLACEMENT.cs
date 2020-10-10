using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace Lucid_Dream
{
    public partial class MainWindow
    {
        [Serializable]
        [StructLayout(LayoutKind.Sequential)]
        internal struct WINDOWPLACEMENT
        {
            public int length;
            public int flags;
            public AnotherWindowState showCmd;
            public Point ptMinPosition;
            public Point ptMaxPosition;
            public Rectangle rcNormalPosition;
        }
    }
}
