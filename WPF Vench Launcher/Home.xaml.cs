using System;
using System.Collections.Generic;
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

namespace WPF_Vench_Launcher.pages
{
    /// <summary>
    /// Логика взаимодействия для Home.xaml
    /// </summary>
    public partial class Home : Page
    {
        public CategoryHighlightStyleSelector CategoryHighlightStyleSelector;

        List<AccountsGroup> accountGroupsSource = new List<AccountsGroup>() { new AccountsGroup(AccountManager.GetAccountsBase(), "all accounts") };

        private bool StartUpParamsChanged = false;

        public Home()
        {
            InitializeComponent();
            var timer = InitTimer();
            InitConfig();
            AccountsTotal.Text = accountsList.Items.Count.ToString();
            AccountsStarted.Text = AccountManager.GetAccountsBase().Where(x => x.Status == 2).Count().ToString();
            PrimeTotal.Text = AccountManager.GetAccountsBase().Where(x => x.PrimeStatus).Count().ToString();
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
            System.Windows.Threading.DispatcherTimer timer = new System.Windows.Threading.DispatcherTimer();
            timer.Tick += new EventHandler(timerTick);
            timer.Interval = new TimeSpan(0, 0, 5);
            timer.Start();
            return timer;
        }

        private void timerTick(object sender, EventArgs e)
        {
            //update accounts info
            AccountsTotal.Text = accountsList.Items.Count.ToString();
            AccountsStarted.Text = AccountManager.GetAccountsBase().Where(x => x.Status == 2).Count().ToString();
            PrimeTotal.Text = AccountManager.GetAccountsBase().Where(x => x.PrimeStatus).Count().ToString();
            //Task.WhenAll(new List<Task>() { Task.Run(AccountManager.UpdateAccountsChildrens), Task.Run(AccountManager.SdaCheck) });
            /*Thread th = new Thread(AccountManager.UpdateAccountsChildrens);
            Thread th2 = new Thread(AccountManager.SdaCheck);
            th.Start();
            th2.Start();*/
            UpdateTable();
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
            if (PrimeCheck.IsChecked == true)
                account.PrimeStatus = true;
            AccountManager.AddAccount(account);
            Config.SaveAccountsDataAsync();
            UpdateTable();
        }
        private void ButtonDeleteAccountClick(object sender, RoutedEventArgs e)
        {
            var s = sender as Button;
            var acc = (Account)s.DataContext;
            AccountManager.DelteAccount(acc);
            Config.SaveAccountsDataAsync();
            UpdateTable();
        }

        private void ButtonOpenSteamClick(object sender, RoutedEventArgs e)
        {
            var s = sender as Button;
            var acc = (Account)s.DataContext;
            AccountManager.OpenSteam(acc);
            Config.SaveAccountsDataAsync();
        }

        private void ButtonStartClick(object sender, RoutedEventArgs e)
        {
            var selectedAccounts = accountsList.SelectedItems;
            lock (selectedAccounts)
            {
                foreach (var item in selectedAccounts)
                {
                    AccountManager.StartAccount((Account)item, startupParams.Text);
                }
            }
            Config.SaveAccountsDataAsync();
            ClearSelected();
            UpdateTable();
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
        public List<Account> InGroupAccounts = new List<Account>();

        public string GroupName { get; set; }

        public List<string> AccountLogins
        {
            get { return InGroupAccounts.Select(x => x.Login).ToList(); }
            set
            {
                List<string> logins = AccountManager.GetAccountsBase().Select(x => x.Login).ToList();
                foreach (var item in value)
                {
                    if (logins.Contains(item))
                    {
                        foreach (var acc in AccountManager.GetAccountsBase())
                        {
                            if (acc.Login == item)
                            {
                                InGroupAccounts.Add(acc);
                                break;
                            }

                        }
                    }
                }
            }
        }

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
