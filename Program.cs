using System;
using System.Reflection;

namespace Proxycord
{
    public class Program
    {
        public static Proxycord Proxycord;

        static void Main(string[] args)
        {
            Log.Info("Proxycord by Mibble v" + Assembly.GetExecutingAssembly().GetName().Version);

            Proxycord = new Proxycord();

            _ = Proxycord.Listen();

            while (true) 
            {
                string line = Console.ReadLine()?.Trim().ToLower();

                if (line == "reload")
                {
                    Log.Info("Reloading config...");
                    Proxycord.Config.Load();
                    Log.Info($"Config reloaded! {Proxycord.Config.Rules.Count} rules loaded.");
                }
                else if (line == "help")
                {
                    Log.Info("Type \"reload\" to reload the proxy config.");
                }
            }
        }
    }
}
