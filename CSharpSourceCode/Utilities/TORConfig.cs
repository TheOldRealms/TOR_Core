using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace TOR_Core.Utilities
{
    public static class TORConfig
    {
        private static TORConfiguration _config = null;
        public static int FakeBannerFrequency
        {
            get => _config.FakeBannerFrequency;
            set => _config.FakeBannerFrequency = value;
        }

        public static int MaximumCustomResourceValue => _config.MaximumCustomResourceValue;
        public static int NumberOfTroopsPerFormationWithStandard
        {
            get => _config.NumberOfTroopsPerFormationWithStandard;
            set => _config.NumberOfTroopsPerFormationWithStandard = value;
        }

        public static double YearsToEndEarlyCampaign => _config.YearsToEndEarlyCampaign;
        public static double YearsToEndMidCampaign => _config.YearsToEndMidCampaign;
        public static int NumberOfMaximumLooterPartiesEarly
        {
            get => _config.NumberOfMaximumLooterPartiesEarly;
            set => _config.NumberOfMaximumLooterPartiesEarly = value;
        }

        public static int NumberOfMaximumLooterParties
        {
            get => _config.NumberOfMaximumLooterParties;
            set => _config.NumberOfMaximumLooterParties = value;
        }

        public static int NumberOfMaximumLooterPartiesLate
        {
            get => _config.NumberOfMaximumLooterPartiesLate;
            set => _config.NumberOfMaximumLooterPartiesLate = value;
        }

        public static int NumberOfMaximumBanditPartiesAroundEachHideout
        {
            get => _config.NumberOfMaximumBanditPartiesAroundEachHideout;
            set => _config.NumberOfMaximumBanditPartiesAroundEachHideout = value;
        }

        public static int NumberOfMaximumBanditPartiesInEachHideout
        {
            get => _config.NumberOfMaximumBanditPartiesInEachHideout;
            set => _config.NumberOfMaximumBanditPartiesInEachHideout = value;
        }

        public static int NumberOfInitialHideoutsAtEachBanditFaction
        {
            get => _config.NumberOfInitialHideoutsAtEachBanditFaction;
            set => _config.NumberOfInitialHideoutsAtEachBanditFaction = value;
        }

        public static int NumberOfMaximumHideoutsAtEachBanditFaction
        {
            get => _config.NumberOfMaximumHideoutsAtEachBanditFaction;
            set => _config.NumberOfMaximumHideoutsAtEachBanditFaction = value;
        }

        public static int MaximumNumberOfCareerPerkPoints => _config.MaximumNumberOfCareerPerkPoints;
        public static float DeclareWarScoreDistanceMultiplier
        {
            get => _config.DeclareWarScoreDistanceMultiplier;
            set => _config.DeclareWarScoreDistanceMultiplier = value;
        }

        public static float DeclareWarScoreFactionStrengthMultiplier
        {
            get => _config.DeclareWarScoreFactionStrengthMultiplier;
            set => _config.DeclareWarScoreFactionStrengthMultiplier = value;
        }

        public static float DeclareWarScoreReligiousEffectMultiplier
        {
            get => _config.DeclareWarScoreReligiousEffectMultiplier;
            set => _config.DeclareWarScoreReligiousEffectMultiplier = value;
        }

        public static int NumMinKingdomWars
        {
            get => _config.NumMinKingdomWars;
            set => _config.NumMinKingdomWars = value;
        }

        public static int NumMaxKingdomWars
        {
            get => _config.NumMaxKingdomWars;
            set => _config.NumMaxKingdomWars = value;
        }

        public static int MinPeaceDays
        {
            get => _config.MinPeaceDays;
            set => _config.MinPeaceDays = value;
        }

        public static int MinWarDays
        {
            get => _config.MinWarDays;
            set => _config.MinWarDays = value;
        }

        public static void SetDefaultValues()
        {
            _config = new TORConfiguration
            {
                FakeBannerFrequency = 10,
                MaximumCustomResourceValue = 2500,
                NumberOfTroopsPerFormationWithStandard = 1,
                YearsToEndEarlyCampaign = 1,
                YearsToEndMidCampaign = 10.5,
                NumberOfMaximumLooterPartiesEarly = 150,
                NumberOfMaximumLooterParties = 250,
                NumberOfMaximumLooterPartiesLate = 350,
                NumberOfMaximumBanditPartiesAroundEachHideout = 50,
                NumberOfMaximumBanditPartiesInEachHideout = 3,
                NumberOfInitialHideoutsAtEachBanditFaction = 10,
                NumberOfMaximumHideoutsAtEachBanditFaction = 60,
                MaximumNumberOfCareerPerkPoints = 30,
                DeclareWarScoreDistanceMultiplier = 50,
                DeclareWarScoreFactionStrengthMultiplier = 0.5f,
                DeclareWarScoreReligiousEffectMultiplier = 5,
                NumMinKingdomWars = 0,
                NumMaxKingdomWars = 2,
                MinPeaceDays = 20,
                MinWarDays = 40
            };
        }
        public static void ReadConfig()
        {
            try
            {
                var ser = new XmlSerializer(typeof(TORConfiguration));
                var path = TORPaths.TORCoreModuleExtendedDataPath + "tor_config.xml";

                if (!File.Exists(path))
                {
                    SetDefaultValues();
                    UpdateConfiguraiton();
                }
                else
                {
                    _config = ser.Deserialize(File.OpenRead(path)) as TORConfiguration;
                }

                
            }
            catch(Exception e)
            {
                TORCommon.Log("TOR Config file read failed.", NLog.LogLevel.Error);
                throw(e);
            }
        }

        public static void UpdateConfiguraiton()
        {
            try
            {
                var ser = new XmlSerializer(typeof(TORConfiguration));
                XmlWriterSettings settings = new XmlWriterSettings()
                {
                    NewLineOnAttributes = true,
                    Indent = true,
                    OmitXmlDeclaration = true
                };
                
                var path = TORPaths.TORCoreModuleExtendedDataPath + "tor_config.xml";

                using var writer = XmlWriter.Create(path, settings);
                ser.Serialize(writer,_config);

            }
            catch (Exception e)
            {
                Console.WriteLine("saving TOR config file failed");
                throw;
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
            public int NumMinKingdomWars;
            [XmlAttribute]
            public int NumMaxKingdomWars;
            [XmlAttribute]
            public int MinPeaceDays;
            [XmlAttribute]
            public int MinWarDays;
        }
    }
}
