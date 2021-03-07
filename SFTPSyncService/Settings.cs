using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinSCP;

namespace SFTPSyncService
{
    public class Settings
    {
        private static string DEFAULT_FILENAME = getCurrentFolder() + "settings.json";

        public string HostName { get; set; }
        public int PortNumber { get; set; } 
        public string UserName { get; set; } 
        public string SshPrivateKeyPath { get; set; }
        public string SshHostKeyFingerprint { get; set; } 
        public int FrequencyInSec { get; set; }
        public string FolderToMoveFilesSentRemote { get; set; }
        public List<SyncFoldersSettings> FoldersSettings { get; set; } = new List<SyncFoldersSettings>();
        
        public void Save()
        {
            File.WriteAllText(DEFAULT_FILENAME, JsonConvert.SerializeObject(this));
        }

        public static Settings Load()
        {
            try
            {
                return JsonConvert.DeserializeObject<Settings>(File.ReadAllText(DEFAULT_FILENAME));
            }
            catch (Exception er)
            {
                throw er;
            }
        }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }

        public static string getCurrentFolder()
        {
            var folder = System.Reflection.Assembly.GetExecutingAssembly().Location;
            var index = folder.LastIndexOf(@"\");
            return folder.Substring(0, index + 1);
        }
        public class SyncFoldersSettings
        {
            public string LocalPath { get; set; }
            public string RemotePath { get; set; }
            public SynchronizationMode Direction { get; set; }
        }
    }
}
