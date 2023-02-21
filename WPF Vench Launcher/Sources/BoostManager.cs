using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

//Software by Venchass
//My:
//Discord VenchasS#9039
//telegram @VenchasS
//https://vk.com/venchass

namespace WPF_Vench_Launcher
{
    public class BoostManager
    {
        [DllImport("User32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport("gdi32.dll")]
        public static extern uint GetPixel(IntPtr hDC, int nXPos, int nYPos);


        [DllImport("user32.dll")]
        public static extern IntPtr GetDC(IntPtr hwnd);

        [DllImport("user32.dll")]
        public static extern int ReleaseDC(IntPtr hwnd, IntPtr hDC);

        static uint WM_MOUSEMOVE = 0x0200;
        static uint WM_LBUTTONDOWN = 0x0201;
        static uint WM_LBUTTONUP = 0x0202;

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

        public class BoostAccount
        {
            public string Login { get; set; }
            public int PID { get; set; }

            private Process csgo;

            public string InviteCode { get; set; }
            public BoostAccount(Account acc)
            {
                this.Login = acc.Login;
                this.PID = acc.PID;
                this.csgo = AccountManager.GetGameProcess(acc);
            }

            static int MakeLParam(int x, int y)
            {
                return (y << 16) | (x & 0xFFFF);
            }

            public void LeftClick(int x, int y)
            {
                PostMessage(csgo.MainWindowHandle, WM_MOUSEMOVE,(IntPtr)0, (IntPtr)MakeLParam(x, y));
                PostMessage(csgo.MainWindowHandle, WM_LBUTTONDOWN, (IntPtr)1, (IntPtr)MakeLParam(x, y));
                PostMessage(csgo.MainWindowHandle, WM_LBUTTONUP, (IntPtr)0, (IntPtr)MakeLParam(x, y));
            }

            public void LeftClick(Vector2Int vec)
            {
                PostMessage(csgo.MainWindowHandle, WM_MOUSEMOVE, (IntPtr)0, (IntPtr)MakeLParam(vec.X, vec.Y));
                PostMessage(csgo.MainWindowHandle, WM_LBUTTONDOWN, (IntPtr)1, (IntPtr)MakeLParam(vec.X, vec.Y));
                PostMessage(csgo.MainWindowHandle, WM_LBUTTONUP, (IntPtr)0, (IntPtr)MakeLParam(vec.X, vec.Y));
            }

            public Color GetColor(int x, int y)
            {
                IntPtr hDC = GetDC(csgo.MainWindowHandle);//Ссылка на окно, в котором будет выполнен поиск пикселя
                uint pixel = GetPixel(hDC, x, y);
                ReleaseDC(IntPtr.Zero, hDC);

                int r = (byte)(pixel & 0x000000FF);//получим составляющие цвета
                int g = (byte)((pixel & 0x0000FF00) >> 8);
                int b = (byte)((pixel & 0x00FF0000) >> 16);
                var color = new Color(r,g,b);
                return color;
            }
            
            public string GetInviteCode()
            {
                if (InviteCode == null)
                {
                    while (!GetColor(269, 78).Equals(new Color(244, 244, 245)))
                    {
                        Thread.Sleep(200);
                        LeftClick(339, 62);
                    }
                    LeftClick(269, 78);
                    WaitColor(209, 108, new Color(37, 37, 37));
                    LeftClick(191, 149);
                    Thread.Sleep(10);
                    try
                    {
                        InviteCode = Clipboard.GetText();
                    }
                    catch
                    {
                        
                    }
                    LeftClick(211, 145);
                }
                return InviteCode;
            }

            public void InviteToLobbyByCode(string code)
            {
                LeftClick(340, 64);
                Thread.Sleep(100);
                LeftClick(339, 62);
                while (!GetColor(269, 78).Equals(new Color(244, 244, 245)))
                {
                    Thread.Sleep(200);
                    LeftClick(339, 62);
                }
                LeftClick(269, 78);
                while (!GetColor(209, 108).Equals(new Color(37, 37, 37)))
                    Thread.Sleep(100);
                //AccountManager.SendText(code, csgo.MainWindowHandle);
                while (!GetColor(268, 120).Equals(new Color(43, 43, 43))) //ждем окно для нажатия на письмо
                {
                    LeftClick(204, 133);
                    Thread.Sleep(500);
                }
                do
                {
                    LeftClick(328, 124);
                    Thread.Sleep(200);
                } while (GetColor(327, 124).Equals(new Color(213, 213, 213))); //жмем на отправку приглашения
                while (GetColor(197, 101).Equals(new Color(37, 37, 37)))
                    LeftClick(211, 157);
            }
            public void AcceptLobbyInvite()
            {
                do
                {
                    LeftClick(338, 94);
                    Thread.Sleep(200);
                } while (!GetColor(337, 94).Equals(new Color(221, 221, 221)));
                while (!GetColor(269, 10).Equals(new Color(197, 197, 197)))
                {
                    Thread.Sleep(200);
                }
                while (GetColor(269, 10).Equals(new Color(197, 197, 197)))
                {
                    LeftClick(334, 71);
                    Thread.Sleep(500);
                }
            }

            public void WaitLobbyInvite()
            {
                Thread.Sleep(2000);
                AcceptLobbyInvite();
            }

            public void OpenRightTab()
            {
                while(GetColor(320, 216).Equals(new Color(0,0,0)))
                {
                    LeftClick(349, 230);
                    Thread.Sleep(200);
                }
            }

            public Vector2Int FindMessage()
            {
                int x = 338;
                for (int y = 50; y < 200; y++)
                {
                    if (GetColor(x, y).Equals(new Color(220, 220, 220)))
                    {
                        return new Vector2Int(x, y);
                    }
                }
                return new Vector2Int(0, 0);
            }

            public void WaitColor(int x, int y, Color color)
            {
                while(!GetColor(x,y).Equals(color))
                {
                    Thread.Sleep(200);
                }
            }
        }

        public class BoostGroup
        {
            private List<BoostAccount> boostAccountsList = new List<BoostAccount>();

            public int Id { get; }
            public BoostGroup(List<Account> accounts)
            {
                foreach (var acc in accounts)
                {
                    boostAccountsList.Add(new BoostAccount(acc));
                }
                Id = boostGroups.Count;
            }

            public List<BoostAccount> GetAccounts()
            {
                return boostAccountsList;
            }

            private void AllLeftClick(int x,int y)
            {
                foreach (var acc in boostAccountsList)
                    acc.LeftClick(x, y);
            }

            private bool IsAllAccsHaveInviteCode()
            {
                foreach (var acc in boostAccountsList)
                    if (acc.InviteCode == null)
                        return false;
                return true;
            }

            private void CollectInviteCodes()
            {
                foreach (var account in boostAccountsList)
                {
                    account.GetInviteCode();
                }
            }

            private void VertigoBoost()
            {
                //AllLeftClick(343, 214);
                /*if (!IsAllAccsHaveInviteCode())
                    CollectInviteCodes();*/

                /*list[0].InviteToLobbyByCode(list[1].GetInviteCode());
                list[0].InviteToLobbyByCode(list[2].GetInviteCode());
                list[0].InviteToLobbyByCode(list[3].GetInviteCode());
                list[0].InviteToLobbyByCode(list[4].GetInviteCode());

                list[5].InviteToLobbyByCode(list[6].GetInviteCode());
                list[5].InviteToLobbyByCode(list[7].GetInviteCode());
                list[5].InviteToLobbyByCode(list[8].GetInviteCode());
                list[5].InviteToLobbyByCode(list[9].GetInviteCode());*/

                foreach(var acc in boostAccountsList)
                {
                    acc.OpenRightTab();
                }
                foreach (var acc in boostAccountsList)
                {
                    var msgCoords = acc.FindMessage();
                    acc.LeftClick(msgCoords);
                    var addFriendCoords = new Vector2Int(msgCoords.X - 70, msgCoords.Y + 20);
                    //acc.WaitColor(addFriendCoords.X, addFriendCoords.Y, new Color(173, 173, 173));
                    Thread.Sleep(1000);
                    /*acc.LeftClick(addFriendCoords);
                    acc.WaitColor(207, 110, new Color(37, 37, 37));*/
                    acc.GetInviteCode();

                }
            }

            public void Start()
            {
                VertigoBoost();
            }
            
        }

        private static List<BoostGroup> boostGroups = new List<BoostGroup>();

        public static List<BoostGroup> GetGroups()
        {
            return boostGroups;
        }

        public static bool MakeBoostGroup(List<Account> accounts)
        {
            
            try
            {
                lock (boostGroups)
                {
                    boostGroups.Add(new BoostGroup(accounts));
                }
            }
            catch
            {
                return false;
            }

            return true;
        }
    }
}
