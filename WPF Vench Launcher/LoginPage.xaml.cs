﻿using System;
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

namespace WPF_Vench_Launcher
{
    /// <summary>
    /// Логика взаимодействия для LoginPage.xaml
    /// </summary>
    public partial class LoginPage : Page
    {
        Action callback;
        public LoginPage(Action signInCallback)
        {
            InitializeComponent();
            this.callback = signInCallback;
        }

        public async void SignInButtonClickAsync(object sender, EventArgs e)
        {
            await AccountManager.TrySignInAsync(Login.Text, Password.Text);
            if (AccountManager.GetIsSignedIn())
            {
                callback();
            }
            else
            {
                MessageBox.Show("Sign in error");
            }
        }

    }
}
