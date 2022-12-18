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
using WPF_Vench_Launcher.pages;
using WinForms = System.Windows.Forms;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Security.Principal;

//Software by Venchass
//My:
//Discord VenchasS#9039
//telegram @VenchasS
//https://vk.com/venchass

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

        private static string steamPath = @"C:\Program Files (x86)\Steam";
        public static string SteamPath { get { return steamPath; }  }

        public static void SetSteamPath(string newPath)
        {
            steamPath = newPath;
        }

        static List<Account> AccountsBase = new List<Account>();
        public static void StartAccount(Account account, string startParams = "", bool startSteam = false)
        {
            lock (account)
            {
                if (account.SteamId32 == 0)
                {
                    var login = new UserLogin(account.Login, account.Password);
                    var response = login.DoLogin();
                    if (response == LoginResult.Need2FA)
                    {
                        if (SteamGuard.HasGuard(account.Login.ToLower()))
                        {
                            login.TwoFactorCode = SteamGuard.GetGuard(account.Login.ToLower());
                            response = login.DoLogin();
                        }
                    }
                    if (login.LoggedIn)
                    {
                        account.SteamId32 = login.Session.SteamID - 76561197960265728;
                    }
                }
                if (account.SteamId32 != 0)
                {
                    Config.SetOptimizeSettings(account.SteamId32);

                }










                string path = steamPath + @"\steam.exe";
                var startApp = startSteam ? "" : "";
                ProcessStartInfo processStartInfo = new ProcessStartInfo()
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    WorkingDirectory = steamPath,
                    FileName = path,
                    Arguments = string.Format("-noreactlogin -login {0} {1}  {2}  {3} ", account.Login, account.Password, startApp, startParams)
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

        public static void OpenSteam(Account acc)
        {
            lock (acc)
            {
                if (acc.Status == 0)
                {
                    StartAccount(acc, "-console", true);
                }
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


        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = false)]
        public static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, ref COPYDATASTRUCT lParam);

        [DllImport("user32.dll")]
        static extern int SetWindowText(IntPtr hWnd, string text);

        public struct COPYDATASTRUCT
        {
            public IntPtr dwData;
            public int cbData;
            public IntPtr lpData;
        }

        public static COPYDATASTRUCT CreateForString(int dwData, string value, bool Unicode = false)
        {
            var result = new COPYDATASTRUCT();
            result.dwData = (IntPtr)dwData;
            result.lpData = Unicode ? Marshal.StringToCoTaskMemUni(value) : Marshal.StringToCoTaskMemAnsi(value);
            result.cbData = value.Length + 1;
            return result;
        }

        public static void SendCmd(string str)
        {
            foreach (var acc in GetAccountsBase())
            {
                var task = Task.Factory.StartNew(() =>
                {
                    try
                    {
                        var cds = CreateForString(0, str, false);
                        var csgo = GetGameProcess(acc);
                        SendMessage(csgo.MainWindowHandle, 0x4A, IntPtr.Zero, ref cds);
                    }
                    catch
                    {

                    }
                });
            }
        }

        public static void RenameWindows()
        {
            foreach (var acc in GetAccountsBase())
            {
                try
                {
                    var csgo = GetGameProcess(acc);
                    SetWindowText(csgo.MainWindowHandle, acc.Login);
                }
                catch
                {

                }
            }
        }

        public struct RECT
        {

            public int Left;

            public int Top;

            public int Right;

            public int Bottom;

        }

        [DllImport("user32.dll", EntryPoint = "SetWindowPos")]
        static extern bool SetWindowPos(IntPtr hWnd, int hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);


        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]

        static extern bool GetWindowRect(IntPtr hWnd, ref RECT lpRect);
        public static void AutoMoveWidnow()
        {
            var SysHeight = SystemParameters.FullPrimaryScreenHeight;
            var SysWidth = SystemParameters.FullPrimaryScreenWidth;
            int currentHeight = 0;
            int currentWith = 0;
            const int SWP_NOSIZE = 1;
            foreach (Process proc in Process.GetProcessesByName("csgo"))
            {
                RECT rc = new RECT();
                GetWindowRect(proc.MainWindowHandle, ref rc);
                int width = rc.Right - rc.Left;
                int height = rc.Bottom - rc.Top;
                if (currentWith + width >= SysWidth)
                {
                    currentHeight += height;
                    currentWith = 0;
                }
                var widthBuff = currentWith;
                Task.Factory.StartNew(() =>
                {
                    SetWindowPos(proc.MainWindowHandle, 1, 60 + widthBuff, currentHeight, 900, 261, SWP_NOSIZE);
                });
                currentWith += width;
            }
        }

        public static void StopAllAccounts()
        {
            lock (StartedAccountsDict)
            {
                StartedAccountsDict.Clear();
            }
            lock (GetAccountsBase())
            {
                foreach (var item in GetAccountsBase())
                {
                    item.Status = 0;
                    item.PID = 0;
                }
            }
            foreach (Process proc in Process.GetProcessesByName("steam"))
            {
                proc.Kill();
            }
            foreach (Process proc in Process.GetProcessesByName("csgo"))
            {
                proc.Kill();
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

        public static void DelteAccount(Account acc)
        {
            lock(AccountsBase)
            {
                AccountsBase.Remove(acc);
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
        public static extern int SendMessage(IntPtr hWnd, int wMsg, char wParam, int lParam);


        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        public static void SendText(string message , IntPtr hwnd)
        {
            const int WM_KEYDOWN = 0x100;
            const int WM_KEYUP = 0x101;
            int VK_RETURN = 13;
            int WM_CHAR = 0x0102;
            if (message == "ENTER")
            {
                SetForegroundWindow(hwnd);
                //Thread.Sleep(150);
                Thread.Sleep(10);
                SendMessage(hwnd, WM_KEYDOWN, (char)VK_RETURN, 0);
                Thread.Sleep(10);
                SendMessage(hwnd, WM_CHAR, (char)VK_RETURN, 1);
                SendMessage(hwnd, WM_CHAR, (char)VK_RETURN, 1);
                SendMessage(hwnd, WM_KEYUP, (char)VK_RETURN, 0);
                Thread.Sleep(10);
                SendMessage(hwnd, WM_KEYDOWN, (char)VK_RETURN, 0);
                Thread.Sleep(10);
                SendMessage(hwnd, WM_CHAR, (char)VK_RETURN, 1);
                SendMessage(hwnd, WM_CHAR, (char)VK_RETURN, 1);
                SendMessage(hwnd, WM_KEYUP, (char)VK_RETURN, 0);
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
                    SendMessage(hwnd, WM_CHAR, ch, 1);
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

        public bool PrimeStatus { get; set; }

        public ulong SteamId32 { get; set; }

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
        public string SteamPath { get; set; }

        public List<AccountsGroup> Groups { get; set; }

        public ConfigObject()
        {

        }
    }

    public static class Config
    {
        private static ConfigObject config = new ConfigObject();
        private static string currentDirectoryPath;
        public static string DirectoryPath { get { return currentDirectoryPath; } } //update's with initializing components

        public static void SetOptimizeSettings(ulong steamId32)
        {
            //check folder and settings
            string subPath = AccountManager.SteamPath + @"\userdata\" + steamId32 + @"\730\local\cfg";
            bool exists = System.IO.Directory.Exists(subPath);
            if (!exists)
            {
                System.IO.Directory.CreateDirectory(subPath);
            }

            Action<string, string> rewrite = (path, value) =>
            {
                if (File.Exists(path))
                {
                    File.SetAttributes(path, FileAttributes.Normal);
                }
                var file = File.Create(path);
                if (file != null && file.CanWrite)
                {
                    file.Write(Encoding.Default.GetBytes(value), 0, value.Length);
                };
            };
            rewrite(subPath + @"/config.cfg", Properties.Resources.configDefault);
            rewrite(subPath + @"/video.txt", Properties.Resources.video);
            rewrite(subPath + @"/videodefaults.txt", Properties.Resources.videodefaults);

        }

        public static void SetDirectoryPath(string newPath)
        {
            currentDirectoryPath = newPath;
        }

        public static async void SaveAccountsDataAsync()
        {
            var options = new JsonSerializerOptions 
            { 
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                WriteIndented = true 
            };
            var accounts = AccountManager.GetAccountsBase();
            var json = "";
            lock (accounts)
            {
                json = System.Text.Json.JsonSerializer.Serialize(accounts, options);
            }
            using (StreamWriter writer = new StreamWriter(DirectoryPath + @"/Accounts.cfg", false))
            {
                await writer.WriteLineAsync(json);
            }
        }

        public static void SaveGroupsParams(List<AccountsGroup> groups)
        {
            config.Groups = groups;
            SaveConfig();
        }

        public static void SaveStartUpParams(string startupParams)
        {
            config.StartParams = startupParams;
            SaveConfig();
        }

        public static void SaveSteamPath(string steamPath)
        {
            config.SteamPath = steamPath;
            SaveConfig();
        }

        /// <summary>
        /// Load Accounts from Accounts.cfg to AccountsManager, rewrite file to empty when error
        /// </summary>
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

        private static async void SaveConfig()
        {
            var options = new JsonSerializerOptions 
            {
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                WriteIndented = true
            };
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
                if (config.SteamPath == null)
                {
                    AskSteamPath();
                }
            }
            catch
            {
                MessageBox.Show("не удалось загрузить конфиг");
            }
            return GetConfig();
        }

        public static ConfigObject GetConfig()
        {
            return config;
        }

        public static void AskSteamPath()
        {
            var folderDialog = new WinForms.FolderBrowserDialog();
            folderDialog.Description = "Select Steam folder which contains steam.exe";
            if (folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                SaveSteamPath(folderDialog.SelectedPath);
            }
            AccountManager.SetSteamPath(GetConfig().SteamPath);
        }

        public static bool ImportAccountsFromFile()
        {
           var fildeDialog = new Microsoft.Win32.OpenFileDialog();



            // Set filter for file extension and default file extension 
            fildeDialog.DefaultExt = ".txt";



            if (fildeDialog.ShowDialog() == true)
            {
                var path = fildeDialog.FileName;
                // Open file
                using (var file = System.IO.File.OpenText(path))
                {
                    // Read file
                    while (!file.EndOfStream)
                    {
                        var line = file.ReadLine();
                        if (line.Length > 0)
                        {
                            var accountLine = line.Split(':');
                            if (accountLine.Length == 2 || accountLine.Length == 3)
                            {
                                var login = accountLine[0];
                                var password = accountLine[1];
                                var account = new Account(login, password);
                                if (accountLine.Length == 3)
                                    account.PrimeStatus = accountLine[3] == "true";
                                AccountManager.AddAccount(account);
                            }
                        }
                    }
                    Config.SaveAccountsDataAsync();
                    return true;
                }
            }
            return false;
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
