using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace BeatSaverDownloader
{
    class PluginConfig
    {
        static private string configPath = "favouriteSongs.cfg";

        public static List<string> favouriteSongs = new List<string>();

        public static void LoadOrCreateConfig()
        {
            if (!File.Exists(configPath))
            {
                File.Create(configPath);
            }

            favouriteSongs.AddRange(File.ReadAllLines(configPath, Encoding.UTF8));
        }
        
        public static void SaveConfig()
        {
            File.WriteAllLines(configPath, favouriteSongs.ToArray(), Encoding.UTF8);
        }

    }
}
