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
        private static readonly Dictionary<string, FactionBannerOverride> _overrides = [];
        private static readonly Random _random = new();

        public static Banner GetRandomBannerFor(string cultureId, string faction = "")
        {
            FactionBannerOverride bannerOverride;
            if (faction == "")
            {
                _overrides.TryGetValue(cultureId, out bannerOverride);
            }
            else if (faction == "player_faction")
            {
                _overrides.TryGetValue(Hero.MainHero.Culture.StringId, out bannerOverride);
            }
            else
            {
                _overrides.TryGetValue(faction, out bannerOverride);
                if (bannerOverride?.Banners?.Count == 0)
                {
                    _overrides.TryGetValue(cultureId, out bannerOverride);
                }
            }

            if (bannerOverride?.Banners?.Count > 0)
            {
                var i = _random.Next(0, bannerOverride.Banners.Count);
                return bannerOverride.Banners[i];
            }
            else return null;
        }

        public static void LoadXML()
        {
            try
            {
                var ser = new XmlSerializer(typeof(List<FactionBannerOverride>));
                var path = TORPaths.TORCoreModuleExtendedDataPath + "tor_factionbanneroverrides.xml";
                var list = ser.Deserialize(File.OpenRead(path)) as List<FactionBannerOverride>;
                foreach (var item in list)
                {
                    if (!_overrides.ContainsKey(item.CultureOrFactionId))
                    {
                        foreach (var item2 in item.BannerCodes)
                        {
                            item.Banners.Add(new Banner(item2));
                        }
                        _overrides.Add(item.CultureOrFactionId, item);
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
    public class FactionBannerOverride
    {
        [XmlAttribute]
        public string CultureOrFactionId { get; set; } = string.Empty;
        [XmlElement(ElementName = "BannerCode")]
        public List<string> BannerCodes { get; set; } = [];
        [XmlIgnore]
        public List<Banner> Banners { get; set; } = [];
    }
}
