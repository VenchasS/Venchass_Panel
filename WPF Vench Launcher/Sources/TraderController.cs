using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WPF_Vench_Launcher.Sources
{
    public class TraderController
    {
        public static int threadSleepTimer = 15000;

        private static List<Account> accounts = new List<Account>();

        public static void AddAccount(Account account)
        {
            if (SteamGuard.HasGuard(account.Login.ToLower()))
            {
                lock (accounts)
                {
                    if(!accounts.Select(x => x.Login).Contains(account.Login))
                        accounts.Add(account);
                }
            }
        }

        public static void DeleteAccount(Account account)
        {
            lock (accounts)
            {
                if (accounts.Contains(account))
                    accounts.Remove(account);
            }
        }

        private static void StartProgramm(string path, string paramsString, int timeToExecute = 600000)
        {
            var args = paramsString;
            Process p = new Process();
            p.StartInfo.FileName = "cmd.exe";
            p.StartInfo.Arguments = "/c node " + path + args;
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.CreateNoWindow = true;

            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.RedirectStandardInput = true;

            p.OutputDataReceived += (s, e) =>
                AccountManager.SaveLogInfo(e.Data);
            p.ErrorDataReceived += (s, e) =>
                AccountManager.SaveLogInfo(e.Data);
            p.Start();
            p.BeginOutputReadLine();
            p.BeginErrorReadLine();
            p.WaitForExit(timeToExecute);
            p.Close();
            AccountManager.SaveLogInfo("Invite process " + " ended");
        }

        private static void SendTrade(Account account, int timeToTrade = 60000)
        {
            var login = account.Login.ToLower();
            var pass = account.Password;
            var shared_secret = SteamGuard.GetsharedSecret(login);
            var identify_secret = SteamGuard.GetIdentitySecret(login);
            if(shared_secret == null || identify_secret == null) { return; }
            var tradeLink = Config.GetConfig().TradeLink;
            if(tradeLink == null || tradeLink == "") { return; }
            var path = "CaseTrader\\case-trader.js ";
            var args = String.Format(" {0} \"{1}\" {2} {3} 1 {4}", login, pass, shared_secret, identify_secret, tradeLink.Replace("&token", "^&token"));
            Process p = new Process();
            p.StartInfo.FileName = "cmd.exe";
            p.StartInfo.Arguments = "/c node " + path + args;
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.CreateNoWindow= true;

            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.RedirectStandardInput = true;

            p.OutputDataReceived += (s, e) => 
                AccountManager.SaveLogInfo(e.Data);
            p.ErrorDataReceived += (s, e) => 
                AccountManager.SaveLogInfo(e.Data);
            p.Start();
            p.BeginOutputReadLine();
            p.BeginErrorReadLine();
            p.WaitForExit(timeToTrade);
            p.Close();
            AccountManager.SaveLogInfo("Trade process " + login + " ended");
            DeleteAccount(account);
        }

        public static void TraderMainThread()
        {
            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    Thread.Sleep(threadSleepTimer);
                    if (accounts.Count == 0 || Config.GetConfig().TradeLink == "")
                        continue;
                    var acc = accounts.First();
                    SendTrade(acc);
                }
            });
        }

        public static void sendInvites(List<Account> accs, string mainLogin)
        {
            if (mainLogin == "" || mainLogin == null) return;
            var mainAcc = AccountManager.GetAccountsBase().Where(x => x.Login.ToLower() == mainLogin.ToLower()).ToList();
            if (mainAcc.Count() == 0)
            {
                return;
            }
            var paramsString = "";
            accs.Insert(0, mainAcc[0]);
            foreach (var acc in accs)
            {
                if (SteamGuard.HasGuard(acc.Login.ToLower()))
                {
                    paramsString += String.Format(" {0} \"{1}\" {2} ", acc.Login, acc.Password, SteamGuard.GetsharedSecret(acc.Login.ToLower()));
                } else
                {
                    paramsString += String.Format(" {0} \"{1}\" 0 ", acc.Login, acc.Password);
                }
            }
            Task.Factory.StartNew(() =>
            {
                StartProgramm("CaseTrader\\tools.js", paramsString);
            });
        }
    }
}
