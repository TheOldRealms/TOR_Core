using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using TaleWorlds.ModuleManager;
using TOR_Core.Utilities;

namespace TOR_Core.BattleMechanics.StatusEffect
{
    public class StatusEffectManager
    {
        private static readonly string EffectsFileName = "tor_statuseffects.xml";
        private static Dictionary<string, StatusEffectTemplate> _idToStatusEffect = new Dictionary<string, StatusEffectTemplate>();

        public static void LoadStatusEffects()
        {
            var ser = new XmlSerializer(typeof(List<StatusEffectTemplate>), new XmlRootAttribute("StatusEffects"));
            var path = TORPaths.TORCoreModuleExtendedDataPath + EffectsFileName;
            if (File.Exists(path))
            {
                var list = ser.Deserialize(File.OpenRead(path)) as List<StatusEffectTemplate>;
                foreach (var effect in list)
                {
                    _idToStatusEffect.Add(effect.Id, effect);
                }
            }
        }

        public static StatusEffect GetStatusEffect(string effectId)
        {
            return new StatusEffect(_idToStatusEffect[effectId]);
        }
    }
}
