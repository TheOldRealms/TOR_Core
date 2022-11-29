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
        private const string RootNodeId = "0";


        private static string _filename = "tor_careertemplates.xml";
        public static IEnumerable<string> ListAvailableCareers()
        {
            return (from object career in Enum.GetValues(typeof(CareerId)) select career.ToString()).ToList();
        }

        public static List<CareerTemplate> GetAllTemplates()
        {
            var list = new List<CareerTemplate>();
            foreach(var template in _templates.Values) list.Add(template);
            return list;
        }

        public static CareerTemplate GetTemplate(CareerId id)
        {
            if (id == CareerId.None) return null;
            return _templates.ContainsKey(id.ToString()) ? _templates[id.ToString()] : null;
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
                    ValidateTreeStructure(career.ToString(),ref treeElements, ref structure);

                    if (structure.Count == 0)  continue;

                    career.Structure = structure;

                    career.KeyStoneNodes = career.KeyStoneNodes.Where(x => structure.ContainsNode(x.Id)).ToList();
                    career.PassiveNodes = career.PassiveNodes.Where(x => structure.ContainsNode(x.Id)).ToList();
                    
                    
                    _templates.Add(career.CareerId.ToString(), career);
                }
                
            }
        }
        
        private  static void ValidateTreeStructure(string CareerId, ref List<CareerTreeNode> nodes, ref List<SubTree> structure)
        {
            var exceptionbase = $"Error: {CareerId} CareerTree structure is invalid.";
            if (nodes.Any(x=>x.Id==RootNodeId))        //The Root node is always the Ability, or empty, but is not part of key stones or Passive nodes
            {
                throw new Exception($"{exceptionbase} Tree contained invalid id ( {RootNodeId} ) ");
            }

            structure= structure.SimplifyTreeStructure();        //Maybe a warning message. This should not happen, but lets just ensure the tree structure is handled probably
            
            foreach (var subtree in structure)
            {
                var exception = $"Career {CareerId} subtree with parent node {subtree.Parent}";
                if (subtree.Children.Contains(subtree.Parent))
                {
                    throw new Exception(exception+ " was part of his own leafs");
                }
            }
            
            //var nodeIds = nodes.Select(x => x.Id).ToList();
            var leaves = structure.GetAllLeaves();

            foreach (var leaf in leaves)
            {
                var path = new List<string>();
                var parent = leaf;
                path.Add(leaf);
                while (parent != null && parent != RootNodeId)
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
                            structure.RemoveAll(x => x.Parent == item);
                        }
                        break;
                    }
                }

                var treeElement = structure.FirstOrDefault(x => x.Children.Contains(leaf));
                if(treeElement!=null)
                    treeElement.Level=path.Count-1;//we dont count the first
            }
        }
        
        





        




    }
}