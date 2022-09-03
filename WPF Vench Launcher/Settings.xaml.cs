using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            UpdateSteamPathTExtBlockContent();
        }

        private void UpdateSteamPathTExtBlockContent()
        {
            SteamPathTextBlock.Text = Config.GetConfig().SteamPath;
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
            UpdateSteamPathTExtBlockContent();
        }
    }
}
