using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TOR_Core.Utilities;

namespace TOR_Core.BattleMechanics.Banners
{
    public static class CustomBannerManager
    {
        private static Dictionary<string, List<Banner>> _patterns = new Dictionary<string, List<Banner>>();
        private static Random _random = new Random();

        public static Banner GetRandomBannerFor(string cultureId, string faction = "")
        {
            List<Banner> banners = null;
            if (faction == "")
            {
                _patterns.TryGetValue(cultureId, out banners);
            }
            else if (faction == "player_faction")
            {
                _patterns.TryGetValue(Hero.MainHero.Culture.StringId, out banners);
            }
            else
            {
                _patterns.TryGetValue(faction, out banners);
                if (banners == null || banners.Count == 0)
                {
                    _patterns.TryGetValue(cultureId, out banners);
                }
            }

            if (banners != null && banners.Count > 0)
            {
                var i = _random.Next(0, banners.Count);
                return banners[i];
            }
            else return null;
        }

        public static void LoadXML()
        {
            try
            {
                var ser = new XmlSerializer(typeof(List<ShieldPattern>));
                var path = TORPaths.TORCoreModuleExtendedDataPath + "tor_shieldpatterns.xml";
                var list = ser.Deserialize(File.OpenRead(path)) as List<ShieldPattern>;
                foreach (var item in list)
                {
                    if (!_patterns.ContainsKey(item.CultureOrKingdomId))
                    {
                        _patterns.Add(item.CultureOrKingdomId, new List<Banner>());
                    }

                    foreach (var item2 in item.BannerCodes)
                    {
                        _patterns[item.CultureOrKingdomId].Add(new Banner(item2));
                    }
                }
            }
            catch
            {
                TORCommon.Log("Attempted to load shield patterns but failed.", NLog.LogLevel.Error);
            }
        }
    }

    [Serializable]
    public class ShieldPattern
    {
        [XmlElement]
        public string CultureOrKingdomId { get; set; } = "";
        [XmlElement(ElementName = "BannerCode")]
        public List<string> BannerCodes { get; set; } = new List<string>();
    }
}
