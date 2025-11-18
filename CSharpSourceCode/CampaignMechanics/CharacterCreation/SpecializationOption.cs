using System;
using System.Xml.Serialization;

namespace TOR_Core.CampaignMechanics.CharacterCreation
{
    /// <summary>
    /// XML-driven specialization option for Stage 4 of character creation.
    /// Uses the same simple structure as CharacterCreationOption for consistency.
    /// </summary>
    [Serializable]
    public class SpecializationOption
    {
        /// <summary>
        /// Unique identifier for this specialization option (e.g., "knight_blazing_sun")
        /// </summary>
        [XmlAttribute]
        public string Id;

        /// <summary>
        /// Required profession ID to show this option (e.g., "option_3_empire_knight")
        /// </summary>
        [XmlAttribute]
        public string ProfessionRequirement;

        /// <summary>
        /// Equipment roster ID for visual preview and final equipment
        /// References MBEquipmentRoster from tor_equipment_sets.xml
        /// </summary>
        [XmlAttribute]
        public string EquipmentSetId;

        /// <summary>
        /// Display name with translation key support
        /// Format: {=translation_key}Default Text
        /// Example: {=knight_blazing_sun_name}Order of the Blazing Sun
        /// </summary>
        public string Name;

        /// <summary>
        /// Description text with translation key support
        /// Format: {=translation_key}Default Text
        /// </summary>
        public string Description;

        /// <summary>
        /// Positive effect text shown in green on UI (optional)
        /// Format: {=translation_key}Default Text
        /// Example: {=knight_blazing_sun_positive}Masters of strategy and warfare
        /// </summary>
        public string PositiveEffect = "";

        /// <summary>
        /// Negative effect text shown in red on UI (optional)
        /// Format: {=translation_key}Default Text
        /// Example: {=priest_ulric_negative}Disdains ranged combat
        /// </summary>
        public string NegativeEffect = "";

        /// <summary>
        /// Skills to increase (array of skill StringIds)
        /// Same format as CharacterCreationOption.SkillsToIncrease
        /// Example: ["OneHanded", "Leadership", "Faith"]
        /// </summary>
        public string[] SkillsToIncrease;

        /// <summary>
        /// Single attribute to increase by 1 point
        /// Same format as CharacterCreationOption.AttributeToIncrease
        /// Valid values: Vigor, Control, Endurance, Cunning, Social, Intelligence
        /// </summary>
        public string AttributeToIncrease;

        /// <summary>
        /// Career to apply (CareerObject StringId)
        /// Used for knight orders, priests, vampires
        /// Example: "KnightBlazingSun", "WarriorPriest", "MinorVampire"
        /// </summary>
        public string CareerId;
    }
}
