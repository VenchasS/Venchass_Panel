using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SteamAuth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
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

namespace WPF_Vench_Launcher.pages
{
    /// <summary>
    /// Логика взаимодействия для Home.xaml
    /// </summary>
    public partial class Home : Page
    {
        public CategoryHighlightStyleSelector CategoryHighlightStyleSelector;

        

        List<AccountsGroup> accountGroupsSource = new List<AccountsGroup>() { };

        private bool StartUpParamsChanged = false;
        private static bool timerStarted = false;
        public Home()
        {
            InitializeComponent();
            if(!timerStarted)
                InitTimer();
            InitConfig();
            timerTick(new object(),new EventArgs());
        }

        public void InitConfig()
        {
            //Config.LoadAccountsData();

            accountsList.ItemsSource = AccountManager.GetAccountsBase();
            var config = Config.GetConfig();
            if (config.Groups != null)
                accountGroupsSource = config.Groups;
            if (config.StartParams != null)
                startupParams.Text = config.StartParams;
            AccountGroups.ItemsSource = accountGroupsSource;
        }


        private System.Windows.Threading.DispatcherTimer InitTimer()
        {
            timerStarted= true;
            System.Windows.Threading.DispatcherTimer timer = new System.Windows.Threading.DispatcherTimer();
            timer.Tick += new EventHandler(timerTick);
            timer.Interval = new TimeSpan(0, 0, 5);
            timer.Start();
            return timer;
        }

        private void timerTick(object sender, EventArgs e)
        {
            try
            {
                AccountsTotal.Text = AccountManager.GetAccountsBase().Count().ToString();
                AccountsStarted.Text = AccountManager.GetAccountsBase().Where(x => x.Status == 2).Count().ToString();
                PrimeTotal.Text = AccountManager.GetAccountsBase().Where(x => x.PrimeStatus).Count().ToString();
                UpdateTable();
            }
            catch(Exception ex)
            {
                AccountManager.SaveLogInfo(ex.Message);
            }
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
            account.PrimeStatus = PrimeCheck.IsChecked == true;
            AccountManager.AddAccount(account);
            UpdateTable();
        }
        private void ButtonDeleteAccountClick(object sender, RoutedEventArgs e)
        {
            var s = sender as Button;
            var acc = (Account)s.DataContext;

            var selectedList = new List<Account>();
            var selectedAccounts = accountsList.SelectedItems;
            lock (selectedAccounts)
            {
                foreach (var accountObj in selectedAccounts)
                {
                    var account = accountObj as Account;
                    selectedList.Add(account);
                }
            }
            
            AccountManager.DeleteAccount(acc);
            ClearSelected();
            foreach (var account in selectedList)
            {
                for (int i = 0; i < accountsList.Items.Count; i++)
                {
                    if (((Account)accountsList.Items[i]).Login == account.Login)
                        accountsList.SelectedItems.Add(accountsList.Items[i]);
                }
            }
            UpdateTable();
        }

        private void ButtonOpenSteamClick(object sender, RoutedEventArgs e)
        {
            var s = sender as Button;
            var acc = (Account)s.DataContext;
            AccountManager.OpenSteam(acc);
            Config.SaveAccountsDataAsync(acc);
        }

        private void ButtonStartClick(object sender, RoutedEventArgs e)
        {
            var selectedAccounts = accountsList.SelectedItems;
            List<Account> accounts = new List<Account>();
            lock (selectedAccounts)
            {
                foreach (var accountObj in selectedAccounts)
                {
                    var account = accountObj as Account;
                    accounts.Add(account);
                    AccountManager.StartAccount(account, startupParams.Text);
                }
            }
            Config.SaveAccountsDataAsync(accounts);
            ClearSelected();
            UpdateTable();
        }

        private void ButtonExportClick(object sender, RoutedEventArgs e)
        {
            var selectedAccounts = accountsList.SelectedItems;
            var fileText = "";
            lock (selectedAccounts)
            {
                foreach (var accountObj in selectedAccounts)
                {
                    var account = accountObj as Account;
                    fileText += account.Login + ":" + account.Password + "\n";
                }
            }
            System.Windows.Forms.SaveFileDialog sfd = new System.Windows.Forms.SaveFileDialog();
            sfd.Filter = "Text files(*.txt)|*.txt|All files(*.*)|*.*";
            if (sfd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                System.IO.File.WriteAllText(sfd.FileName, fileText);
            }


/*
            var selectedAccounts = accountsList.SelectedItems;
            var fileText = "";
            lock (selectedAccounts)
            {
                foreach (var accountObj in selectedAccounts)
                {
                    var account = accountObj as Account;
                    fileText = "{\r\n  \"Enabled\": true,\r\n  \"Paused\": true,\r\n  \"SteamLogin\": \"" + account.Login + "\",\r\n  \"SteamPassword\": \"" + account.Password + "\",\r\n  \"SteamTradeToken\": \"aMLn_QlD\",\r\n  \"SteamUserPermissions\": {\r\n    \"76561199149809591\": 3\r\n  }\r\n}";
                    System.IO.File.WriteAllText("C:\\Users\\PC1\\Desktop\\buyed_accs_2\\" + account.Login + ".json", fileText);
                }
            }*/

        }

        private void ButtonImportClick(object sender, RoutedEventArgs e)
        {
            Config.ImportAccountsFromFile();
            UpdateTable();
            UpdateGroupsList();
        }

        private void OnTestButtonclick(object sender, RoutedEventArgs e)
        {
            //AccountManager.UpdateAccountsChildrens();
            //AccountManager.GetWindowHandles();
            //AccountManager.SendCmd();
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
        private void StopAllButtonClick(object sender, RoutedEventArgs e)
        {
            AccountManager.StopAllAccounts();
            Config.SaveAccountsDataAsync(AccountManager.GetAccountsBase());
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
            if(!Keyboard.IsKeyDown(Key.LeftShift))
                ClearSelected();
            var selected = (AccountsGroup)AccountGroups.SelectedItem;
            foreach (var acc in selected.InGroupAccounts())
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

        public void UpdateGroupsList()
        {
            AccountGroups.ItemsSource = Config.GetConfig().Groups;
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

        private void FarmSelectedButtonClick(object sender, RoutedEventArgs e)
        {
            var selectedAccs = accountsList.SelectedItems;
            List<Account> list = new List<Account>();
            foreach (var acc in selectedAccs)
            {
                list.Add((Account)acc);
            }
            FarmManager.AutoFarm(list);
            ClearSelected();
        }

        private void AutoFarmButtonClick(object sender, RoutedEventArgs e)
        {
            FarmManager.AutoFarm(FarmManager.GetAutoFarmAccounts());
        }
        private void PrimeCheck_Checked(object sender, RoutedEventArgs e)
        {
            var selectedAccs = accountsList.SelectedItems;
            foreach (Account acc in selectedAccs)
            {
                acc.PrimeStatus = PrimeCheck.IsChecked == true;
                Config.SaveAccountsDataAsync(acc);
            }
        }

        private void SendTradesButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedAccs = accountsList.SelectedItems;
            foreach (Account acc in selectedAccs)
            {
                TraderController.AddAccount(acc);
            }

        }
        private void SendFriendsButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedAccs = accountsList.SelectedItems;
            var accs = new List<Account>();
            foreach (Account acc in selectedAccs)
            {
                accs.Add(acc);
            }
            TraderController.sendInvites(accs, Config.GetConfig().FriendLogin.ToLower());

        }
        
    }

    public class CategoryHighlightStyleSelector : StyleSelector
    {
        public Style IsStartedStyle { get; set; }
        public Style IsntStartedStyle { get; set; }

        public Style IsStartedCSGO { get; set; }

        private Style NotStartedNonPrime;

        private Style StartedNonPrime;
        public CategoryHighlightStyleSelector()
        {

        }

        public override Style SelectStyle(object item, DependencyObject container)
        {
            Account acc = (Account)item;
            switch (acc.Status)
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
        public List<Account> InGroupAccounts()
        {
            List<Account> res = new List<Account>();
            List<string> logins = AccountManager.GetAccountsBase().Select(x => x.Login).ToList();
            foreach (var item in AccountLogins)
            {
                if (logins.Contains(item))
                {
                    foreach (var acc in AccountManager.GetAccountsBase())
                    {
                        if (acc.Login == item)
                        {
                            res.Add(acc);
                            break;
                        }

                    }
                }
            }
            return res;
        }

        public string GroupName { get; set; }

        public List<string> AccountLogins
        {
            get; set;
        }

        public AccountsGroup()
        {

        }

        public AccountsGroup(List<Account> inGroupAccounts, string groupName)
        {
            this.GroupName = groupName;
            this.AccountLogins = inGroupAccounts.Select(x => x.Login).ToList();
        }
    }
}
