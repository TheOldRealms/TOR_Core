using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.Library;
using TaleWorlds.ModuleManager;
using TaleWorlds.MountAndBlade;

namespace TOR_Core.Utilities
{
    public static class TORPaths
    {
        /// <summary>
        /// The root directory of the TOR Core module
        /// </summary>
        public static string TORCoreModuleRootPath
        {
            get { return ModuleHelper.GetModuleFullPath("TOR_Core"); }
        }

        /// <summary>
        /// The ModuleData directory of the TOR Core module
        /// </summary>
        public static string TORCoreModuleDataPath
        {
            get { return TORCoreModuleRootPath + "ModuleData/"; }
        }

        /// <summary>
        /// The ModuleData/tor_custom_xmls directory of the TOR Core module
        /// </summary>
        public static string TORCoreModuleExtendedDataPath
        {
            get { return TORCoreModuleDataPath + "tor_custom_xmls/"; }
        }

        /// <summary>
        /// The path for the log txt files. Configured for Nlog.
        /// </summary>
        public static string TORLogPath
        {
            get { return TORCoreModuleRootPath + "Logs/${LogHome}${date:format=yyyy}/${date:format=MMMM}/${date:format=dd}/TOR_log${shortdate}.txt"; }
        }

        public static bool IsPlatformSteamWorkshop()
        {
            return TORCoreModuleRootPath.Contains("workshop");
        }
        public static string TOREnvironmentModuleRootPath
        {
            get { return ModuleHelper.GetModuleFullPath("TOR_Environment"); }
        }

        public static string TOREnvironmentModuleDataPath
        {
            get { return TOREnvironmentModuleRootPath + "ModuleData/"; }
        }
    }
}
