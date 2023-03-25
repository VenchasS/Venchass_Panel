using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
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


//Software by Venchass
//My:
//Discord VenchasS#9039
//telegram @VenchasS
//VK https://vk.com/venchass

namespace WPF_Vench_Launcher
{
    public partial class MainWindow : Window
    {
        private ConfigObject config;
        public MainWindow()
        {
            try
            {
                InitializeComponent();
                InitializeFolder();
                InitConfig();
                InitEvents();
                UpdateAccountsProcessesInfoThread();
            }
            catch(Exception e)
            {
                AccountManager.SaveLogInfo(e.Message);
            }
            
            AccountManager.SaveLogInfo("Panel started");
            /*MainFrame.Navigate(new LoginPage(() =>
            {
                MainFrame.Source = new Uri("Home.xaml", UriKind.Relative);
            }));*/
        }

        private void InitDataBase()
        {
            //throw new NotImplementedException();
        }



        /// <summary>
        /// Read all cfg files
        /// </summary>
        public void InitConfig()
        {
            Config.LoadAccountsData();
            config = Config.LoadConfig();
            AccountManager.SetSteamPath(config.SteamPath);
            Config.InitCSGOconfig(Properties.Resources.Venchcfg);
        }


        void InitEvents()
        {
            CustomTitle.MouseLeftButtonDown += new MouseButtonEventHandler(MoveWindow);
            LogoLabel.MouseLeftButtonDown += new MouseButtonEventHandler(MoveWindow);
            this.MouseLeftButtonDown += new MouseButtonEventHandler(window_MouseDown);
        }

        public void UpdateAccountsProcessesInfoThread()
        {
            Task.Factory.StartNew(() => {
                
                while (true)
                {
                    try
                    {

                        AccountManager.UpdateAccountsChildrens();
                        
                        AccountManager.SdaCheck();
                        Thread.Sleep(2000);
                    }
                    catch (Exception ex) {
                        //MessageBox.Show(ex.Message);
                    }
                }
            });
        }

        void MoveWindow(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            //AccountLogin.Focus();
            Keyboard.ClearFocus();
        }

        private void CheckFile(string name)
        {
            var currentDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"/VenchassPanel";
            if (!File.Exists(currentDirectory + name))
                File.Create(currentDirectory + name);
        }

        private void CheckFolder(string name)
        {
            var currentDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"/VenchassPanel";
            if (!System.IO.Directory.Exists(currentDirectory + name))
                System.IO.Directory.CreateDirectory(currentDirectory + name);
        }

        void InitializeFolder()
        {
            var currentDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"/VenchassPanel";
            Config.SetDirectoryPath(currentDirectory);
            var dir = new DirectoryInfo(currentDirectory);
            if (!dir.Exists)
                dir.Create();

            CheckFile(@"/Accounts.cfg");
            CheckFile(@"/config.cfg");
            CheckFile(@"/log.txt");
            CheckFolder(@"/Steam Desktop Authenticator");
            CheckFolder(@"/Steam Desktop Authenticator/maFiles");
        }
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void HideButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void HomeuButtonClick(object sender, RoutedEventArgs e)
        {
            if(AccountManager.GetIsSignedIn())
                MainFrame.Source = new Uri("Home.xaml", UriKind.Relative);
        }

        private void SettingsButtonClick(object sender, RoutedEventArgs e)
        {
            if (AccountManager.GetIsSignedIn())
                MainFrame.Source = new Uri("Settings.xaml", UriKind.Relative);
        }

        private void BoostButtonClick(object sender, RoutedEventArgs e)
        {
            if (AccountManager.GetIsSignedIn())
                MainFrame.Source = new Uri("Boost.xaml", UriKind.Relative);
        }

        private void LoginPage(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new LoginPage(() =>
            {
                MainFrame.Source = new Uri("Home.xaml", UriKind.Relative);
            }));
        }
    }

    
}
