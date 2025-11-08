using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace TOR_Core.CampaignMechanics.CharacterCreation
{
    [Serializable]
    public class CharacterCreationOption
    {
        [XmlAttribute]
        public string Id;
        [XmlAttribute]
        public string Culture;
        [XmlAttribute]
        public int StageNumber;
        public string[] SkillsToIncrease;
        public string AttributeToIncrease;
        public string OptionText;
        public string OptionFlavourText;
        public string PositiveEffectText = "";
        [XmlAttribute]
        public string EquipmentSetId;
    }
}
