using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.MountAndBlade;

namespace TOR_Core.GameManagers
{
    public static  class InputGameManager
    {
        public static void RegisterCustomKeys()
        {
            TORGameKeyContext torGameKeyContext = new TORGameKeyContext();
            
            HotKeyManager.AddAuxiliaryCategory(torGameKeyContext);
        }
    }
    
}