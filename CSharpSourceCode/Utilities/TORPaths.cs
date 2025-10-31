using TaleWorlds.ModuleManager;

namespace TOR_Core.Utilities
{
    public static class TORPaths
    {
        /// <summary>
        /// Gets the root directory path of the TOR Core module on disk.
        /// </summary>
        /// <returns>The absolute module folder path for the "TOR_Core" module.</returns>
        public static string TORCoreModuleRootPath
        {
            get { return ModuleHelper.GetModuleFullPath("TOR_Core"); }
        }

        /// <summary>
        /// Gets the path to the ModuleData directory inside the TOR Core module.
        /// </summary>
        /// <returns>Path to the "ModuleData/" folder for the TOR Core module.</returns>
        public static string TORCoreModuleDataPath
        {
            get { return TORCoreModuleRootPath + "ModuleData/"; }
        }

        /// <summary>
        /// Gets the path to the extended custom XMLs directory for TOR Core.
        /// </summary>
        /// <returns>Path to the "ModuleData/tor_custom_xmls/" folder for custom XML data.</returns>
        public static string TORCoreModuleExtendedDataPath
        {
            get { return TORCoreModuleDataPath + "tor_custom_xmls/"; }
        }

        /// <summary>
        /// Gets the configured log file path pattern for TOR Core.
        /// </summary>
        /// <returns>An NLog-style path pattern used to generate dated log files under the module folder.</returns>
        /// <remarks>
        /// The returned string contains NLog layout renderers (for example ${date}) and is intended for use directly in
        /// the NLog configuration to resolve actual log file paths at runtime.
        /// </remarks>
        public static string TORLogPath
        {
            get { return TORCoreModuleRootPath + "Logs/${LogHome}${date:format=yyyy}/${date:format=MMMM}/${date:format=dd}/TOR_log${shortdate}.txt"; }
        }

        /// <summary>
        /// Determines whether the TOR Core module is located in a Steam Workshop path.
        /// </summary>
        /// <returns>True when the module root path contains the substring "workshop"; otherwise false.</returns>
        public static bool IsPlatformSteamWorkshop()
        {
            return TORCoreModuleRootPath.Contains("workshop");
        }

        /// <summary>
        /// Gets the root directory path of the TOR Environment module.
        /// </summary>
        /// <returns>The absolute module folder path for the "TOR_Environment" module.</returns>
        public static string TOREnvironmentModuleRootPath
        {
            get { return ModuleHelper.GetModuleFullPath("TOR_Environment"); }
        }

        /// <summary>
        /// Gets the path to the ModuleData directory inside the TOR Environment module.
        /// </summary>
        /// <returns>Path to the "ModuleData/" folder for the TOR Environment module.</returns>
        public static string TOREnvironmentModuleDataPath
        {
            get { return TOREnvironmentModuleRootPath + "ModuleData/"; }
        }

        /// <summary>
        /// Gets the root directory path of the TOR Armory module.
        /// </summary>
        /// <returns>The absolute module folder path for the "TOR_Armory" module.</returns>
        public static string TORArmoryModuleRootPath
        {
            get { return ModuleHelper.GetModuleFullPath("TOR_Armory"); }
        }

        /// <summary>
        /// Gets the path to the ModuleData directory inside the TOR Armory module.
        /// </summary>
        /// <returns>Path to the "ModuleData/" folder for the TOR Armory module.</returns>
        public static string TORArmoryModuleDataPath
        {
            get { return TORArmoryModuleRootPath + "ModuleData/"; }
        }
    }
}
