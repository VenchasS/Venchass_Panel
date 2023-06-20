using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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

namespace WPF_Vench_Launcher
{
    /// <summary>
    /// Логика взаимодействия для Settings.xaml
    /// </summary>
    public partial class Settings : Page
    {
        public Settings()
        {
            InitializeComponent();
            InitConfig();
        }

        public void InitConfig()
        {
            var config = Config.GetConfig();
            UpdateSteamPathTextBlockContent();
            csgoNews.IsChecked = (config.csgoNews);
            csgoNews.Checked += csgoNewsChecked;
            csgoNews.Unchecked += csgoNewsChecked;
            oldSteamVersion.IsChecked = Config.GetConfig().oldSteamVersion;
            oldSteamVersion.Checked += oldVersionSteamChecked;
            oldSteamVersion.Unchecked += oldVersionSteamChecked;
            version.Content = "ver. 1.6";
        }

        private void UpdateSteamPathTextBlockContent()
        {
            SteamPathTextBlock.Text = Config.GetConfig().SteamPath;
            CSGOPathTextBlock.Text = Config.GetConfig().CSGOPath;
        }

        private void RenameWindows(object sender, RoutedEventArgs e)
        {
            AccountManager.RenameWindows();
        }

        private void SendCMD(object sender, RoutedEventArgs e)
        {
            AccountManager.SendCmd(TextBoxToConsole.Text);
        }

        private void MoveWindows(object sender, RoutedEventArgs e)
        {
            AccountManager.AutoMoveWidnow();
        }

        private void OpenSDAFolder(object sender, RoutedEventArgs e)
        {
            Process.Start(Config.DirectoryPath + @"\Steam Desktop Authenticator\maFiles");
        }

        private void SelectSteamPath(object sender, RoutedEventArgs e)
        {
            Config.AskSteamPath();
            UpdateSteamPathTextBlockContent();
        }

        private void ImportAccountsFromFile(object sender, RoutedEventArgs e)
        {
            Config.ImportAccountsFromFile();
        }

        private void OpenLogs(object sender, RoutedEventArgs e)
        {
            var path = Config.DirectoryPath + @"\log.txt";
            if (File.Exists(path))
            {
                System.Diagnostics.Process.Start(path);
            }
        }

        private void SelectCSGOPath(object sender, RoutedEventArgs e)
        {
            Config.AskCSGOPath();
            UpdateSteamPathTextBlockContent();
        }

        private void csgoNewsChecked(object sender, RoutedEventArgs e)
        {
            var check = csgoNews.IsChecked == true;
            Config.SetCSGONews(check);
            Config.SaveCsgoNewsChecked(check);
        }

        private void oldVersionSteamChecked(object sender, RoutedEventArgs e)
        {
            var check = oldSteamVersion.IsChecked == true;
            Config.SaveOldSteamVersion(check);
        }
    }
}
