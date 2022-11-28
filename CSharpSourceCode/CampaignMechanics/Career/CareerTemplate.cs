using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using System.Xml.Serialization;
using HarmonyLib;
using NLog.Layouts;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TOR_Core.AbilitySystem;
using TOR_Core.AbilitySystem.Crosshairs;
using TOR_Core.BattleMechanics.DamageSystem;

namespace TOR_Core.CampaignMechanics.Career
{
    [Serializable]
    public class CareerTemplate        //The Serializable Component that stores all passive nodes and key stones, and is load into the game on start up
    {
        [XmlAttribute]
        public CareerId CareerId = CareerId.None;     //might also be just a string ensures, that no typos are made and the right Career template
        [XmlAttribute]
        public string AbilityTemplateId;
        [XmlArray("CareerAbilityWeaponRequirements")] 
        public List<WeaponClass> CareerAbilityWeaponRequirements = new List<WeaponClass>(); 
        [XmlArray("KeyStones")] 
        public List<KeyStoneNode> KeyStoneNodes= new List<KeyStoneNode>();
        [XmlArray("PassiveNodes")]
        public List<PassiveNode> PassiveNodes = new List<PassiveNode>();
        [XmlArray("Structure")]
        public List<SubTree> Structure= new List<SubTree>();
        
        public CareerTemplate()
        {
            
        }
        public CareerTemplate(CareerId id) => CareerId = id;
        
    }
    
    public class CareerTreeNode 
    {
        [XmlAttribute] 
        public string Id="";
        [XmlAttribute]
        public List<string> ParentIDs=new List<string>();
        [XmlAttribute]// The node can have 1 or many parents. If parents are empty it's the root node. The parents only account for setting the "unlockable" state if other conditions are met.
        public TreeNodeState State=TreeNodeState.Locked;
        [XmlAttribute]
        public string DescriptionText="";
        [XmlAttribute]
        public string AttributeCondition=""; // The Node requires the character to posses a certain attribute, like beeing a Questing Knight, to unlock Node
        [XmlAttribute]
        public string UnlockConditionText="";
        [XmlAttribute]
        public List<string> LockNodes=new List<string>(); // On investing into this talent node, the list of  nodes that get blocked
    }



    [Serializable]
    public class PassiveNode : CareerTreeNode
    {
        [XmlAttribute]
        public float Amount=0;
        [XmlAttribute]
        public PassiveEffect EffectType=PassiveEffect.None;
    }
    
    [Serializable]
    public class KeyStoneNode : CareerTreeNode
    {
        [XmlElement] 
        public AbilityTemplateModifier Modifier;
        [XmlElement] 
        public AbilityTemplateOverrides Overrides;
        [XmlAttribute]
        public string CharacterAttribute=""; // Attributes can serve to add new benefits for campaign via models : Example : grailknight: Requires attribute "QuestingKnight" to recruit Questing Knights 
        [XmlAttribute]
        public float AttributeValue; // Not mandatory but potential either a scale factor or addition 
        
    }

    [Serializable]
    public class AbilityTemplateOverrides
    {
        //Override : Based on Hierachical structure, the former effect, is overriden by the new effect. Game Design needs to consider to lock abilities that could be conflicting.
        //null, '""',"none" or Invalid will be ignored upon application of the Modifier 
        [XmlElement(IsNullable = true)]
        public string NameOverride = "";         //Override
        [XmlElement(IsNullable = true)] 
        public string SpriteOverride = "";   //Override
        [XmlElement(IsNullable = true)]  
        public bool? StartsOnCoolDownOverride;    //Override
        [XmlAttribute]  
        public ChargeType ChargeType = ChargeType.Invalid;  //Override
        [XmlElement(IsNullable = true)]  
        public string TriggeredEffectID = "";        //Override
        [XmlElement(IsNullable = true)]  
        public bool? HasLight = null;                //Override
        [XmlElement(IsNullable = true)]  
        public float? LightIntensity = null;           //Override
        [XmlElement(IsNullable = true)]  
        public float? LightRadius = null;              //Override
        [XmlElement(IsNullable = true)]  
        public Vec3? LightColorRGB = null;  //Override
        [XmlElement(IsNullable = true)] 
        public float? LightFlickeringMagnitude = null;  //Override
        [XmlElement(IsNullable = true)] 
        public float? LightFlickeringInterval = null;    //Override
        [XmlElement(IsNullable = true)] 
        public bool? ShadowCastEnabled = true;       //Override
        [XmlElement(IsNullable = true)] 
        public string ParticleEffectPrefab = "";     //Override
        [XmlElement(IsNullable = true)] 
        public string SoundEffectToPlay = "";        //Override
        [XmlElement(IsNullable = true)] 
        public bool? ShouldSoundLoopOverDuration = null; //Override
        [XmlAttribute] 
        public CastType CastType = AbilitySystem.CastType.Invalid;   //Override
        [XmlElement(IsNullable = true)] 
        public string ScriptNameToTrigger = "none";                 //Override
        [XmlElement(IsNullable = true)] 
        public string TroopIdToSummon = "none";                     //override
        [XmlElement(IsNullable = true)] 
        public string BurstParticleEffectPrefab = "none";           //Override
        [XmlElement(IsNullable = true)] 
        public string SoundEffectId = "none";                       //Override
        [XmlElement(IsNullable = true)] 
        public float? SoundEffectLength = null;                      //Override
        [XmlElement(IsNullable = true)]
        public float? ImpactRadius = null;                           //Modifier
        [XmlElement(IsNullable = true)] 
        public bool? HasShockWave = null;                            //Override
        [XmlAttribute] 
        public TargetType TargetType = TargetType.Invalid;          //Override  
        [XmlElement(IsNullable = true)] 
        public string ImbuedStatusEffectID = "none";                //Override
        [XmlAttribute]  
        public DamageType DamageType = DamageType.Invalid;          //Override
        [XmlAttribute] 
        public AbilityTargetType AbilityTargetType = AbilityTargetType.Invalid; //Override
        [XmlAttribute] 
        public string TooltipDescription = "";       //Override, Stack?
    }

    [Serializable]
    public class AbilityTemplateModifier
    {
        //modify : More or less, we are going with addition for easier handling.
        [XmlAttribute] public int Charge = 0;                   //Modifier
        [XmlAttribute] public int ChargeRequirement = 0;        //Modifier
        [XmlAttribute] public int CoolDown = 0;                 //Modifier
        [XmlAttribute] public int Usages =0;                   //Modifier
        [XmlAttribute] public int WindsOfMagicCost = 0;         //Modifier
        [XmlAttribute] public float BaseMisCastChance = 0;      //Modifier
        [XmlAttribute] public float Duration = 0;               //Modifier
        [XmlAttribute] public float Radius = 0;                 //Modifier
        [XmlAttribute] public float BaseMovementSpeed = 0;      //Modifier
        [XmlAttribute] public float CastTime = 0;               //Modifier
        [XmlAttribute] public float Offset = 0;                 //Modifier
        [XmlAttribute] public float MinDistance = 0;            //Modifier
        [XmlAttribute] public float MaxDistance = 0;            //Modifier
        [XmlAttribute] public int Damage;                        //Modifier
        [XmlAttribute] public float ImbuedStatusEffectDuration = 0;               //Modifier
        [XmlAttribute] public int NumberToSummon = 0;                               //modifier
        public AbilityTemplateModifier() { }
    }
    
    [Serializable]
    public class SubTree
    {
        [XmlAttribute] 
        public string Parent= "";
        [XmlArray("Children")] 
        public List<string> Children=new List<string>();
    }
    


    public enum ModificationType
    {
        Add,
        Override,
        ForceOveride
    }
    
    //OnAbilityAttackScript
    public enum CareerId
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

    public enum PassiveEffect
    {
        None,
        HP,         //Health Points
        AP,         //Ammunition Points
        WP,         //Winds of Magic Points
    }

 
}