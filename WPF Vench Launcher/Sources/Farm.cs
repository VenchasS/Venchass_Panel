using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Net;
using System.Web;
using Newtonsoft.Json;

namespace WPF_Vench_Launcher.Sources
{
    public class Color
    {
        public int R { get; set; }
        public int G { get; set; }
        public int B { get; set; }
        public Color(int r, int g, int b)
        {
            R = r;
            G = g;
            B = b;
        }

        public override bool Equals(object obj)
        {
            if (obj is Color)
            {
                var c = (Color)obj;
                return R == c.R && G == c.G && B == c.B;
            }
            return false;
        }
    }

    public class Vector2Int
    {
        public int X { get; set; }
        public int Y { get; set; }

        public Vector2Int(int x, int y)
        {
            this.X = x;
            this.Y = y;
        }
    }

    public class FarmAccount
    {
        [DllImport("User32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport("gdi32.dll")]
        private static extern uint GetPixel(IntPtr hDC, int nXPos, int nYPos);


        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr hwnd);

        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hwnd, IntPtr hDC);

        private static uint WM_MOUSEMOVE = 0x0200;
        private static uint WM_LBUTTONDOWN = 0x0201;
        private static uint WM_LBUTTONUP = 0x0202;


        public Account prop { get; private set; }
        public double StartupTime { get; set; }

        public Process csgo { get; set; }

        public int consoleIndex { get; set; }

        public int privateRang { get; set; }



        public FarmAccount(Account acc)
        {
            this.prop = acc;
        }

        public void SetLastDrop()
        {
            this.prop.LastDrop = DateTime.Now.ToString();
        }

        public Color GetColor(int x, int y)
        {
            IntPtr hDC = GetDC(AccountManager.GetGameProcess(this.prop).MainWindowHandle); ;//Ссылка на окно, в котором будет выполнен поиск пикселя
            uint pixel = GetPixel(hDC, x, y);
            ReleaseDC(IntPtr.Zero, hDC);

            int r = (byte)(pixel & 0x000000FF);//получим составляющие цвета
            int g = (byte)((pixel & 0x0000FF00) >> 8);
            int b = (byte)((pixel & 0x00FF0000) >> 16);
            var color = new Color(r, g, b);
            return color;
        }
        public bool CheckGrayWindow()
        {
            try
            {
                if (GetColor(60, 20).Equals(new Color(65, 65, 65)))
                {
                    return true;
                }
                return false;
            }
            catch { return false; }
        }
    }

    public static class FarmManager
    {
        private static List<FarmAccount> queueToFarm = new List<FarmAccount>();

        private static List<FarmAccount> currentFarmQueue = new List<FarmAccount>();

        public static int QueueCount { get { return queueToFarm.Count; } private set { } }
        public static int StartedCount { get { return currentFarmQueue.Count; } private set { } }
        public static int FarmedCount { get; private set; }

        

        public static void AutoFarm(List<Account> list)
        {
            string path = Config.GetConfig().SteamPath + @"\NoSandBoxsteam.exe";
            try
            {
                Process.GetProcessesByName("NoSandBoxsteam").Where(x => { x.Kill(); return true; });
                File.Copy(Config.GetConfig().SteamPath + @"\steam.exe", path, true);
            }
            catch {
            }
            var listToFarm = queueToFarm.Select(x => x.prop.Login).Concat(currentFarmQueue.Select(x => x.prop.Login)).ToList();

            Task.Factory.StartNew(() =>
            {
                foreach (var account in list.Where(x => x.SteamId32 == 0))
                {
                    AccountManager.TryGetSteamId(account, 5000);
                }
                foreach (var account in list)
                {
                    if (listToFarm.Contains(account.Login))
                        continue;
                    var farmAccount = new FarmAccount(account);
                    lock (queueToFarm)
                    {
                        queueToFarm.Add(farmAccount);
                    }
                }
                if(listToFarm.Count() == 0)
                    AutoFarmController();
            });

        }
        private static DateTime GetLastWednesday()
        {
            DateTime currentDate = DateTime.Now;
            DayOfWeek currentWeek = currentDate.DayOfWeek;
            // Находим разницу в днях между текущим днем недели и средой (DayOfWeek.Wednesday)
            int diff = (int)DayOfWeek.Wednesday - (int)currentWeek;
            // Если разница отрицательная, то нужно вычесть 7 дней
            if (diff < 0)
                diff -= 7;
            // Вычитаем разницу в днях из текущей даты, чтобы получить первую прошлую среду
            DateTime lastWednesday = currentDate.AddDays(diff);

            return lastWednesday.Date.AddHours(6);
        }


        public static List<Account> GetAutoFarmAccounts()
        {
            return AccountManager.GetAccountsBase()
                .Where(x => x.PrimeStatus == true)
                .Where(x => x.LastDrop != null)
                .Where(x => { var last = Convert.ToDateTime(x.LastDrop); return (last < GetLastWednesday()); })
                .Concat(AccountManager.GetAccountsBase()
                .Where(x => x.PrimeStatus == true)
                .Where(x => x.LastDrop == null))
                .ToList();
        }
        public static void CloseAccount(FarmAccount farmAcc)
        {
            lock (currentFarmQueue)
            {
                if (currentFarmQueue.Contains(farmAcc))
                {
                    currentFarmQueue.Remove(farmAcc);
                }
            }
            AccountManager.StopAccount(farmAcc.prop);
            if (PanelManager.isEnabled())
            {
                PanelManager.DeleteTarget(Convert.ToString(farmAcc.prop.SteamId32));
            }
            FarmedCount += 1;
        }

        public static void StartFarmAccount(FarmAccount farmAcc)
        {
            farmAcc.StartupTime = DateTime.Now.Subtract(new DateTime(1970, 1, 1)).TotalMinutes;
            lock (queueToFarm)
            {
                if (queueToFarm.Contains(farmAcc))
                    queueToFarm.Remove(farmAcc);

            }
            lock (currentFarmQueue)
            {
                currentFarmQueue.Add(farmAcc);
            }
            if (farmAcc.prop.Status == 0)
            {
                AccountManager.StartAccount(farmAcc.prop, String.Format("-novid -nosound  -nomouse -widnow -w 640 -h 480  -nomouse {0} ++attack2 +-left", Config.GetConfig().StartParams), false);
            }
            if (PanelManager.isEnabled())
            {
                var resp = PanelManager.AddTarget(Convert.ToString(farmAcc.prop.SteamId32));
            }
            else
            {
                AccountManager.SaveLogInfo("account " + farmAcc.prop.Login + "not added, panel not found");
            }
            Config.SaveAccountsDataAsync(farmAcc.prop);
        }

        private static void AccountsLaunchController()
        {
            while (true)
            {
                try
                {
                    while (queueToFarm.Count != 0 && (currentFarmQueue.Count < Config.GetConfig().MaxSameTimeAccounts || Config.GetConfig().MaxSameTimeAccounts == 0))
                    {
                        lock (queueToFarm)
                        {
                            StartFarmAccount(queueToFarm.First());
                        }
                        Thread.Sleep(Config.GetConfig().launchDelay * 1000);
                    }
                    Thread.Sleep(5000);
                }
                catch (Exception ex) 
                {
                    AccountManager.SaveLogInfo(ex.Message);
                }
            }
            
        }

        private static void ParseConsole(FarmAccount account)
        {
            try
            {
                var path = AccountManager.GetPathToLogs(account.prop);
                if (!File.Exists(path))
                    return;
                var index = account.consoleIndex;
                FileInfo log = new FileInfo(path);
                
                using (var streamReader = new StreamReader(log.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
                {
                    string text = streamReader.ReadToEnd();
                    string[] lines = text.Split(
                        new string[] { "\r\n", "\r", "\n" },
                        StringSplitOptions.None
                    );
                    for (var i = index;i <lines.Count();i++)
                    {
                        /*var line = lines[i];
                        if (line == "Disconnect: .")
                        {
                            CloseAccount(account);
                            account.SetLastDrop();
                            if (Config.GetConfig().TradesCheckbox)
                                TraderController.AddAccount(account.prop);
                            AccountManager.SaveLogInfo(String.Format("Accounts {0} closed after kick from server {1}", account.prop.Login, Config.GetConfig().ServersToConnect));
                            Config.SaveAccountsDataAsync(account.prop);
                            break;
                        }
                        else if (line == "Not connected to server")
                        {
                            AccountManager.SendCmd(account.prop, String.Format("connect {0}", Config.GetConfig().ServersToConnect));
                            AccountManager.SaveLogInfo(String.Format("Accounts {0} reconected to server {1}", account.prop.Login, Config.GetConfig().ServersToConnect));
                        }*/
                    }
                    account.consoleIndex = lines.Count()-1;
                }
            }
            catch (Exception e)
            {
                AccountManager.SaveLogInfo(e.Message);
            }
        }

        private static void AutoFarmController()
        {
            while (queueToFarm.Count != 0 || currentFarmQueue.Count != 0)
            {
                try
                {

                    foreach (var farmAcc in currentFarmQueue.ToList())
                    {
                        if (farmAcc.prop.Status == 0)
                        {
                            CloseAccount(farmAcc);
                            AccountManager.SaveLogInfo(String.Format("Accounts {0} closed by user ", farmAcc.prop.Login));
                        }
                        else if (Config.GetConfig().MaxRemainingTimeToDropCase != 0 && DateTime.Now.Subtract(new DateTime(1970, 1, 1)).TotalMinutes - farmAcc.StartupTime > Config.GetConfig().MaxRemainingTimeToDropCase)
                        {
                            try
                            {
                                if (Config.GetConfig().MarkLimitCheckbox)
                                    farmAcc.SetLastDrop();
                                CloseAccount(farmAcc);
                                
                                AccountManager.SaveLogInfo(String.Format("Accounts {0} closed by time {1} minutes", farmAcc.prop.Login, Config.GetConfig().MaxRemainingTimeToDropCase));
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show(ex.Message);
                            }
                        }

                        //ParseConsole(farmAcc); //no need now
                        AccountManager.SendCmd(farmAcc.prop, "slot3");
                    }
                    var targets = PanelManager.GetTargets();
                    var copy = new List<FarmAccount>(currentFarmQueue);
                    foreach (var target in targets)
                    {
                        if (target.rank == 0 || target.rank == -1)
                        {
                            continue;
                        }
                        if (copy.Select(x => x.prop.SteamId32.ToString()).ToList().Contains(target.name))
                        {
                            var acc = copy.Where(x => x.prop.SteamId32.ToString() == target.name).FirstOrDefault();
                            if (acc.privateRang == 0)
                            {
                                acc.privateRang = target.rank;
                            } 
                            else if(acc.privateRang != target.rank && (target.rank > acc.privateRang || target.rank == 1))
                            {
                                CloseAccount(acc);
                                acc.SetLastDrop();
                                if (Config.GetConfig().TradesCheckbox)
                                    TraderController.AddAccount(acc.prop);
                                AccountManager.SaveLogInfo(String.Format("Accounts {0} closed after level up ", acc.prop.Login));
                                Config.SaveAccountsDataAsync(acc.prop);
                            }
                        }
                    }
                    while (queueToFarm.Count != 0 && (currentFarmQueue.Count < Config.GetConfig().MaxSameTimeAccounts || Config.GetConfig().MaxSameTimeAccounts == 0))
                    {
                        lock (queueToFarm)
                        {
                            StartFarmAccount(queueToFarm.First());
                        }
                        Thread.Sleep(Config.GetConfig().launchDelay * 1000);
                    }
                    Thread.Sleep(15000);
                }
                catch (Exception e)
                {
                    AccountManager.SaveLogInfo(e.Message);
                }
            }
        }
    }

    public class PanelTarget
    {
        public bool confirmed { get; set; }
        public string name { get; set; }
        public int rank { get; set; }
        public int score { get; set; }
        public int scoreLimit { get; set; }
        public int timeStatus { get; set; }

    }

    static class PanelManager
    {
        public static string SendGetRequest(string path, Dictionary<string, string> parameters, string url = "")
        {
            if (url == "" || url == null)
            {
                url = "localhost";
            }
            string apiUrlWithParam = $"http://{url}:8322{path}?";
            if (parameters != null && parameters.Count > 0)
            {
                // Создаем QueryString с помощью класса HttpUtility
                var queryString = HttpUtility.ParseQueryString(string.Empty);
                foreach (var parameter in parameters)
                {
                    queryString[parameter.Key] = parameter.Value;
                }

                // Добавляем QueryString к URL
                apiUrlWithParam += queryString;
            }
            

            using (var client = new WebClient())
            {
                try
                {
                    return client.DownloadString(apiUrlWithParam);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An error occurred: {ex.Message}");
                    return string.Empty;
                }
            }
        }

        public static string SendGetRequest(string path, string url = "")
        {
            if (url == "" || url == null)
            {
                url = "localhost";
            }
            string apiUrlWithParam = $"http://{url}:8322{path}";

            using (var client = new WebClient())
            {
                try
                {
                    return client.DownloadString(apiUrlWithParam);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An error occurred: {ex.Message}");
                    return string.Empty;
                }
            }
        }

        public static string AddTarget(string steamId32)
        {
            return SendGetRequest("/addtarget", new Dictionary<string, string>{
                { "id",  steamId32 },
                { "limit", "1000" }
            } , Config.GetConfig().CustomPanelIp);
        }

        public static string DeleteTarget(string value)
        {
            return SendGetRequest("/deletetarget", new Dictionary<string, string>{
                { "id",  value },
            }, Config.GetConfig().CustomPanelIp);
        }

        public static bool  isEnabled()
        {
            return SendGetRequest("/ping", Config.GetConfig().CustomPanelIp) == "200";
        }

        public static List<PanelTarget> GetTargets()
        {
            var resp = SendGetRequest("/getalltargets", Config.GetConfig().CustomPanelIp);
            if (resp == "null" || resp == "")
            {
                return new List<PanelTarget>();
            }
            List<PanelTarget> targets = JsonConvert.DeserializeObject<List<PanelTarget>>(resp);
            return targets;
        }
    }
}
