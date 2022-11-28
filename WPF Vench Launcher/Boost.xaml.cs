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

namespace WPF_Vench_Launcher
{
    /// <summary>
    /// Логика взаимодействия для Boost.xaml
    /// </summary>
    public partial class Boost : Page
    {
        public Selector selector;

        List<AccountsGroup> accountGroupsSource = new List<AccountsGroup>() { new AccountsGroup(AccountManager.GetAccountsBase(), "all accounts") };

        public Boost()
        {
            InitializeComponent();
            accountsList.ItemsSource = AccountManager.GetAccountsBase();
            var config = Config.GetConfig();
            if (config.Groups != null)
                accountGroupsSource = config.Groups;
            AccountGroups.ItemsSource = accountGroupsSource;
        }

        public void UpdateTable()
        {
            accountsList.ItemsSource = AccountManager.GetAccountsBase();
            StyleSelector selector = accountsList.ItemContainerStyleSelector;
            accountsList.ItemContainerStyleSelector = null;
            accountsList.ItemContainerStyleSelector = selector;
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

        private void GroupNameLostFocus(object sender, RoutedEventArgs e)
        {
            Config.SaveGroupsParams(accountGroupsSource);
        }

        public void ClearSelected()
        {
            accountsList.UnselectAll();
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

        private void StartBiistButtonClick(object sender, RoutedEventArgs e)
        {
            var selectedAccs = accountsList.SelectedItems;
            List<Account> list = new List<Account>();
            foreach (var acc in selectedAccs)
            {
                list.Add((Account)acc);
            }
            if (!BoostManager.MakeBoostGroup(list))
            {
                throw new Exception("can not make group");
            }
            BoostManager.GetGroups().Last().Start();
        }
    }

    public class Selector : StyleSelector
    {
        public Style IsStartedStyle { get; set; }
        public Style IsntStartedStyle { get; set; }

        public Style IsStartedCSGO { get; set; }

        private Style NotStartedNonPrime;

        private Style StartedNonPrime;
        public Selector()
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

}
