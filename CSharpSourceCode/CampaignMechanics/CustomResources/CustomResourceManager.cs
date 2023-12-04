using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TOR_Core.CampaignMechanics.CustomResources
{
    public class CustomResourceManager
    {
        private static CustomResourceManager _instance;
        private Dictionary<string, CustomResource> _resources = new Dictionary<string, CustomResource>();
        private CustomResourceManager() { }

        public static void Initialize() 
        {
            _instance = new CustomResourceManager();
            _instance._resources.Clear();
            _instance._resources.Add("DarkEnergy", 
                new CustomResource("DarkEnergy", "Dark Energy", "Dark Energy is used by practitioners of necromancy to raise and upkeep their undead minions.", ""));
            _instance._resources.Add("WindsOfMagic",
                new CustomResource("WindsOfMagic", "Winds of Magic", "Winds of Magic is used by spellcasters to cast spells.", ""));
        }

        public static CustomResource GetResourceObject(string id)
        {
            if (_instance._resources.TryGetValue(id, out CustomResource resource)) { return resource; }
            else throw new Exception(string.Format("CustomResource with the id of {0} has not been initialized in the manager class.", id));
        }
    }
}
