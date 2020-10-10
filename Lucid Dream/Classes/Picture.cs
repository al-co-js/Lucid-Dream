using System;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Lucid_Dream.Classes
{
    public static class Picture
    {
        public static void Delete()
        {
            var pic = Directory.GetFiles(@"C:\Lucid Dream\", "*.png").Select(Path.GetFileName).ToArray();
            foreach (var p in pic)
            {
                try
                {
                    if (DateTime.ParseExact(p.Substring(0, p.Length - 4), "yyyy-MM-dd-hh-mm-ss", CultureInfo.CurrentCulture) + TimeSpan.FromDays(7) <= DateTime.Now)
                    {
                        File.Delete($@"C:\Lucid Dream\{p}");
                    }
                }
                catch { }
            }
        }
    }
}
