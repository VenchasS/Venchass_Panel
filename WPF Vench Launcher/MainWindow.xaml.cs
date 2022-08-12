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
using System.Linq;

namespace WPF_Vench_Launcher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>

    public partial class MainWindow : Window
    {
        public CategoryHighlightStyleSelector CategoryHighlightStyleSelector;

        private bool StartUpParamsChanged = false;

        public MainWindow()
        {
            InitializeComponent();
            InitializeFolder();
            InitConfig();
            var timer = InitTimer();
            CustomTitle.MouseLeftButtonDown += new MouseButtonEventHandler(MoveWindow);
            LogoLabel.MouseLeftButtonDown += new MouseButtonEventHandler(MoveWindow);
            this.MouseLeftButtonDown += new MouseButtonEventHandler(window_MouseDown);
        }
        private void window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            AccountLogin.Focus();
            Keyboard.ClearFocus();
        }
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void HideButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }
        

        void MoveWindow(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private System.Windows.Threading.DispatcherTimer InitTimer()
        {
            System.Windows.Threading.DispatcherTimer timer = new System.Windows.Threading.DispatcherTimer();
            timer.Tick += new EventHandler(timerTick);
            timer.Interval = new TimeSpan(0, 0, 2);
            timer.Start();
            return timer;
        }

        private void timerTick(object sender, EventArgs e)
        {
            //update accounts info
            AccountsTotal.Text = accountsList.Items.Count.ToString();
            AccountsStarted.Text = AccountManager.GetAccountsBase().Where(x => x.Status == 2).Count().ToString();
            Thread th = new Thread(AccountManager.UpdateAccountsChildrens);
            Thread th2 = new Thread(AccountManager.SdaCheck);
            th.Start();
            th2.Start();
            //AccountManager.UpdateAccountsChildrens();
            //AccountManager.SdaCheck();
            
            UpdateTable();
        }
        List<AccountsGroup> accountGroupsSource = new List<AccountsGroup>() { new AccountsGroup(AccountManager.GetAccountsBase(), "all accs") };
        public void InitConfig()
        {
            Config.LoadAccountsData();
            accountsList.ItemsSource = AccountManager.GetAccountsBase();
            var cofig = Config.LoadConfig();
            accountGroupsSource = cofig.Groups;
            startupParams.Text = cofig.StartParams;
            AccountGroups.ItemsSource = accountGroupsSource;
        }

        private void CheckFile(string name)
        {
            var currentDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"/VenchassPanel";
            if (!File.Exists(currentDirectory + name))
                File.Create(currentDirectory + name);
        }

        void InitializeFolder()
        {
            var currentDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"/VenchassPanel";
            Config.DirectoryPath = currentDirectory;
            var dir = new DirectoryInfo(currentDirectory);
            if (!dir.Exists)
                dir.Create();

            CheckFile(@"/Accounts.cfg");
            CheckFile(@"/config.cfg");
        }

        public void UpdateTable()
        {
            accountsList.ItemsSource = AccountManager.GetAccountsBase();
            StyleSelector selector = accountsList.ItemContainerStyleSelector;
            accountsList.ItemContainerStyleSelector = null;
            accountsList.ItemContainerStyleSelector = selector;
        }

        public void ClearSelected()
        {
            accountsList.UnselectAll();
        }

        private void ButtonAddClick(object sender, RoutedEventArgs e)
        {
            var account = new Account(AccountLogin.Text, AccountPassword.Text);
            AccountManager.AddAccount(account);
            Config.SaveAccountsDataAsync();
            UpdateTable();
        }


        private void ButtonStartClick(object sender, RoutedEventArgs e)
        {
            var selectedAccounts = accountsList.SelectedItems;
            foreach (var item in selectedAccounts)
            {
                AccountManager.StartAccount((Account)item, startupParams.Text);
            }
            Config.SaveAccountsDataAsync();
            ClearSelected();
            UpdateTable();
        }

        private void OnTestButtonclick(object sender, RoutedEventArgs e)
        {
            //AccountManager.UpdateAccountsChildrens();
            //AccountManager.GetWindowHandles();
            var selected = (AccountsGroup)AccountGroups.SelectedItem;
            foreach (var acc in selected.InGroupAccounts)
            {
                for (int i = 0; i < accountsList.Items.Count; i++)
                {
                    if( ((Account)accountsList.Items[i]).Login == acc.Login)
                        accountsList.SelectedItems.Add(accountsList.Items[i]);
                }
            }
            UpdateTable();
        }

        private void ButtonStopClick(object sender, RoutedEventArgs e)
        {
            var selectedAccounts = accountsList.SelectedItems;
            foreach (var item in selectedAccounts)
            {
                AccountManager.StopAccount((Account)item);
            }
            ClearSelected();
            UpdateTable();
        }

        private void OnTextBoxStartupParamsChanged(object sender, RoutedEventArgs e)
        {
            StartUpParamsChanged = true;
        }

        private void OnTextBoxStartupParamsLostFocus(object sender, RoutedEventArgs e)
        {
            if (StartUpParamsChanged)
            {
                Config.SaveStartUpParams(startupParams.Text);
            }
            StartUpParamsChanged = false;
            //save startupParams;
        }

        private void AccountGroups_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (AccountGroups.SelectedItem == null)
                return;
            ClearSelected();
            var selected = (AccountsGroup)AccountGroups.SelectedItem;
            foreach (var acc in selected.InGroupAccounts)
            {
                for (int i = 0; i < accountsList.Items.Count; i++)
                {
                    if (((Account)accountsList.Items[i]).Login == acc.Login)
                        accountsList.SelectedItems.Add(accountsList.Items[i]);
                }
            }
            AccountGroups.UnselectAll();
            //UpdateTable();
        }

        private void MakeGroupButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedAccs = accountsList.SelectedItems;
            var groupAccounts = new List<Account>();
            foreach (var acc in selectedAccs)
            {
                groupAccounts.Add((Account)acc);
            }
            var group = new AccountsGroup(groupAccounts, "new groupe");
            accountGroupsSource.Add(group);
            var newG = new List<AccountsGroup>(accountGroupsSource);
            AccountGroups.ItemsSource = newG;
            Config.SaveGroupsParams(accountGroupsSource);
        }

        private void DeleteGroup(object sender, RoutedEventArgs e)
        {
            var s = sender as Button;
            
            var group = s.DataContext;
            accountGroupsSource.Remove((AccountsGroup)group);
            var newG = new List<AccountsGroup>(accountGroupsSource);
            AccountGroups.ItemsSource = newG;
            Config.SaveGroupsParams(accountGroupsSource);
        }

        private void GroupNameLostFocus(object sender, RoutedEventArgs e)
        {
            Config.SaveGroupsParams(accountGroupsSource);
        }

    }

    public class CategoryHighlightStyleSelector : StyleSelector
    {
        public Style IsStartedStyle { get; set; }
        public Style IsntStartedStyle { get; set; }

        public Style IsStartedCSGO { get; set; }

        public override Style SelectStyle(object item, DependencyObject container)
        {
            Account car = (Account)item;

            switch (car.Status)
            {
                case 0:
                    return IsntStartedStyle;
                case 1:
                    return IsStartedStyle;
                case 2:
                    return IsStartedCSGO;
                default:
                    return null;
            }
        }
    }

    public class AccountsGroup
    {
        public List<Account> InGroupAccounts = new List<Account>();

        public string GroupName { get; set; }

        public List<string> AccountLogins { get { return InGroupAccounts.Select(x => x.Login).ToList(); } set {
               List<string> logins = AccountManager.GetAccountsBase().Select(x => x.Login).ToList();
            foreach (var item in value)
                {
                    if(logins.Contains(item))
                    {
                        foreach(var acc in AccountManager.GetAccountsBase())
                        {
                            if (acc.Login == item)
                            {
                                InGroupAccounts.Add(acc);
                                break;
                            }

                        }
                    }
                }
            } }

        public AccountsGroup()
        {

        }

        public AccountsGroup(List<Account> inGroupAccounts, string groupName)
        {
            this.GroupName = groupName;
            this.InGroupAccounts = inGroupAccounts;
        }
    }
}
