using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using TaleWorlds.MountAndBlade;
using TOR_Core.Utilities;

namespace TOR_Core.BattleMechanics.StatusEffect
{
    public class StatusEffectManager
    {
        private static readonly string EffectsFileName = "tor_statuseffects.xml";
        private static Dictionary<string, StatusEffectTemplate> _idToStatusEffect = new Dictionary<string, StatusEffectTemplate>();
        private static int _randomCount = 0;
        public static void LoadStatusEffects()
        {
            var ser = new XmlSerializer(typeof(List<StatusEffectTemplate>), new XmlRootAttribute("StatusEffects"));
            var path = TORPaths.TORCoreModuleExtendedDataPath + EffectsFileName;
            if (File.Exists(path))
            {
                var list = ser.Deserialize(File.OpenRead(path)) as List<StatusEffectTemplate>;
                foreach (var effect in list)
                {
                    _idToStatusEffect.Add(effect.StringID, effect);
                }
            }
        }

        public static List<StatusEffectTemplate> GetStatusEffectTemplatesWithIds(List<string> effectIds)
        {
            List<StatusEffectTemplate> result = new List<StatusEffectTemplate>();
            foreach (var item in effectIds)
            {
                StatusEffectTemplate effect = null;
                if (_idToStatusEffect.TryGetValue(item, out effect))
                {
                    result.Add(effect);
                }
            }
            return result;
        }

        public static StatusEffect CreateNewStatusEffect(string effectId, Agent applierAgent, bool requestClone, int castId = -1)
        {
            StatusEffectTemplate template = _idToStatusEffect[effectId];
            if (requestClone) template = (StatusEffectTemplate)template.Clone(effectId + "*cloned*" + (applierAgent?.Index.ToString() ?? "nullAgent") + _randomCount);//if a persistent effect can apply a status, then the applier agent can die and become null before a future status is applied
            if (applierAgent == null)
            {
                TORCommon.Log("StatusEffectManager : null agent when concatenating the id while cloning the effect template", NLog.LogLevel.Info);
                if (effectId != null)
                {
                    TORCommon.Log("Effect id : " + effectId, NLog.LogLevel.Info);
                }
                TORCommon.Log("Template : " + template.ToString(), NLog.LogLevel.Info);
            }
            _randomCount++;
            return new StatusEffect(template, applierAgent, castId);
        }
    }
}