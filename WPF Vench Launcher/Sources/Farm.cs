﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;


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

        public FarmAccount(Account acc)
        {
            this.prop = acc;

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
        public bool  CheckGrayWindow()
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

        
        public static void AutoFarm(List<Account> list)
        {
            if (queueToFarm.Count != 0 || currentFarmQueue.Count != 0)
                return;
            foreach (var account in list)
            {
                var farmAccount = new FarmAccount(account);
                queueToFarm.Add(farmAccount);
            }
            Task.Factory.StartNew(() =>
            {
                AutoFarmController();
            });
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
        }

        public static void StartFarmAccount(FarmAccount farmAcc)
        {
            farmAcc.StartupTime = DateTime.Now.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
            lock (queueToFarm)
            {
                if (queueToFarm.Contains(farmAcc))
                    queueToFarm.Remove(farmAcc);

            }
            lock (currentFarmQueue)
            {
                currentFarmQueue.Add(farmAcc);
            }
            AccountManager.StartAccount(farmAcc.prop, String.Format("-no-browser -novid -nosound -w 640 -h 480  -nomouse +connect 78.153.5.45:27015"), false);
        }

        private static void AutoFarmController()
        {
            while (queueToFarm.Count != 0 || currentFarmQueue.Count != 0)
            {
                foreach (var farmAcc in currentFarmQueue.ToList())
                {
                    if (DateTime.Now.Subtract(new DateTime(1970, 1, 1)).TotalSeconds - farmAcc.StartupTime > Config.GetConfig().MaxRemainingTimeToDropCase)
                    {
                        try
                        {
                            CloseAccount(farmAcc);
                        }
                        catch(Exception ex)
                        {
                            MessageBox.Show(ex.Message);
                        }
                    }
                    else if (farmAcc.CheckGrayWindow())
                    {
                        try
                        {
                            CloseAccount(farmAcc);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.Message);
                        }
                    }
                }
                while (queueToFarm.Count != 0 && currentFarmQueue.Count < Config.GetConfig().MaxSameTimeAccounts)
                {
                    StartFarmAccount(queueToFarm.First());
                }
                Thread.Sleep(5000);
            }
        }
    }
}
