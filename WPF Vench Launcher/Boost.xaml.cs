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

//Software by Venchass
//My:
//Discord VenchasS#9039
//telegram @VenchasS
//VK https://vk.com/venchass

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
            try
            {
                var config = Config.LoadConfig();
                sameTimeAccounts.Text = Convert.ToString(Config.GetConfig().MaxSameTimeAccounts);
                waitBeforeClose.Text = Convert.ToString(Config.GetConfig().MaxRemainingTimeToDropCase);
                ServerIp.Text = Convert.ToString(Config.GetConfig().CustomPanelIp);

                InQueue.Text = Convert.ToString(FarmManager.QueueCount);
                Started.Text = Convert.ToString(FarmManager.StartedCount);
                Farmed.Text = Convert.ToString(FarmManager.FarmedCount);
                LaunchDelay.Text = Convert.ToString(config.launchDelay);
                tradeLink.Text = Convert.ToString(config.TradeLink);
                TradesCheck.IsChecked = Config.GetConfig().TradesCheckbox;
                TradesCheck.Checked += TradesCheck_Checked;
                TradesCheck.Unchecked += TradesCheck_Checked;
                MarkTimeLimit.IsChecked = Config.GetConfig().MarkLimitCheckbox;
                MarkTimeLimit.Checked += MarkLimitChecked;
                MarkTimeLimit.Unchecked += MarkLimitChecked;
                FriendLogin.Text = Convert.ToString(Config.GetConfig().FriendLogin);
                ConnectedPanelLabel.Content = "Checking walkbot available";
                Task.Run(() =>
                {
                    var enabled = PanelManager.isEnabled();
                    Dispatcher.Invoke((Action)(() =>
                    {
                        ConnectedPanelLabel.Content = enabled ? "walkbot is available" : "walkbot not available";
                    }));
                });
            }
            catch (Exception e)
            {
                AccountManager.SaveLogInfo(e.Message);
            }
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

        private void OnTextBoxDelayLostFocus(object sender, RoutedEventArgs e)
        {
            int seconds;
            if (Int32.TryParse(LaunchDelay.Text, out seconds))
            {
                Config.SaveLaunchDelay(seconds);
            }
        }

        private void tradeLinkLostFocus(object sender, RoutedEventArgs e)
        {
            Config.SaveTradeLink(tradeLink.Text);
        }

        private void OnFormLostFocus(object sender, RoutedEventArgs e)
        {
            timer.Stop();
        }

        private void TradesCheck_Checked(object sender, RoutedEventArgs e)
        {
            var value = TradesCheck.IsChecked == true;
            Config.SaveTradesCheckbox(value);
        }

        private void MarkLimitChecked(object sender, RoutedEventArgs e)
        {
            var value = MarkTimeLimit.IsChecked == true;
            Config.SaveMarkLimitCheckbox(value);
        }
        private void FriendLoginLostFocus(object sender, RoutedEventArgs e)
        {
            Config.SaveFriendLogin(FriendLogin.Text);
        }

        private void DiscordImageClick(object sender, System.Windows.Input.MouseEventArgs e)
        {
            Tools.openUrl("https://discord.gg/3D9SVyBkjA");
        }

        private void DonateClick(object sender, MouseButtonEventArgs e)
        {
            Tools.openUrl("https://steamcommunity.com/tradeoffer/new/?partner=210495666&token=saTO1I-x");
        }

        private void WalkBotClick(object sender, MouseButtonEventArgs e)
        {
            Tools.openUrl("https://disk.yandex.ru/d/7kqjNZuyPlZKNQ");
        }
    }

}
