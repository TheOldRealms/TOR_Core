using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.ViewModelCollection.GameOptions.GameKeys;

namespace TOR_Core.GameManagers
{
    public static  class InputGameManager
    {
        private static GameKeyOptionCategoryVM KeyOptionCategoryVm;
        public static void RegisterCustomKeys()
        {
            HotKeyManager.Load();

            HotKeyManager.OnKeybindsChanged += Test;
            TORGameKeyContext gameKeyContext=null;
            try
            {
                
                 gameKeyContext = (TORGameKeyContext) HotKeyManager.GetCategory("TOR");
            }
            catch (Exception e)
            {
                
                if (gameKeyContext == null)
                {
                    gameKeyContext = new TORGameKeyContext();
           
                    HotKeyManager.AddAuxiliaryCategory(gameKeyContext);
                }

            }
            
            
            
           
                
            
            
        }

        private static void Test()
        {
            
        }
    }
    
}