using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using System.Xml.Serialization;
using HarmonyLib;
using NLog.Filters;
using NLog.Layouts;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.SaveSystem;
using TOR_Core.AbilitySystem;
using TOR_Core.AbilitySystem.Crosshairs;
using TOR_Core.AbilitySystem.Spells;
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
        [XmlAttribute]
        public DamageType DanageType = DamageType.Physical;
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
        [XmlAttribute] public int Damage = 0;                       //Modifier
        [XmlAttribute] public float ImpactRadius = 0;             //Modifier
        [XmlAttribute] public float ImbuedStatusEffectDuration = 0;               //Modifier
        [XmlAttribute] public int NumberToSummon = 0;                               //modifier
        public AbilityTemplateModifier() { }
    }


    
    
    [Serializable]
    public class SubTree
    {
        public int Level;
        [XmlAttribute] 
        public string Parent= "";
        [XmlArray("Children")] 
        public List<string> Children=new List<string>();

        public bool Contains(string element)
        {
            if (Parent == element || Children.Contains(element))
                return true;
            else
                return false;
        }
        
        

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
        MD,         //Melee Damage
        RD,         //Range Damage
        SD,         //Spell Damage
    }
    
    
    public static class TreeStructureExtension
    {
        public const string RootNode = "0";

        public static string GetRootNodeId(this List<SubTree> TreeStructure)
        {
            return RootNode;
        }
        public static bool ContainsNode(this List<SubTree> TreeStructure, string element)
        {
             return TreeStructure.Any(subtree => subtree.Contains(element));
        }

        public static void AddChild(this List<SubTree> treeStructure,string parent, string element)
        {
            
        }

        public static List<SubTree> GetAllParents(this List<SubTree> TreeStructure, string element)
        {
            return TreeStructure.Where(x=> x.Children.Contains(element)).ToList();
        }
        
        /*public static List<SubTree> GetAllChildren(this List<SubTree> TreeStructure, string element)
        {
            return TreeStructure.Where(x=> x.Parent==(element)).ToList();
        }*/


        public static bool IsChildOfAny(this List<SubTree> TreeStructure, string element)
        {
            return TreeStructure.Any(subtree => subtree.Children.Contains(element));
        }

        public static SubTree GetSubTreeWithNode(this List<SubTree> TreeStructre, string element)
        {
            if (element == RootNode) return null;
            
            return TreeStructre.FirstOrDefault(x => x.Parent.Contains(element));
        }
        

        public static string HigherNode(this List<SubTree> TreeStructure, string node , string node2)
        {
            if (!TreeStructure.ContainsNode(node) || !TreeStructure.ContainsNode(node2)) return null;
            
            
            var element = TreeStructure.GetSubTreeWithNode(node);
            var element2 = TreeStructure.GetSubTreeWithNode(node2);
            if (element.Level > element2.Level)
            {
                    
            }
            return null;
        }

        public static List<string> GetAllChildren(this List<SubTree> treeStructure)
        {
            List<string> elements=new List<string>();
            foreach (var tree in treeStructure)
            { 
                elements.AddRange(tree.Children);
            }

            return elements.Distinct().ToList();
        }

        public static List<SubTree> GetAllLeaves(this List<SubTree> treeStructure)
        {
            var leaves = treeStructure.Where(subtree => subtree.Children.Count == 0).ToList();
            return leaves;
        }


        public static List<SubTree> GetSubTree(this List<SubTree> treeStructure, SubTree subTree, int index)
        {
            subTree.Level = index;

            
            foreach (var id in subTree.Children)
            {
                if (treeStructure.ContainsNode(id))
                {
                    return treeStructure.GetSubTree(GetSubTreeWithNode(treeStructure, id), index++).ToList();
                }
            }
            
            return new List<SubTree>();


        }
        
        public static List<string> GetAllLeaveIDs(this List<SubTree> treeStructure)
        {
            var leaves = treeStructure.Where(subtree => subtree.Children.Count == 0).ToList();
            return leaves.Select(tree => tree.Parent).ToList();

        }
        
        public static List<SubTree> GetParents(this List<SubTree> treeStructure)
        {
            var leaves = treeStructure.Where(subtree => subtree.Children.Count > 0).ToList();
            return leaves;
        }

       private static  List<SubTree> FinalizeTreeStructure(this List<SubTree> treeStructure)
       {

           

           //treeStructure.Insert(0,rootnode);

            var children = treeStructure.GetAllChildren();
            var leaves = children.Where(child => treeStructure.All(x => x.Parent != child)).ToList();

            foreach (var leaf in leaves)
            {
                SubTree newElement = new SubTree();
                newElement.Parent = leaf;
                treeStructure.Add(newElement);
            }
            return treeStructure;
        }
        
        public  static List<SubTree> SimplifyTreeStructure(this List<SubTree> structure)
        {
            structure= FinalizeTreeStructure(structure);
            
            //var leaves =  structure.GetAllLeaves();
            
            
            var parents = structure.Select(subtree => subtree.Parent).ToList();
            var unique = structure.Select(subtree => subtree.Parent).Distinct().ToList();
            if (unique.Count() == parents.Count)
            {
                return structure;
            }
            foreach (var item in unique)
            {
                var list = structure.Where(x => x.Parent == item).ToList();
                var reduced = new SubTree();
                reduced.Parent = item;
                reduced.Children=list[0].Children;
                for (int i = 1; i < list.Count; i++)
                    reduced.Children.AddRange(list[i].Children);
                reduced.Children = reduced.Children.Distinct().ToList(); //Remove Duplicate children
                structure.RemoveAll(x => x.Parent == item);
                structure.Add(reduced);
            }
            
            
            
            
            
            
            return structure;
        }

    }
    
    public class SpellCastingTypeDefiner : SaveableTypeDefiner
    {
        public SpellCastingTypeDefiner() : base(1_456_199) { }
        protected override void DefineEnumTypes()
        {
            AddEnumDefinition(typeof(CareerId), 1);
        }
    }

 
}