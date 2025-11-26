using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TOR_Core.Utilities;
using static TaleWorlds.MountAndBlade.SkinVoiceManager;

namespace TOR_Core.BattleMechanics.Voice
{
    /// <summary>
    /// Manages voice playback for agents using custom audio files to bypass native MakeVoice crashes.
    /// </summary>
    public class TORVoiceManager
    {
        private static TORVoiceManager _instance;
        public static TORVoiceManager Instance => _instance ??= new TORVoiceManager();
        private readonly Dictionary<string, VoiceDefinition> _voiceDefinitions = new();
        private readonly string _voiceDefPath = TORPaths.TORArmoryModuleDataPath + "tor_voice_definitions.xml";

        private TORVoiceManager() { }

        /// <summary>
        /// Loads voice definitions and module sounds from XML files.
        /// </summary>
        public static void Initialize()
        {
            Instance.LoadVoiceDefinitions();
            TORCommon.Log($"VoiceManager initialized. Loaded {Instance._voiceDefinitions.Count} voice definitions.", NLog.LogLevel.Info);
        }

        public string GetVoiceToPlay(Agent agent, SkinVoiceType voiceType)
        {
            // Try to get voice definition from agent's MonsterUsageSetIndex or character
            if (agent.Character != null && agent.Character is BasicCharacterObject character)
            {
                // Check if character has a specific voice definition attribute or tag
                // For now, derive from race name
                var raceName = TaleWorlds.Core.FaceGen.GetRaceNames()[character.Race];

                // Map race names to voice definitions
                // This is a simple implementation - you may want to add custom attributes to characters
                var voiceName = raceName switch
                {
                    "human" => "empire_male_01",
                    "vampire" => "vampire_male_01",
                    "skeleton" => "skeleton_01",
                    "spirit_host" => "skeleton_01",
                    "wraith" => "skeleton_01",
                    "ungor" => "beastmen_01",
                    "chaos_ud_cultist" => "empire_male_01",
                    "marauder" => "empire_male_01",
                    "elf" => "elf_male_01",
                    "large_humanoid_monster" => "tree_spirit_01",
                    "medium_humanoid_monster" => "beastmen_01",
                    "gor" => "beastmen_01",
                    "necrarch" => "vampire_male_01",
                    "dwarf" => "empire_male_01",
                    "orc" => "orc_male_01",
                    "goblin" => "orc_male_01",
                    "troll" => "beastmen_01",
                    _ => "empire_male_01" // Default fallback
                };

                if(character.Culture.StringId == TORConstants.Cultures.BRETONNIA)
                {
                    voiceName = "bretonnia_male_01";
                }

                if (_voiceDefinitions.TryGetValue(voiceName, out var voiceDef))
                {
                    var soundPath = voiceDef.GetSoundPathForVoiceType(voiceType.TypeID);
                    if (!string.IsNullOrEmpty(soundPath))
                    {
                        return soundPath;
                    }
                }
            }

            return "event:/voice/combat/male/01/grunt"; // Default
        }

        private void LoadVoiceDefinitions()
        {
            if (!File.Exists(_voiceDefPath))
            {
                TORCommon.Log($"VoiceManager: Voice definitions file not found at {_voiceDefPath}", NLog.LogLevel.Error);
                return;
            }

            try
            {
                var doc = new XmlDocument();
                doc.Load(_voiceDefPath);

                var voiceDefNodes = doc.SelectNodes("//voice_definition");
                if (voiceDefNodes == null) return;

                foreach (XmlNode node in voiceDefNodes)
                {
                    var voiceDef = ParseVoiceDefinitionNode(node);
                    if (voiceDef != null)
                    {
                        _voiceDefinitions[voiceDef.Name] = voiceDef;
                    }
                }
            }
            catch (Exception ex)
            {
                TORCommon.Log($"VoiceManager: Error loading voice definitions: {ex.Message}", NLog.LogLevel.Error);
            }
        }

        private VoiceDefinition ParseVoiceDefinitionNode(XmlNode node)
        {
            var nameAttr = node.Attributes?["name"];
            if (nameAttr == null) return null;

            var voiceDef = new VoiceDefinition
            {
                Name = nameAttr.Value
            };

            // Parse voice mappings
            var voiceNodes = node.SelectNodes("voice");
            if (voiceNodes != null)
            {
                foreach (XmlNode voiceNode in voiceNodes)
                {
                    var typeAttr = voiceNode.Attributes?["type"];
                    var pathAttr = voiceNode.Attributes?["path"];

                    if (typeAttr != null && pathAttr != null)
                    {
                        voiceDef.VoiceTypeToSoundPath[typeAttr.Value] = pathAttr.Value;
                    }
                }
            }

            return voiceDef;
        }
    }

    internal class VoiceDefinition
    {
        public string Name { get; set; }
        public Dictionary<string, string> VoiceTypeToSoundPath { get; set; } = [];

        public string GetSoundPathForVoiceType(string typeId)
        {
            return VoiceTypeToSoundPath.TryGetValue(typeId, out var path) ? path : null;
        }
    }
}