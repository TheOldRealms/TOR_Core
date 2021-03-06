using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.Library;

namespace TOR_Core.Utilities
{
    public static class TORPaths
    {
        /// <summary>
        /// The root directory of the TOR Core module
        /// </summary>
        public static string TORCoreModuleRootPath
        {
            get { return Path.Combine(BasePath.Name, "Modules/TOR_Core/"); }
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

        public static string TOREnvironmentModuleRootPath
        {
            get { return Path.Combine(BasePath.Name, "Modules/TOR_Environment/"); }
        }

        public static string TOREnvironmentModuleDataPath
        {
            get { return TOREnvironmentModuleRootPath + "ModuleData/"; }
        }
    }
}
