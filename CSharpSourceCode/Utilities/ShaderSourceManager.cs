using NLog;
using System;
using System.IO;

namespace TOR_Core.Utilities
{
    /// <summary>
    /// Manages copying custom shader source files from TOR_Armory to the base game's Shaders folder.
    /// This is necessary because the game engine only looks in Bannerlord/Shaders/Sources/ for shader sources.
    /// </summary>
    public static class ShaderSourceManager
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Copies TOR_Armory shader source files to the base game's Shaders/Sources folder.
        /// Should be called early in module initialization before any shaders are compiled.
        /// </summary>
        public static void CopyShaderSourcesToGame()
        {
            try
            {
                string armoryShaderSourcePath = TORPaths.TORArmoryModuleRootPath + "Shaders/Sources/";
                string gameShaderSourcePath = TaleWorlds.Engine.Utilities.GetBasePath() + "Shaders/Sources/";

                if (!Directory.Exists(armoryShaderSourcePath))
                {
                    Log.Warn($"TOR_Armory shader source folder not found: {armoryShaderSourcePath}");
                    return;
                }

                if (!Directory.Exists(gameShaderSourcePath))
                {
                    Log.Error($"Game shader source folder not found: {gameShaderSourcePath}");
                    return;
                }

                int copiedCount = 0;

                // Copy .rs files (shader definitions)
                foreach (var sourceFile in Directory.GetFiles(armoryShaderSourcePath, "*.rs"))
                {
                    if (CopyShaderFile(sourceFile, gameShaderSourcePath))
                        copiedCount++;
                }

                // Copy .rsh files (shader includes/headers)
                foreach (var sourceFile in Directory.GetFiles(armoryShaderSourcePath, "*.rsh"))
                {
                    if (CopyShaderFile(sourceFile, gameShaderSourcePath))
                        copiedCount++;
                }

                if (copiedCount > 0)
                {
                    Log.Info($"Copied {copiedCount} TOR shader source files to game folder.");
                }
                else
                {
                    Log.Debug("No TOR shader source files needed to be copied.");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to copy TOR shader sources to game folder.");
            }
        }

        private static bool CopyShaderFile(string sourceFile, string targetDirectory)
        {
            try
            {
                string fileName = Path.GetFileName(sourceFile);
                string targetFile = Path.Combine(targetDirectory, fileName);

                // Check if we need to copy (file doesn't exist or is different)
                bool needsCopy = !File.Exists(targetFile);

                if (!needsCopy)
                {
                    // Compare file sizes and modification times to determine if update is needed
                    var sourceInfo = new FileInfo(sourceFile);
                    var targetInfo = new FileInfo(targetFile);

                    needsCopy = sourceInfo.Length != targetInfo.Length ||
                                sourceInfo.LastWriteTimeUtc > targetInfo.LastWriteTimeUtc;
                }

                if (needsCopy)
                {
                    File.Copy(sourceFile, targetFile, overwrite: true);
                    Log.Debug($"Copied shader source: {fileName}");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Log.Warn(ex, $"Failed to copy shader file: {sourceFile}");
                return false;
            }
        }

        /// <summary>
        /// Removes TOR shader source files from the base game's Shaders/Sources folder.
        /// Can be called during cleanup/uninstall.
        /// </summary>
        public static void RemoveShaderSourcesFromGame()
        {
            try
            {
                string armoryShaderSourcePath = TORPaths.TORArmoryModuleRootPath + "Shaders/Sources/";
                string gameShaderSourcePath = TaleWorlds.Engine.Utilities.GetBasePath() + "Shaders/Sources/";

                if (!Directory.Exists(armoryShaderSourcePath) || !Directory.Exists(gameShaderSourcePath))
                    return;

                int removedCount = 0;

                // Get list of TOR shader files
                var torFiles = Directory.GetFiles(armoryShaderSourcePath, "*.rs");
                foreach (var sourceFile in torFiles)
                {
                    string fileName = Path.GetFileName(sourceFile);
                    string targetFile = Path.Combine(gameShaderSourcePath, fileName);

                    if (File.Exists(targetFile))
                    {
                        File.Delete(targetFile);
                        removedCount++;
                    }
                }

                var torHeaderFiles = Directory.GetFiles(armoryShaderSourcePath, "*.rsh");
                foreach (var sourceFile in torHeaderFiles)
                {
                    string fileName = Path.GetFileName(sourceFile);
                    string targetFile = Path.Combine(gameShaderSourcePath, fileName);

                    if (File.Exists(targetFile))
                    {
                        File.Delete(targetFile);
                        removedCount++;
                    }
                }

                if (removedCount > 0)
                {
                    Log.Info($"Removed {removedCount} TOR shader source files from game folder.");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to remove TOR shader sources from game folder.");
            }
        }
    }
}
