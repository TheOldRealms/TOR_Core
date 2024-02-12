using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using TaleWorlds.Core;
using TOR_Core.BattleMechanics.Banners;

namespace TOR_Core.Utilities
{
    public static class TORConfig
    {
        private static TORConfiguration _config = null;
        public static int FakeBannerFrequency => _config.FakeBannerFrequency;
        public static int MaximumCustomResourceValue => _config.MaximumCustomResourceValue;
        public static int NumberOfTroopsPerFormationWithStandard => _config.NumberOfTroopsPerFormationWithStandard;
        public static int NumberOfMaximumLooterPartiesEarly => _config.NumberOfMaximumLooterPartiesEarly;
        public static int NumberOfMaximumLooterParties => _config.NumberOfMaximumLooterParties;
        public static int NumberOfMaximumLooterPartiesLate => _config.NumberOfMaximumLooterPartiesLate;
        public static int NumberOfMaximumBanditPartiesAroundEachHideout => _config.NumberOfMaximumBanditPartiesAroundEachHideout;
        public static int NumberOfMaximumBanditPartiesInEachHideout => _config.NumberOfMaximumBanditPartiesInEachHideout;
        public static int NumberOfInitialHideoutsAtEachBanditFaction => _config.NumberOfInitialHideoutsAtEachBanditFaction;
        public static int NumberOfMaximumHideoutsAtEachBanditFaction => _config.NumberOfMaximumHideoutsAtEachBanditFaction;
        public static int MaximumNumberOfCareerPerkPoints => _config.MaximumNumberOfCareerPerkPoints;
        public static float DeclareWarScoreMultiplierTor => _config.DeclareWarScoreMultiplierTor;
        public static float DeclareWarScoreMultiplierNative => _config.DeclareWarScoreMultiplierNative;
        public static float DeclarePeaceMultiplier => _config.DeclarePeaceMultiplier;

        public static void ReadConfig()
        {
            try
            {
                var ser = new XmlSerializer(typeof(TORConfiguration));
                var path = TORPaths.TORCoreModuleExtendedDataPath + "tor_config.xml";
                _config = ser.Deserialize(File.OpenRead(path)) as TORConfiguration;
            }
            catch(Exception e)
            {
                TORCommon.Log("TOR Config file read failed.", NLog.LogLevel.Error);
                throw(e);
            }
        }

        [Serializable]
        public class TORConfiguration
        {
            [XmlAttribute]
            public int FakeBannerFrequency;
            [XmlAttribute]
            public int MaximumCustomResourceValue;
            [XmlAttribute]
            public int NumberOfTroopsPerFormationWithStandard;
            [XmlAttribute]
            public int NumberOfMaximumLooterPartiesEarly;
            [XmlAttribute]
            public int NumberOfMaximumLooterParties;
            [XmlAttribute]
            public int NumberOfMaximumLooterPartiesLate;
            [XmlAttribute]
            public int NumberOfMaximumBanditPartiesAroundEachHideout;
            [XmlAttribute]
            public int NumberOfMaximumBanditPartiesInEachHideout;
            [XmlAttribute]
            public int NumberOfInitialHideoutsAtEachBanditFaction;
            [XmlAttribute]
            public int NumberOfMaximumHideoutsAtEachBanditFaction;
            [XmlAttribute]
            public int MaximumNumberOfCareerPerkPoints;
            [XmlAttribute]
            public float DeclareWarScoreMultiplierTor;
            [XmlAttribute]
            public float DeclareWarScoreMultiplierNative;
            [XmlAttribute]
            public float DeclarePeaceMultiplier;
        }
    }
}
