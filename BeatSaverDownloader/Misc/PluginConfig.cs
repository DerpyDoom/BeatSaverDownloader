using SongBrowserPlugin;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace BeatSaverDownloader
{
    class PluginConfig
    {
        static private string configPath = "favoriteSongs.cfg";
        static private string songBrowserSettings = "song_browser_settings.xml";

        public static List<string> favoriteSongs = new List<string>();

        public static void LoadOrCreateConfig()
        {
            if (!File.Exists(configPath))
            {
                File.Create(configPath);
            }

            favoriteSongs.AddRange(File.ReadAllLines(configPath, Encoding.UTF8));
            

            if (!File.Exists(songBrowserSettings))
            {
                return;
            }
        
            FileStream fs = null;
            try
            {
                fs = File.OpenRead(songBrowserSettings);

                XmlSerializer serializer = new XmlSerializer(typeof(SongBrowserSettings));

                SongBrowserSettings settings = (SongBrowserSettings)serializer.Deserialize(fs);

                favoriteSongs.AddRange(settings.favorites);

                fs.Close();
                
                SaveConfig();
            }
            catch (Exception e)
            {
                Logger.StaticLog($"Can't parse BeatSaberSongBrowser settings file! Exception: {e}");
                if (fs != null) { fs.Close(); }
            }

        }
        
        public static void SaveConfig()
        {
            File.WriteAllLines(configPath, favoriteSongs.Distinct().ToArray(), Encoding.UTF8);
        }

    }
}
