using HarmonyLib;
using System.Xml;
using System.Xml.Xsl;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;
using TOR_Core.CharacterDevelopment;

namespace TOR_Core.HarmonyPatches
{
    [HarmonyPatch]
    public static class ObjectManagerPatches
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(MBObjectManager), "GetMergedXmlForManaged")]
        public static bool SkipValidation(string id, ref bool skipValidation)
        {
            if (id == "Settlements" || id == "Religions")
            {
                skipValidation = true;
            }
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(MBObjectManager), "ApplyXslt")]
        public static bool EnableTrustedXslt(ref XmlDocument __result, string xsltPath, XmlDocument baseDocument)
        {
            XmlReader input = new XmlNodeReader(baseDocument);
            XslCompiledTransform xslCompiledTransform = new XslCompiledTransform();
            xslCompiledTransform.Load(xsltPath, XsltSettings.TrustedXslt, null);
            XmlDocument xmlDocument = new XmlDocument(baseDocument.CreateNavigator().NameTable);
            using XmlWriter xmlWriter = xmlDocument.CreateNavigator().AppendChild();
            xslCompiledTransform.Transform(input, xmlWriter);
            xmlWriter.Close();
            __result = xmlDocument;
            return false;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Game), "InitializeDefaultGameObjects")]
        public static void LoadAdditionalSkillsAndAttributes()
        {
            _ = new TORAttributes();
            _ = new TORSkills();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Campaign), "InitializeDefaultCampaignObjects")]
        public static void LoadAdditionalCampaignObjects()
        {
            _ = new TORSkillEffects();
            _ = new TORCharacterTraits();
            _ = new TORPerks();
        }
    }
}
