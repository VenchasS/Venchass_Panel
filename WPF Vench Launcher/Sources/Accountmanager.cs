using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Text.Json;
using System.Windows;
using System.Runtime.InteropServices;
using System.Text;
using SteamAuth;
using Newtonsoft.Json;
using System.Threading;

namespace WPF_Vench_Launcher
{
    
    public class AccountManager
    {
        delegate bool EnumThreadDelegate(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        static extern bool EnumThreadWindows(int dwThreadId, EnumThreadDelegate lpfn, IntPtr lParam);

        [DllImport("user32.dll")]
        static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern int GetWindowTextLength(IntPtr hWnd);

        static Dictionary<Account, Process> StartedAccountsDict = new Dictionary<Account, Process>();

        static string steamPath = @"C:\Program Files (x86)\Steam";

        static List<Account> AccountsBase = new List<Account>();
        public static void StartAccount(Account account, string startParams = "")
        {
            lock (account)
            {
                string path = steamPath + @"\steam.exe";
                ProcessStartInfo processStartInfo = new ProcessStartInfo()
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    WorkingDirectory = steamPath,
                    FileName = path,
                    Arguments = string.Format(" -login {0} {1}  -applaunch 730  {2} ", account.Login, account.Password, startParams)
                };
                Process process = new Process()
                {
                    StartInfo = processStartInfo
                };
                process.Start();
                account.Status = 1;
                account.PID = process.Id;
                SaveAccountData(account, process); //save started acc
            }
        }

        private static Process GetProcessByAccount(Account acc)
        {
            return StartedAccountsDict[acc];
        }

        public static void StopAccount(Account acc)
        {
            
            if (acc.Status == 2)
            {
                try
                {
                    var game = GetGameProcess(acc);
                    game.Kill();
                }
                catch
                {

                }
            }
            lock (StartedAccountsDict)
            {
                if (!StartedAccountsDict.ContainsKey(acc))
                    return;
                var proc = StartedAccountsDict[acc];
                proc.Kill();
                StartedAccountsDict.Remove(acc);
                acc.Status = 0;
            }
        }

        static void SaveAccountData(Account acc, Process proc)
        {
            lock (StartedAccountsDict)
            {
                StartedAccountsDict.Add(acc, proc);
            }
        }

        public static List<Account> GetAccountsBase()
        {
            return AccountsBase;
        }

        public static void AddAccount(Account acc)
        {
            AccountsBase.Add(acc);
            if (acc.PID != 0)
            {
                try
                {
                    var proc = Process.GetProcessById(acc.PID);
                    SaveAccountData(acc, proc);
                }
                catch
                {
                    acc.Status = 0;
                    acc.PID = 0;
                }
            }
        }

        public static List<Process> GetChildrens(Account account)
        {
            lock (StartedAccountsDict)
            {
                var children = new List<Process>();
                if (StartedAccountsDict.ContainsKey(account))
                {
                    var process = StartedAccountsDict[account];
                    var mos = new ManagementObjectSearcher(String.Format($"Select * From Win32_Process Where ParentProcessID={process.Id}"));

                    foreach (ManagementObject mo in mos.Get())
                    {
                        try
                        {
                            children.Add(Process.GetProcessById(Convert.ToInt32(mo["ProcessID"])));
                        }
                        catch
                        {

                        }
                    }

                }
                return children;
            }
        }

        public static Process GetGameProcess(Account acc)
        {
            var childrens = GetChildrens(acc);
            foreach (var process in childrens)
            {
                if (process.ProcessName == "csgo")
                {
                    return process;
                }
            }
            throw new Exception("процесс не найден");
        }

        public static void UpdateAccountsChildrens()
        {
            lock (StartedAccountsDict)
            {
                foreach (var pair in StartedAccountsDict)
                {
                    if (pair.Key.Status == 0)
                    {
                        return;
                    }
                    var childrens = GetChildrens(pair.Key);
                    pair.Key.Status = 1;
                    foreach (var item in childrens)
                    {
                        if (item.ProcessName == "csgo")
                        {
                            pair.Key.Status = 2;
                        }
                    }
                }
            }
        }
        
        private static string GetWindowNameByHwnd(IntPtr hwnd)
        {
            int len = GetWindowTextLength(hwnd) + 1;
            var buff = new StringBuilder(len);
            if (GetWindowText(hwnd, buff, len) > 0)
            {
                return buff.ToString();
            }
            return "";
        }

        public static IEnumerable<IntPtr> GetWindowHandles(Account acc)
        {
            Process proc = GetProcessByAccount(acc);
            var handles = new List<IntPtr>();
            var names = new List<string>();
            foreach (ProcessThread thread in proc.Threads)
                EnumThreadWindows(thread.Id,
                    (hWnd, lParam) => { handles.Add(hWnd); return true; }, IntPtr.Zero);
            foreach (var item in handles)
            {
                int len = GetWindowTextLength(item) + 1;
                var buff = new StringBuilder(len);
                if (GetWindowText(item, buff, len) > 0)
                {
                    names.Add(buff.ToString());
                }
            }
            return handles;
        }

        public static void SdaCheck()
        {
            lock (AccountsBase)
            {
                foreach (var acc in AccountsBase)
                {
                    if (acc.Status == 0)
                    {
                        continue;
                    }
                    var windows = GetWindowHandles(acc);
                    foreach (var hwnd in windows)
                    {
                        if (GetWindowNameByHwnd(hwnd) == "Steam Guard - Computer Authorization Required")
                        {
                            if (SteamGuard.HasGuard(acc.Login.ToLower()))
                            {
                                SendText(SteamGuard.GetGuard(acc.Login.ToLower()), hwnd);
                                SendText("ENTER", hwnd);
                            }
                        }
                    }
                }
            }
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern bool PostMessage(IntPtr hWnd, int Msg, char wParam, int lParam);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        private static void SendText(string message , IntPtr hwnd)
        {
            const int WM_KEYDOWN = 0x100;
            const int WM_KEYUP = 0x101;
            int VK_RETURN = 13;
            int WM_CHAR = 0x0102;
            if (message == "ENTER")
            {
                SetForegroundWindow(hwnd);
                //Thread.Sleep(150);
                PostMessage(hwnd, WM_KEYDOWN, (char)VK_RETURN, 0);
                //Thread.Sleep(10);
                PostMessage(hwnd, WM_KEYUP, (char)VK_RETURN, 0);
                return;
            }
            try
            {
                //активизируем окно, которое имело фокус
                SetForegroundWindow(hwnd);
                Thread.Sleep(100);
                //передаем ему текст посимвольно
                foreach (char ch in message)
                {
                    PostMessage(hwnd, WM_CHAR, ch, 1);
                }
            }
            catch (Exception error)
            {
                MessageBox.Show(error.Message);
            }

        }
    }
    public class Account
    {
        public string Login { get; set; }
        public string Password { get; set; }

        public int Status { get; set; }

        public int PID { get; set; }

        public override string ToString()
        {
            return Login;
        }
        public Account()
        {

        }
        public Account(string login, string password, bool isStarted)
        {
            this.Login = Login;
            this.Password = Password;
        }

        public Account(string login, string password)
        {
            this.Login = login;
            this.Password = password;
        }


    }

    public class ConfigObject
    {
        public string StartParams { get; set; }

        public List<AccountsGroup> Groups { get; set; }

        public ConfigObject()
        {

        }
    }

    public static class Config
    {
        private static ConfigObject config = new ConfigObject();

        public static string DirectoryPath = "VenchassPanel"; //update's with initializing components

        public static async void SaveAccountsDataAsync()
        {
            var json = System.Text.Json.JsonSerializer.Serialize(AccountManager.GetAccountsBase());
            // полная перезапись файла 
            using (StreamWriter writer = new StreamWriter(DirectoryPath + @"/Accounts.cfg", false))
            {
                await writer.WriteLineAsync(json);
            }
        }

        public static async void SaveGroupsParams(List<AccountsGroup> groups)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            config.Groups = groups;
            var json = System.Text.Json.JsonSerializer.Serialize(config, options);
            using (StreamWriter writer = new StreamWriter(DirectoryPath + @"/config.cfg", false))
            {
                await writer.WriteLineAsync(json);
            }
        }

        public static void LoadAccountsData()
        {
            var json = "";
            using (StreamReader reader = new StreamReader(DirectoryPath + @"/Accounts.cfg"))
            {
                json = reader.ReadToEnd();
            }
            try
            {
                var accountsList = System.Text.Json.JsonSerializer.Deserialize<List<Account>>(json);
                foreach (var item in accountsList)
                {
                    AccountManager.AddAccount(item);
                }
            }
            catch
            {
                File.WriteAllText(DirectoryPath + @"/Accounts.cfg", @"[]");
            }
            
        }

        public static async void SaveStartUpParams(string startupParams)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            config.StartParams = startupParams;
            var json = System.Text.Json.JsonSerializer.Serialize(config, options);
            using (StreamWriter writer = new StreamWriter(DirectoryPath + @"/config.cfg", false))
            {
                await writer.WriteLineAsync(json);
            }
        }

        public static ConfigObject LoadConfig()
        {
            try
            {
                var json = "";
                using (StreamReader reader = new StreamReader(DirectoryPath + @"/config.cfg"))
                {
                    json = reader.ReadToEnd();
                }
                config = System.Text.Json.JsonSerializer.Deserialize<ConfigObject>(json);
            }
            catch
            {
                MessageBox.Show("не удалось загрузить конфиг");
            }
            return config;
        }
    }

    public static class SteamGuard
    {
        private static bool IsInited = false;

        private static Dictionary<string, string> SteamGuardDict = new Dictionary<string, string>();

        private static string GetsharedSecret(string login)
        {
            if (SteamGuardDict.ContainsKey(login))
                return SteamGuardDict[login];
            return null;
        }

        public static bool HasGuard(string login)
        {
            if (!IsInited)
                init();
            return SteamGuardDict.ContainsKey(login);
        }

        public static string GetGuard(string login)
        {
            if (!IsInited)
                init();
            SteamGuardAccount account = new SteamGuardAccount();
            account.SharedSecret = GetsharedSecret(login);
            return account.GenerateSteamGuardCode();
        }

        private class SdaAccount
        {
            public string shared_secret { get; set; }
            public string account_name { get; set; } //login
        }

        private static void init()
        {
            //init
            DirectoryInfo dir = new DirectoryInfo(Config.DirectoryPath + @"/Steam Desktop Authenticator/maFiles");
            if (!dir.Exists)
                dir.Create();

            foreach (FileInfo file in dir.GetFiles("*.maFile"))
            {
                var account = JsonConvert.DeserializeObject<SdaAccount>(File.ReadAllText(file.FullName));
                SteamGuardDict.Add(account.account_name, account.shared_secret);
            }


            IsInited = true;
        }
    }
}
