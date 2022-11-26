using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TOR_Core.AbilitySystem;

namespace TOR_Core.CampaignMechanics.Career
{
    public class CareerSkillTreeTemplate        //The Serializable Component that stores all passive nodes and key stones, and is load into the game on start up
    {
        public CareerClass CareerClass = CareerClass.None;     //might also be just a string ensures, that no typos are made and the right Career template
        public List<WeaponClass> CareerAbilityWeaponRequirement; 
        public Dictionary<string, CareerSkillTreeNode> _nodes;
        // Optional, if the said type is not wield, the ability can't be performed.
    }
    
    public class KeyStone : CareerSkillTreeNode
    {
        private AbilityTemplate _template=null;
        
        public string CharacterAttribute=""; // Attributes can serve to add new benefits for campaign via models : Example : grailknight: Requires attribute "QuestingKnight" to recruit Questing Knights 
        public float AttributeValue; // Not mandatory but potential either a scale factor or addition 
    }

    public class PassiveNode : CareerSkillTreeNode
    {
        public float PassiveNodeValue=0;
        public PassiveNodeEffect EffectType=PassiveNodeEffect.None;
    }

    public class CareerSkillTreeNode    //needs to be serializable
    {
        public int order; // nodes with higher order have preference over other nodes in case of a conflict.
        public string id;
        public CareerSkillTreeNode Parent;
        public TreeNodeState state;
        public string DescriptionText;
        public string AttributeCondition; // The Node requires the character to posses a certain attribute, like beeing a Questing Knight, to unlock Node
        public string UnlockConditionText;
   
    }

    
    
    //OnAbilityAttackScript
    public enum CareerClass
    {
        None,
        MinorVampire,
        GrailKnight,
        WarriorPriest
    }

    public enum TreeNodeState
    {
        Locked,
        Available,
        Unlocked
    }

    public enum PassiveNodeEffect
    {
        None,
        HealthPoint,
        Ammunition,
        WindsOfMagic
    }
}