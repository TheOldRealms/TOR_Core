using System;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace TOR_Core.CampaignMechanics
{
    public class SpecialbuttonEventManagerHandler
    { 
        public event EventHandler<TroopEventArgs> ButtonClickedEventHandler;
        
        private static SpecialbuttonEventManagerHandler _instance;
        private SpecialbuttonEventManagerHandler()
        {
            
        }

        public static SpecialbuttonEventManagerHandler Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new SpecialbuttonEventManagerHandler();
                }
                return _instance;
            }
        }
        
        
        public void OnButtonClicked(string troopID)
        {

            
            var e= new TroopEventArgs();
            e.TroopId = troopID;
            
            ButtonClickedEventHandler(this,e);
        }
        
    
    }

    public class TroopEventArgs: EventArgs
    {
        public string TroopId { get; set; }
    }
}