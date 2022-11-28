using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TOR_Core.AbilitySystem;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignMechanics.Career
{
    public class CareerFactory
    {
        private static Dictionary<string, CareerTemplate> _templates = new Dictionary<string, CareerTemplate>();
        private static string rootNodeID="0";
        
        
        private static string _filename = "tor_careertemplates.xml";
        public static IEnumerable<string> ListAvailableCareers()
        {
            return (from object career in Enum.GetValues(typeof(CareerId)) select career.ToString()).ToList();
        }
        
        public static void LoadTemplates()
        {
            var ser = new XmlSerializer(typeof(List<CareerTemplate>));
            var path = TORPaths.TORCoreModuleExtendedDataPath + _filename;
            if (File.Exists(path))
            {
                var careerList = ser.Deserialize(File.OpenRead(path)) as List<CareerTemplate>;
                foreach (var career in careerList)
                {
                    
                    //validate career node entries
                    var treeElements = career.PassiveNodes.Cast<CareerTreeNode>().ToList();
                    treeElements.AddRange(career.KeyStoneNodes.Cast<CareerTreeNode>());
                    var structure = career.Structure.ToList();
                    
                    

                    IsValidTreeStructure(career.ToString(),treeElements, ref structure);
                    

                    
                    
                    

                    _templates.Add(career.CareerId.ToString(), career);
                }

              
                foreach (var template in _templates)
                {
                    
                }

                TORCommon.Say(_templates.Count.ToString());
            }
        }



        private  static bool IsValidTreeStructure(string CareerId, List<CareerTreeNode> nodes, ref List<SubTree> structure)
        {
            var exceptionbase = $"Error: {CareerId} CareerTree structure is invalid.";
            if (nodes.Any(x=>x.Id==rootNodeID))        //The Root node is always the Ability, or empty, but is not part of key stones or Passive nodes
            {
                throw new Exception($"{exceptionbase} Tree contained invalid id ( {rootNodeID} ) ");
            }

            structure = SimplfyTreeStructure(structure);        //Maybe a warning message. This should not happen, but lets just ensure the tree structure is handled probably
            
            foreach (var subtree in structure)
            {
                var exception = $"Career {CareerId} subtree with parent node {subtree.Parent}";
                if (subtree.Children.Contains(subtree.Parent))
                {
                    throw new Exception(exception+ " was part of his own leafs");
                }
            }
            
            var nodeIds = nodes.Select(x => x.Id).ToList();
            var leafs = new List<string>();
            foreach (var nodeId in nodeIds)
            {
                if (structure.All(x => x.Parent != nodeId))
                {
                    leafs.Add(nodeId);
                }
            }
            
            foreach (var leaf in leafs)
            {
                var path = new List<string>();
                var parent = leaf;
                path.Add(leaf);
                while (parent != null && parent != rootNodeID)
                {
                    var level= structure.FirstOrDefault(x => x.Children.Contains(parent));
                    if (level != null)
                    {
                        path.Add(level.Parent);
                        parent = level.Parent;
                    }
                    else
                    {
                        foreach (var item in path)
                        {
                            nodes.RemoveAll(x => x.Id == item);
                            structure.RemoveAll(x => x.Parent == item);
                        }
                        break;
                    }
                }

                var treeElement = structure.FirstOrDefault(x => x.Children.Contains(leaf));
                if(treeElement!=null)
                    treeElement.level=path.Count-1;//we dont count the first
            }
            

           


            return false;

        }
        
        





        private static List<SubTree> SimplfyTreeStructure(List<SubTree> structure)
        {
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
}