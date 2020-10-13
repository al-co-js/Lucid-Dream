using Lucid_Dream.Classes;
using System;
using System.Diagnostics;
using System.Windows;

namespace Lucid_Dream
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            if (Process.GetProcessesByName("Lucid Dream").Length > 1)
            {
                Current.Shutdown();
            }
            else
            {
                base.OnStartup(e);
            }
        }
    }
}
