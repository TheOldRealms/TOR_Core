using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using TaleWorlds.Core;
using TOR_Core.AbilitySystem;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignMechanics.Career
{
    public class CareerFactory
    {
        private static readonly Dictionary<CareerId, CareerBody> _careerBodies = new Dictionary<CareerId, CareerBody>();
        
        private static readonly Dictionary<string, CareerTemplate> _templates = new Dictionary<string, CareerTemplate>();


        private static readonly string _filename = "tor_careertemplates.xml";

        public static IEnumerable<string> ListAvailableCareers()
        {
            return (from object career in Enum.GetValues(typeof(CareerId)) select career.ToString()).ToList();
        }

        public static List<CareerTemplate> GetAllTemplates()
        {
            var list = new List<CareerTemplate>();
            foreach (var template in _templates.Values) list.Add(template);
            return list;
        }

        public static CareerBody GetCareerBody(CareerId id)
        {
            if (id == CareerId.None) return null;
            else
            {
                return _careerBodies.ContainsKey(id) ? _careerBodies[id] : null;
            }
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
            if (!File.Exists(path)) return;
            
            var careerList = ser.Deserialize(File.OpenRead(path)) as List<CareerTemplate>;
            foreach (var career in careerList)
            {

                if (career.AbilityTemplateId == null||career.AbilityTemplateId == "none")
                {
                    career.AbilityTemplateId = "";
                }
                //validate career node entries
                var treeElements = career.PassiveNodes.Cast<CareerTreeNode>().ToList();
                treeElements.AddRange(career.KeyStoneNodes);
                var structure = career.Structure.ToList();
                ValidateTreeStructure(career.ToString(), ref treeElements, ref structure);
                if (structure.Count == 0) continue;

                career.Structure = structure;
                career.KeyStoneNodes = career.KeyStoneNodes.Where(x => structure.ContainsNode(x.Id)).ToList();
                career.PassiveNodes = career.PassiveNodes.Where(x => structure.ContainsNode(x.Id)).ToList();

                var rootNode = new RootNode();

                var t = career.Structure.FirstOrDefault(x => x.Parent =="0");
                rootNode.ChildrenIDs.AddRange(t.Children);

                career.CareerTree.Add(rootNode);
                career.CareerTree.AddRange(career.KeyStoneNodes);
                career.CareerTree.AddRange(career.PassiveNodes);

                foreach (var elem in career.CareerTree)
                {
                    var treeStructureElements= career.Structure.Where(x => x.Children.Contains(elem.Id));

                    var level = 0;
                    foreach (var subTree in treeStructureElements)
                    {
                        if (level <= subTree.Level)
                            level = subTree.Level;
                        elem.ParentIDs.Add(subTree.Parent);
                    }

                    elem.Level = level;
                    
                    var parent = career.Structure.FirstOrDefault(x => x.Parent == elem.Id);
                    if(parent!=null)
                        elem.ChildrenIDs.AddRange(parent.Children);
                }

                career.PassiveNodes = career.CareerTree.GetPassiveNodes();
                career.KeyStoneNodes = career.CareerTree.GetKeyStoneNodes();
                
                _templates.Add(career.CareerId.ToString(), career);


                CareerBody careerBody = new CareerBody();

                careerBody.Id = career.CareerId;
                careerBody.CareerTree = career.CareerTree;
                careerBody.AbilityTemplateId = career.AbilityTemplateId;
                careerBody.CanBeUsedOnHorse = career.CanBeUsedOnHorse;
                careerBody.CareerAbilityWeaponRequirements = career.CareerAbilityWeaponRequirements;
                
                _careerBodies.Add(careerBody.Id,careerBody);


            }
        }

        private static void ValidateTreeStructure(string CareerId, ref List<CareerTreeNode> nodes, ref List<SubTree> structure)
        {
            var root = structure.GetRootNodeId();
            var exceptionbase = $"Error: {CareerId} CareerTree structure is invalid.";
            if (nodes.Any(x => x.Id == root)) //The Root node is always the Ability, or empty, but is not part of key stones or Passive nodes
                throw new Exception($"{exceptionbase} Tree contained invalid id ( {root} ) ");
            
            structure = structure.SimplifyTreeStructure(); //Maybe a warning message. This should not happen, but lets just ensure the tree structure is handled probably

            foreach (var subtree in structure)
            {
                var exception = $"Career {CareerId} subtree with parent node {subtree.Parent}";
                if (subtree.Children.Contains(subtree.Parent)) 
                    throw new Exception(exception + " was part of his own leafs");
            }

            var children = structure.GetAllChildren();
            var parentsList = structure.Select(tree => tree.Parent).ToList();
            parentsList.Select(x => children.Contains(x));
            
            foreach (var leaf in parentsList)
            {
                var path = new List<string>();
                var parent = leaf;
                path.Add(leaf);

                while (parent != null && parent != root)
                {
                    var level = structure.FirstOrDefault(x => x.Children.Contains(parent));
                    if (level != null)
                    {
                        structure.Find(elem => elem.Parent == leaf).Level=path.Count;
                        path.Add(level.Parent);
                        parent = level.Parent;
                    }
                    else
                    {
                        foreach (var item in path)
                        {
                            var parents = structure.GetAllParents(item);
                            structure.RemoveAll(x => x.Parent == item);
                            if (parents.Count > 1) parents.FirstOrDefault().Children.Add(item);
                        }

                        break;
                    }
                }

                if (leaf == root) structure.FirstOrDefault(x => leaf.Contains(x.Parent)).Level = -1;
            }
        }
    }
}