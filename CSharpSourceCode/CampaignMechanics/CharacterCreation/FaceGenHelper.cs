using SandBox.Objects.Usables;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignMechanics.CharacterCreation
{
    public static class FaceGenHelper
    {
        private static readonly string SkinFilePath = TORPaths.TORArmoryModuleDataPath + "tor_skins.xml";
        private static XmlDocument SkinDocument;


        public static string GetBeardName(int index, int race, int gender)
        {
            if (SkinDocument == null) LoadSkinsXML();

            if (SkinDocument != null)
            {
                var raceList = SkinDocument.GetElementsByTagName("race");
                var selectedRace = raceList[race];

                var selectedGender = selectedRace.ChildNodes.Item(gender);

                var beards = selectedGender.SelectNodes("beard_meshes/beard_mesh");

                if (beards.Count > 0 && index < beards.Count)
                {
                    if (beards[index].Attributes.GetNamedItem("name") != null)
                    {
                        return beards[index].Attributes["name"].Value;
                    }
                    else return string.Empty;
                }
            }
            
            return null;
        }

        public static string GetHairName(int index, int race, int gender)
        {
            if (SkinDocument == null) LoadSkinsXML();

            if(SkinDocument != null)
            {
                var raceList = SkinDocument.GetElementsByTagName("race");
                var selectedRace = raceList[race];

                var selectedGender = selectedRace.ChildNodes[gender];

                var hairs = selectedGender.SelectNodes("hair_meshes/hair_mesh");

                if (hairs.Count > 0 && index < hairs.Count)
                {
                    if (hairs[index].Attributes.GetNamedItem("name") != null)
                    {
                        return hairs[index].Attributes["name"].Value;
                    }
                }
            }

            return null;
        }

        public static string GetTattooName(int index, int race, int gender)
        {
            if (SkinDocument == null) LoadSkinsXML();

            if(SkinDocument != null)
            {
                var raceList = SkinDocument.GetElementsByTagName("race");
                var selectedRace = raceList[race];

                var selectedGender = selectedRace.ChildNodes.Item(gender);

                var tattoos = selectedGender.SelectNodes("tattoo_materials/tattoo_material");

                if (tattoos.Count > 0 && index < tattoos.Count)
                {
                    if (tattoos[index].Attributes.GetNamedItem("name") != null)
                    {
                        return tattoos[index].Attributes["name"].Value;
                    }
                }
            }

            return null;
        }

        private static void LoadSkinsXML()
        {
            if (File.Exists(SkinFilePath))
            {
                var settings = new XmlReaderSettings();
                settings.IgnoreComments = true;
                var reader = XmlReader.Create(SkinFilePath, settings);
                
                SkinDocument = new XmlDocument();
                SkinDocument.Load(reader);
            }
        }
    }
}
