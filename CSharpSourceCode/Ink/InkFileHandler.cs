using Ink;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TOR_Core.Utilities;

namespace TOR_Core.Ink
{
    public class InkFileHandler : IFileHandler
    {
        public string LoadInkFileContents(string fullFilename)
        {
            if (File.Exists(fullFilename))
            {
                return File.ReadAllText(fullFilename);
            }
            return string.Empty;
        }

        public string ResolveInkFilename(string includeName)
        {
            var path = TORPaths.TORCoreModuleRootPath + "InkStories" + Path.DirectorySeparatorChar + includeName;
            if (File.Exists(path))
            {
                return path;
            }
            return string.Empty;
        }
    }
}