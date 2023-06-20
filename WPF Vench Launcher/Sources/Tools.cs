using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPF_Vench_Launcher.Sources
{
    class Tools
    {
        public static void openUrl(string url)
        {
            try
            {
                Process.Start(url);
            }
            catch (Exception ex)
            {
            }
        }
    }
}
