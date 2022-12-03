using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using TaleWorlds.Core;
using TaleWorlds.SaveSystem;
using TOR_Core.BattleMechanics.DamageSystem;

namespace TOR_Core.CampaignMechanics.Career
{
    [Serializable]
    public class CareerTreeNode
    {
        [SaveableField(1)][XmlAttribute] public string Id = "";
        [SaveableField(2)]public NodeType NodeType;
        
        [SaveableField(3)][XmlAttribute]public string AttributeCondition = ""; // The Node requires the character to posses a certain attribute, like being a Questing Knight, to unlock Node

        [SaveableField(4)][XmlAttribute]public string DescriptionText = "";

        
        [SaveableField(5)][XmlAttribute]public TreeNodeState State = TreeNodeState.Locked;
        [SaveableField(6)][XmlAttribute] public List<string> LockNodes = new List<string>(); // On investing into this talent node, the list of  nodes that get blocked
        
        
        // The node can have 1 or many parents. If parents are empty it's the root node. The parents only account for setting the "unlockable" state if other conditions are met.
        

        [XmlAttribute] public string UnlockConditionText = "";
        
        
        [SaveableField(7)] 
        public List<string> ParentIDs = new List<string>(); 
        [SaveableField(8)] 
        public List<string> ChildrenIDs = new List<string>();
        [SaveableField(9)] 
        public int Level;
        

        public bool HasUnlockedNeighbor(List<SubTree> treeStructure, List<CareerTreeNode> allNodes, out List<CareerTreeNode> neighbors)
        {
            neighbors = new List<CareerTreeNode>();
            if (!treeStructure.ContainsNode(this.Id)) return false;
            
            
            var list = treeStructure.Where(x => x.Contains(this.Id)).ToList();      //nodes that have this node as children
            
            if (list.IsEmpty())
                return false;

            foreach (var subtree in list)
            {
                var node =allNodes.FirstOrDefault(x => x.Id == subtree.Parent);
                if (node.State == TreeNodeState.Unlocked)
                {
                    neighbors.Add(node);
                    return true;
                }
            }
            

            foreach (var subtree in list)
            {
                var node =allNodes.FirstOrDefault(x => subtree.Children.Contains(x.Id));
                if (node.State == TreeNodeState.Unlocked)
                {
                    neighbors.Add(node);
                    return true;
                }
            }

            
            return false;
        }
        
        public bool HasSameValues(CareerTreeNode node)
        {
            if (node == null) return false;

            if (NodeType != node.NodeType)
            {
                return false;
            }

            if (Level != node.Level)
                return false;


            if (ChildrenIDs.Where((t, i) => node.ChildrenIDs[i] != t).Any())
            {
                return false;
            }

            return !ParentIDs.Where((t, i) => node.ParentIDs[i] != t).Any();
        }
    }
    
    [Serializable]
    public class RootNode : CareerTreeNode
    {
        public RootNode()
        {
            NodeType = NodeType.RootNode;
            Level = 0;
            ParentIDs = new List<string>() { "-1" };
            ChildrenIDs = new List<string>();
            State = TreeNodeState.Unlocked;
        }
    }


    [Serializable]
    public class PassiveNode : CareerTreeNode
    {
        public PassiveNode()
        {
            NodeType = NodeType.PassiveNode;
        }
        
        [XmlAttribute] public float Amount;

        [XmlAttribute] public DamageType DamageType;

        [XmlAttribute] public PassiveEffect EffectType = PassiveEffect.None;
    }

    [Serializable]
    public class KeyStoneNode : CareerTreeNode
    {
        public KeyStoneNode()
        {
            NodeType = NodeType.KeyStoneNode;
        }
        
        [XmlAttribute] public float AttributeValue; // Not mandatory but potential either a scale factor or addition 

        [XmlAttribute] public string CharacterAttribute = ""; // Attributes can serve to add new benefits for campaign via models : Example : grailknight: Requires attribute "QuestingKnight" to recruit Questing Knights 

        [XmlElement] public AbilityTemplateModifier Modifier;

        [XmlElement] public AbilityTemplateOverrides Overrides;
    }
    
    public static class TreeNodeDataStructureExtension
    {
        
        public static List<CareerTreeNode> GetNeighbors(this List<CareerTreeNode> tree, CareerTreeNode selected)
        {
            var neighbors=new List<CareerTreeNode>();
            foreach (var node in tree)
            {
                if(node.Id!=selected.Id) continue;
                foreach (var id in node.ParentIDs)
                {
                    var parent = tree.GetNode(id);
                    if(parent!=null)
                        neighbors.Add(parent);
                }
                
                
                foreach (var id in node.ChildrenIDs)
                {
                    var child = tree.GetNode(id);
                    if(child!=null)
                        neighbors.Add(child);
                }
            }
            return neighbors;
        }
        
        
        public static void UnlockNode(this List<CareerTreeNode> allNodes, CareerTreeNode selectedNode)
        {
            foreach (var node in allNodes)
            {
                if(selectedNode==node)continue;
                if (!selectedNode.LockNodes.Contains(node.Id)) continue;
                if (node.State == TreeNodeState.Unlocked)
                {
                    return;
                }
            }

            var neighbors = allNodes.GetNeighbors(selectedNode);

            foreach (var neighbor in neighbors)
            {
                if (neighbor.State == TreeNodeState.Locked)
                {
                    neighbor.State = TreeNodeState.Available;
                }
            }
            
            selectedNode.State = TreeNodeState.Unlocked;
            //TODO lock nodes ? 
        }
        
        public static List<KeyStoneNode> GetKeyStoneNodes(this List<CareerTreeNode> tree)
        {
            return tree.Where(element => element.GetType() == typeof(KeyStoneNode)).Cast<KeyStoneNode>().ToList();
        }
        
        public static List<PassiveNode> GetPassiveNodes(this List<CareerTreeNode> tree)
        {
            return tree.Where(element => element.GetType() == typeof(PassiveNode)).Cast<PassiveNode>().ToList();
        }

        public static RootNode GetRootNode(this List<CareerTreeNode> tree)
        {
            var node = tree.FirstOrDefault(element => element.NodeType == NodeType.RootNode);
            return (RootNode) node;
        }

        public static CareerTreeNode GetNode(this List<CareerTreeNode> treeStructure, string id)
        {
            return treeStructure.FirstOrDefault(x => x.Id == id);
        }
        
        
        public static bool IsReplicant(this List<CareerTreeNode> TreeStructure, List<CareerTreeNode> otherTree)
        {
            if (TreeStructure == null) return false;
            if (otherTree == null) return false;
            if (TreeStructure.Count != otherTree.Count) return false;

            return !TreeStructure.Where((t, i) => !t.HasSameValues(otherTree[i])).Any();
        }
        
    }
    
    
    
    public class CareerIdDefiner : SaveableTypeDefiner
    {
        public CareerIdDefiner() : base(1_1773_199) { }
        protected override void DefineEnumTypes()
        {
            AddEnumDefinition(typeof(CareerId), 1);
        }
    }
    
    public class TreeNodeStateDefiner : SaveableTypeDefiner
    {
        public TreeNodeStateDefiner() : base(1_12341_199) { }
        protected override void DefineEnumTypes()
        {
            AddEnumDefinition(typeof(TreeNodeState), 1);
        }
    }

    
    
    public class CareerTreeNodeDefiner: SaveableTypeDefiner
    {
        public CareerTreeNodeDefiner() : base(1_899_199)
        {
        }
        
        protected override void DefineClassTypes()
        {
            
            AddClassDefinition(typeof(CareerTreeNode), 1);
        }
        
        
        protected override void DefineContainerDefinitions()
        {
            ConstructContainerDefinition(typeof(List<CareerTreeNode>));
        }
    }
    
}