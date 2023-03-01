using Microsoft.CSharp;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Runtime.InteropServices;
using WPF_Vench_Launcher.pages;
using WPF_Vench_Launcher.Sources;
using System.Windows.Forms;

namespace WPF_Vench_Launcher
{
    /// <summary>
    /// Логика взаимодействия для Boost.xaml
    /// </summary>
    public partial class Boost : Page
    {
        private System.Windows.Threading.DispatcherTimer timer;
        public Boost()
        {
            InitializeComponent();
            LoadConfig();
            timer = InitTimer();
            
        }

        private void LoadConfig()
        {
            sameTimeAccounts.Text = Convert.ToString(Config.GetConfig().MaxSameTimeAccounts);
            waitBeforeClose.Text = Convert.ToString(Config.GetConfig().MaxRemainingTimeToDropCase);
            ServerIp.Text = Convert.ToString(Config.GetConfig().ServersToConnect);

            InQueue.Text = Convert.ToString(FarmManager.QueueCount);
            Started.Text = Convert.ToString(FarmManager.StartedCount);
            Farmed.Text = Convert.ToString(FarmManager.FarmedCount);
        }

        private System.Windows.Threading.DispatcherTimer InitTimer()
        {
            System.Windows.Threading.DispatcherTimer timer = new System.Windows.Threading.DispatcherTimer();
            timer.Tick += new EventHandler(timerTick);
            timer.Interval = new TimeSpan(0, 0, 1);
            timerTick(new object(),new EventArgs());
            timer.Start();
            return timer;
        }

        private void timerTick(object sender, EventArgs e)
        {
            //update accounts info
            WantToFarm.Text = Convert.ToString(FarmManager.GetAutoFarmAccounts().Count);
            InQueue.Text = Convert.ToString(FarmManager.QueueCount); 
            Started.Text = Convert.ToString(FarmManager.StartedCount);
            Farmed.Text  = Convert.ToString(FarmManager.FarmedCount);
        }

        

        private void OnTextBoxMaxSameTimeLostFocus(object sender, RoutedEventArgs e)
        {
            Config.SaveMaxSameTimeAccounts(Convert.ToInt32(sameTimeAccounts.Text));
        }

        private void OnTextBoxWaitBeforeCloseLostFocus(object sender, RoutedEventArgs e)
        {
            Config.SaveWaitBeforeCloseAccounts(Convert.ToInt32(waitBeforeClose.Text));
        }

        private void OnTextBoxServersLostFocus(object sender, RoutedEventArgs e)
        {
            Config.SaveServersIp(ServerIp.Text);
        }

        private void OnFormLostFocus(object sender, RoutedEventArgs e)
        {
            timer.Stop();
        }
    }

}
