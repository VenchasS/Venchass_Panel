using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Principal;
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
using WPF_Vench_Launcher.Sources;


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
            CheckStartedApps();
            InitializeComponent();
            CheckAdminRole();
            Loaded += MainWindow_Loaded;
            Closing += MainWindow_Closing;
            InitConfig();
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                InitEvents();
                UpdateAccountsProcessesInfoThread();
                Config.OptimizePanorama(true);
                TraderController.TraderMainThread();
            }
            catch (Exception ex)
            {
                AccountManager.SaveLogInfo(ex.Message);
            }
            AccountManager.SaveLogInfo("Panel started");
        }

        void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            //Your code to handle the event
            Config.OptimizePanorama(false);
        }

        private void InitDataBase()
        {
            //throw new NotImplementedException();
        }

        private void CheckAdminRole()
        {
            bool isElevated;
            using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
            {
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                isElevated = principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            if(!isElevated)
            {
                MessageBox.Show("This app should run as administrator");
                Close();
            }
        }

        private void CheckStartedApps()
        {
            var hwnd = AccountManager.FindWindow(null, "Venchass Panel");
            if(hwnd != null && hwnd != IntPtr.Zero)
            {
                AccountManager.SetForegroundWindow(hwnd);
                Close();
            }
        }


        /// <summary>
        /// Read all cfg files
        /// </summary>
        public void InitConfig()
        {
            InitializeFolder();
            config = Config.LoadConfig();
            AccountManager.SetSteamPath(config.SteamPath);
            Config.InitCSGOconfig(Properties.Resources.Venchcfg);
            Config.InitDataBase();
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
                        AccountManager.SaveLogInfo(ex.Message);
                    }
                }
            });
        }

        void MoveWindow(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        private void window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Keyboard.ClearFocus();
        }

        

        private void CheckFile(string name)
        {
            var currentDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"/VenchassPanel";
            if (!File.Exists(currentDirectory + name))
                File.Create(currentDirectory + name).Close();
        }

        private void CheckFolder(string name)
        {
            var currentDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"/VenchassPanel";
            if (!System.IO.Directory.Exists(currentDirectory + name))
                System.IO.Directory.CreateDirectory(currentDirectory + name);
        }

        void InitializeFolder()
        {
            var currentDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\VenchassPanel";
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
