using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace TOR_Core.Utilities
{
    public static class TORConfig
    {
        private static TORConfiguration _config = null;
        public static int FakeBannerFrequency => _config.FakeBannerFrequency;
        public static int MaximumCustomResourceValue => _config.MaximumCustomResourceValue;
        public static int NumberOfTroopsPerFormationWithStandard => _config.NumberOfTroopsPerFormationWithStandard;
        public static double YearsToEndEarlyCampaign => _config.YearsToEndEarlyCampaign;
        public static double YearsToEndMidCampaign => _config.YearsToEndMidCampaign;
        public static int NumberOfMaximumLooterPartiesEarly => _config.NumberOfMaximumLooterPartiesEarly;
        public static int NumberOfMaximumLooterParties => _config.NumberOfMaximumLooterParties;
        public static int NumberOfMaximumLooterPartiesLate => _config.NumberOfMaximumLooterPartiesLate;
        public static int NumberOfMaximumBanditPartiesAroundEachHideout => _config.NumberOfMaximumBanditPartiesAroundEachHideout;
        public static int NumberOfMaximumBanditPartiesInEachHideout => _config.NumberOfMaximumBanditPartiesInEachHideout;
        public static int NumberOfInitialHideoutsAtEachBanditFaction => _config.NumberOfInitialHideoutsAtEachBanditFaction;
        public static int NumberOfMaximumHideoutsAtEachBanditFaction => _config.NumberOfMaximumHideoutsAtEachBanditFaction;
        public static int MaximumNumberOfCareerPerkPoints => _config.MaximumNumberOfCareerPerkPoints;
        public static float DeclareWarScoreDistanceMultiplier => _config.DeclareWarScoreDistanceMultiplier;
        public static float DeclareWarScoreFactionStrengthMultiplier => _config.DeclareWarScoreFactionStrengthMultiplier;
        public static float DeclareWarScoreReligiousEffectMultiplier => _config.DeclareWarScoreReligiousEffectMultiplier;
        public static float NumMinKingdomWars => _config.NumMinKingdomWars;
        public static float NumMaxKingdomWars => _config.NumMaxKingdomWars;
        public static float MinPeaceDays => _config.MinPeaceDays;
        public static float MinWarDays => _config.MinWarDays;

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
            public double YearsToEndEarlyCampaign;
            [XmlAttribute]
            public double YearsToEndMidCampaign;
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
            public float DeclareWarScoreDistanceMultiplier;
            [XmlAttribute]
            public float DeclareWarScoreFactionStrengthMultiplier;
            [XmlAttribute]
            public float DeclareWarScoreReligiousEffectMultiplier;
            [XmlAttribute]
            public float NumMinKingdomWars;
            [XmlAttribute]
            public float NumMaxKingdomWars;
            [XmlAttribute]
            public int MinPeaceDays;
            [XmlAttribute]
            public int MinWarDays;
        }
    }
}
