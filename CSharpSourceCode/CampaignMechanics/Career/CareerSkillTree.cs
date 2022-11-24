namespace TOR_Core.CampaignMechanics.Career
{
    public class CareerSkillTree
    {
        
    }

    public class CareerSkillTreeNode
    {
        public CareerSkillTreeNode Parent;
        public int PassiveNodeValue=0;
        public ImbuedPassiveNodeEffect None;
        public string CharacterAttribute;
        public string DescriptionText;
    }

    public enum ImbuedPassiveNodeEffect
    {
        None,
        Healthpoint,
        Ammunition,
        WindsofMagic
    }
}