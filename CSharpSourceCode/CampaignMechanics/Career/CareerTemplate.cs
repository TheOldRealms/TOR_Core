using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.SaveSystem;
using TOR_Core.AbilitySystem;
using TOR_Core.BattleMechanics.DamageSystem;

namespace TOR_Core.CampaignMechanics.Career
{
    [Serializable]
    public class CareerTemplate //The Serializable Component that stores all passive nodes and key stones, and is load into the game on start up
    {
        [XmlAttribute] 
        public string AbilityTemplateId;
        [XmlArray("CareerAbilityWeaponRequirements")]
        public List<WeaponClass> CareerAbilityWeaponRequirements = new List<WeaponClass>();
        [XmlAttribute] 
        public bool CanBeUsedOnHorse = true;
        [XmlAttribute] public CareerId CareerId = CareerId.None; //might also be just a string ensures, that no typos are made and the right Career template

        [XmlArray("KeyStones")] 
        public List<KeyStoneNode> KeyStoneNodes = new List<KeyStoneNode>();

        [XmlArray("PassiveNodes")] 
        public List<PassiveNode> PassiveNodes = new List<PassiveNode>();

        [XmlArray("Structure")] 
        public List<SubTree> Structure = new List<SubTree>();
        
        
        //Attributes that will be applied upon Changing or acquiring the Career e.g. IsVampire(for adding body), IsKnight, IsWarriorpriest (e.g. for adding a title)

        public CareerTemplate()
        {

        }
        public CareerTemplate(CareerId id)
        {
            CareerId = id;
        }
    }

    public class CareerTreeNode
    {
        [XmlAttribute] public string AttributeCondition = ""; // The Node requires the character to posses a certain attribute, like being a Questing Knight, to unlock Node

        [XmlAttribute] public string DescriptionText = "";

        [XmlAttribute] public string Id = "";

        [XmlAttribute] public List<string> LockNodes = new List<string>(); // On investing into this talent node, the list of  nodes that get blocked

        [XmlAttribute] public List<string> ParentIDs = new List<string>();

        [XmlAttribute] // The node can have 1 or many parents. If parents are empty it's the root node. The parents only account for setting the "unlockable" state if other conditions are met.
        public TreeNodeState State = TreeNodeState.Locked;

        [XmlAttribute] public string UnlockConditionText = "";
    }

    [Serializable]
    public class PassiveNode : CareerTreeNode
    {
        [XmlAttribute] public float Amount;

        [XmlAttribute] public DamageType DamageType = DamageType.Physical;

        [XmlAttribute] public PassiveEffect EffectType = PassiveEffect.None;
    }

    [Serializable]
    public class KeyStoneNode : CareerTreeNode
    {
        [XmlAttribute] public float AttributeValue; // Not mandatory but potential either a scale factor or addition 

        [XmlAttribute] public string CharacterAttribute = ""; // Attributes can serve to add new benefits for campaign via models : Example : grailknight: Requires attribute "QuestingKnight" to recruit Questing Knights 

        [XmlElement] public AbilityTemplateModifier Modifier;

        [XmlElement] public AbilityTemplateOverrides Overrides;
    }

    [Serializable]
    public class AbilityTemplateOverrides
    {
        //Override : Based on Hierachical structure, the former effect, is overriden by the new effect. Game Design needs to consider to lock abilities that could be conflicting.
        //null, '""',"none" or Invalid will be ignored upon application of the Modifier 
        [XmlAttribute] public AbilityTargetType AbilityTargetType = AbilityTargetType.Invalid;

        [XmlElement(IsNullable = true)] public string ScriptNameToTrigger = "none";

        [XmlElement(IsNullable = true)] public string TroopIdToSummon = "none";

        [XmlElement(IsNullable = true)] public string BurstParticleEffectPrefab = "none";

        [XmlAttribute] public CastType CastType = CastType.Invalid;

        [XmlAttribute] public ChargeType ChargeType = ChargeType.Invalid;

        [XmlAttribute] public DamageType DamageType = DamageType.Invalid;

        [XmlElement(IsNullable = true)] public bool? StartsOnCoolDownOverride;

        [XmlElement(IsNullable = true)] public string TriggeredEffectId = "";

        [XmlElement(IsNullable = true)] public bool? HasLight;

        [XmlElement(IsNullable = true)] public string SoundEffectId = "none";

        [XmlElement(IsNullable = true)] public float? SoundEffectLength;

        [XmlElement(IsNullable = true)] public bool? HasShockWave;

        [XmlElement(IsNullable = true)] public string ImbuedStatusEffectId = "none";

        [XmlElement(IsNullable = true)] public float? LightIntensity;

        [XmlElement(IsNullable = true)] public float? LightRadius;

        [XmlElement(IsNullable = true)] public Vec3? LightColorRgb;

        [XmlElement(IsNullable = true)] public float? LightFlickeringMagnitude;

        [XmlElement(IsNullable = true)] public float? LightFlickeringInterval;


        [XmlElement(IsNullable = true)] public string NameOverride = "";

        [XmlElement(IsNullable = true)] public bool? ShadowCastEnabled = true;

        [XmlElement(IsNullable = true)] public string ParticleEffectPrefab = "";


        [XmlElement(IsNullable = true)] public string SoundEffectToPlay = "";

        [XmlElement(IsNullable = true)] public bool? ShouldSoundLoopOverDuration;


        [XmlElement(IsNullable = true)] public string SpriteOverride = "";


        [XmlAttribute] public TargetType TargetType = TargetType.Invalid;

        [XmlAttribute] public string TooltipDescription = "";
    }

    [Serializable]
    public class AbilityTemplateModifier
    {
        [XmlAttribute] public float BaseMisCastChance;

        [XmlAttribute] public float BaseMovementSpeed;

        [XmlAttribute] public float CastTime;

        //modify : More or less, we are going with addition for easier handling.
        [XmlAttribute] public int Charge;

        [XmlAttribute] public int ChargeRequirement;

        [XmlAttribute] public int CoolDown;

        [XmlAttribute] public int Damage;

        [XmlAttribute] public float Duration;

        [XmlAttribute] public float ImbuedStatusEffectDuration;

        [XmlAttribute] public float ImpactRadius;

        [XmlAttribute] public float MaxDistance;

        [XmlAttribute] public float MinDistance;

        [XmlAttribute] public int NumberToSummon;

        [XmlAttribute] public float Offset;

        [XmlAttribute] public float Radius;

        [XmlAttribute] public int Usages;

        [XmlAttribute] public int WindsOfMagicCost;
    }


    [Serializable]
    public class SubTree
    {
        [XmlArray("Children")] public List<string> Children = new List<string>();

        public int Level;

        [XmlAttribute] public string Parent = "";

        public bool Contains(string element)
        {
            if (Parent == element || Children.Contains(element))
                return true;
            return false;
        }
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
        HP, //Health Points
        AP, //Ammunition Points
        WP, //Winds of Magic Points
        MD, //Melee Damage
        RD, //Range Damage
        SD //Spell Damage
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

        public static List<SubTree> GetAllParents(this List<SubTree> TreeStructure, string element)
        {
            return TreeStructure.Where(x => x.Children.Contains(element)).ToList();
        }

        public static SubTree GetSubTreeWithNode(this List<SubTree> TreeStructre, string element)
        {
            if (element == RootNode) return null;

            return TreeStructre.FirstOrDefault(x => x.Parent.Contains(element));
        }

        public static List<string> GetAllChildren(this List<SubTree> treeStructure)
        {
            var elements = new List<string>();
            foreach (var tree in treeStructure) elements.AddRange(tree.Children);

            return elements.Distinct().ToList();
        }

        private static List<SubTree> FinalizeTreeStructure(this List<SubTree> treeStructure)
        {
            var children = treeStructure.GetAllChildren();
            var leaves = children.Where(child => treeStructure.All(x => x.Parent != child)).ToList();

            foreach (var leaf in leaves)
            {
                var newElement = new SubTree();
                newElement.Parent = leaf;
                treeStructure.Add(newElement);
            }

            return treeStructure;
        }

        public static int GetNodeLevel(this List<SubTree> treeStructure, string element)
        {
            var level = 0;
            var node = treeStructure.GetSubTreeWithNode(element);
            return node != null ? node.Level : 0;
        }

        public static List<SubTree> SimplifyTreeStructure(this List<SubTree> structure)
        {
            structure = FinalizeTreeStructure(structure);

            //var leaves =  structure.GetAllLeaves();
            var parents = structure.Select(subtree => subtree.Parent).ToList();
            var unique = structure.Select(subtree => subtree.Parent).Distinct().ToList();
            if (unique.Count() == parents.Count) return structure;
            foreach (var item in unique)
            {
                var list = structure.Where(x => x.Parent == item).ToList();
                var reduced = new SubTree();
                reduced.Parent = item;
                reduced.Children = list[0].Children;
                for (var i = 1; i < list.Count; i++)
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
        public SpellCastingTypeDefiner() : base(1_456_199)
        {
        }

        protected override void DefineEnumTypes()
        {
            AddEnumDefinition(typeof(CareerId), 1);
        }
    }
}