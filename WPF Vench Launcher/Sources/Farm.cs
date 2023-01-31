using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WPF_Vench_Launcher.Sources
{
    public class FarmAccount
    {
        public Account prop { get; private set; }
        public long StartupTime { get; private set; }

        public FarmAccount(Account acc)
        {
            this.prop = acc;
            StartupTime = DateTime.Now.Second;
        }

    }

    public static class FarmMaanager
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
        }

        public static void StartFarmAccount(FarmAccount farmAcc)
        {
            lock (queueToFarm)
            {
                if (queueToFarm.Contains(farmAcc))
                    queueToFarm.Remove(farmAcc);

            }
            lock (currentFarmQueue)
            {
                currentFarmQueue.Add(farmAcc);
            }
        }

        private static void AutoFarmController()
        {
            while (queueToFarm.Count != 0 || currentFarmQueue.Count != 0)
            {
                foreach (var farmAcc in currentFarmQueue)
                {
                    if (DateTime.Now.Second - farmAcc.StartupTime > Config.GetConfig().MaxRemainingTimeToDropCase)
                        CloseAccount(farmAcc);
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
