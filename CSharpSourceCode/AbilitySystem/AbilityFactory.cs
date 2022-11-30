using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using TaleWorlds.MountAndBlade;
using TOR_Core.AbilitySystem.Crosshairs;
using TOR_Core.AbilitySystem.Spells;
using TOR_Core.Utilities;

namespace TOR_Core.AbilitySystem
{
    public class AbilityFactory
    {
        private static Dictionary<string, AbilityTemplate> _templates = new Dictionary<string, AbilityTemplate>();
        private static string _filename = "tor_abilitytemplates.xml";

        public static List<string> GetAllSpellNamesAsList()
        {
            List<string> list = new List<string>();
            var q = _templates.Distinct().Where(x => x.Value.AbilityType == AbilityType.Spell);
            foreach (var template in q)
            {
                list.Add(template.Value.StringID);
            }
            return list;
        }
        
        public static List<string> GetAllCareerAbilityNamesAsList()
        {
            List<string> list = new List<string>();
            var q = _templates.Distinct().Where(x => x.Value.AbilityType == AbilityType.CareerAbility);
            foreach (var template in q)
            {
                list.Add(template.Value.StringID);
            }
            return list;
        }

        public static List<AbilityTemplate> GetAllTemplates()
        {
            var list = new List<AbilityTemplate>();
            foreach(var template in _templates.Values) list.Add(template);
            return list;
        }

        public static AbilityTemplate GetTemplate(string id)
        {
            return _templates.ContainsKey(id) ? _templates[id] : null;
        }

        public static void LoadTemplates()
        {
            var ser = new XmlSerializer(typeof(List<AbilityTemplate>));
            var path = TORPaths.TORCoreModuleExtendedDataPath + _filename;
            if (File.Exists(path))
            {
                var list = ser.Deserialize(File.OpenRead(path)) as List<AbilityTemplate>;
                foreach (var item in list)
                {
                    _templates.Add(item.StringID, item);
                }
            }
        }

        public static Ability CreateNew(string id, Agent caster)
        {
            Ability ability = null;
            if (_templates.ContainsKey(id))
            {
                ability = InitializeAbility(_templates[id], caster);
            }
            return ability;
        }

        private static Ability InitializeAbility(AbilityTemplate template, Agent caster)
        {
            Ability ability = null;

            switch (template.AbilityType)
            {
                case AbilityType.Spell:
                    ability = new Spell(template);
                    break;
                case AbilityType.Prayer:
                    ability = new Prayer(template);
                    break;
                case AbilityType.ItemBound:
                    ability = new ItemBoundAbility(template);
                    break;
                case AbilityType.CareerAbility:
                    ability = new CareerAbility(template,caster);
                    break;
            }
            return ability;
        }

        public static AbilityCrosshair InitializeCrosshair(AbilityTemplate template)
        {
            AbilityCrosshair crosshair = null;
            switch (template.CrosshairType)
            {
                case CrosshairType.Missile:
                    {
                        crosshair = new MissileCrosshair(template);
                        break;
                    }
                case CrosshairType.SingleTarget:
                    {
                        crosshair = new SingleTargetCrosshair(template);
                        break;
                    }
                case CrosshairType.Wind:
                    {
                        crosshair = new WindCrosshair(template);
                        break;
                    }
                case CrosshairType.Pointer:
                    {
                        crosshair = new Pointer(template);
                        break;
                    }
                case CrosshairType.TargetedAOE:
                    {
                        crosshair = new TargetedAOECrosshair(template);
                        break;
                    }
                case CrosshairType.Self:
                    {
                        crosshair = new SelfCrosshair(template);
                        break;
                    }
            }
            return crosshair;
        }
    }
}
