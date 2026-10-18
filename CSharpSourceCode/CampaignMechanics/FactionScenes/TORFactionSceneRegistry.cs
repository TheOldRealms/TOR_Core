using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using TaleWorlds.CampaignSystem;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignMechanics.FactionScenes
{
    public static class TORFactionSceneRegistry
    {
        private const string FileName = "tor_faction_scenes.xml";

        private static List<FactionSceneEntry> _entries = new List<FactionSceneEntry>();
        private static bool _loaded;

        public static void Load()
        {
            _entries = new List<FactionSceneEntry>();
            _loaded = true;

            string path = TORPaths.TORCoreModuleDataPath + FileName;
            if (!File.Exists(path))
            {
                TORCommon.Log(
                    "TORFactionSceneRegistry: " + FileName + " not found; no scene overrides active.",
                    NLog.LogLevel.Info);
                return;
            }

            XDocument doc;
            try
            {
                doc = XDocument.Load(path);
            }
            catch (Exception e)
            {
                TORCommon.Log(
                    "TORFactionSceneRegistry: failed to parse " + FileName + ": " + e.Message,
                    NLog.LogLevel.Error);
                return;
            }

            foreach (var sceneEl in doc.Descendants("Scene"))
            {
                var entry = new FactionSceneEntry
                {
                    Id = (string)sceneEl.Attribute("id"),
                    Trigger = (string)sceneEl.Attribute("trigger"),
                    SceneId = (string)sceneEl.Attribute("scene_id"),
                    Culture = (string)sceneEl.Attribute("culture"),
                };

                if (string.IsNullOrEmpty(entry.Id) ||
                    string.IsNullOrEmpty(entry.Trigger) ||
                    string.IsNullOrEmpty(entry.SceneId))
                {
                    TORCommon.Log(
                        "TORFactionSceneRegistry: skipping entry missing required attribute (id/trigger/scene_id).",
                        NLog.LogLevel.Warn);
                    continue;
                }

                _entries.Add(entry);
            }

            TORCommon.Log(
                "TORFactionSceneRegistry: loaded " + _entries.Count + " entries from " + FileName + ".",
                NLog.LogLevel.Info);
        }

        public static string Resolve(string trigger, Kingdom kingdom)
        {
            if (!_loaded) Load();
            if (string.IsNullOrEmpty(trigger)) return null;

            string cultureId = kingdom?.Culture?.StringId;

            foreach (var e in _entries)
            {
                if (!string.Equals(e.Trigger, trigger, StringComparison.Ordinal)) continue;
                if (!string.IsNullOrEmpty(e.Culture) &&
                    !string.Equals(e.Culture, cultureId, StringComparison.Ordinal)) continue;
                return e.SceneId;
            }
            return null;
        }
    }
}
