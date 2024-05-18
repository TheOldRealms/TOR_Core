using SandBox;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TOR_Core.Models;
using BinaryReader = System.IO.BinaryReader;
using BinaryWriter = System.IO.BinaryWriter;

namespace TOR_Core.Utilities
{
    public class TORWorldMapScript : ScriptComponentBehavior
	{
		private static string SettlementsXmlPath => TORPaths.TORCoreModuleDataPath + "tor_settlements.xml";
		private static string SettlementsDistanceCacheFilePath => TORPaths.TORCoreModuleDataPath + "settlements_distance_cache.bin";

        public SimpleButton CheckPositions;
        public SimpleButton SavePositions;
        public SimpleButton ComputeAndSaveSettlementDistanceCache;

        protected override bool IsOnlyVisual() => true;

        protected override void OnEditorVariableChanged(string variableName)
		{
			base.OnEditorVariableChanged(variableName);
			if (variableName == "SavePositions")
			{
                SaveSettlementPositions();
			}
			if (variableName == "ComputeAndSaveSettlementDistanceCache")
			{
                SaveSettlementDistanceCache();
			}
			if (variableName == "CheckPositions")
			{
                CheckSettlementPositions();
			}
		}

		protected override void OnSceneSave(string saveFolder)
		{
            base.OnSceneSave(saveFolder);
            SaveSettlementPositions();
            SaveSettlementDistanceCache();
		}

		private void CheckSettlementPositions()
		{
            XmlDocument xmlDocument = LoadXmlFile(SettlementsXmlPath);
            GameEntity.RemoveAllChildren();
            List<string> settlementIds = [];
            bool problemsDetected = false;
            foreach (XmlNode node in xmlDocument.DocumentElement.SelectNodes("Settlement"))
            {
                var settlementId = node.Attributes["id"].Value;
                var campaignEntityWithId = Scene.GetCampaignEntityWithName(settlementId);

                if(campaignEntityWithId == null)
                {
                    TORCommon.Say($"Settlement: {settlementId} has no game entity with a corresponding name! Aborting further checks.");
                    return;
                }
                if (settlementIds.Contains(settlementId))
                {
                    TORCommon.Say($"Duplicate settlement Ids detected! {settlementId} has 2 or more entries in the settlements.xml. Aborting further checks.");
                    return;
                }
                else settlementIds.Add(settlementId);

                var settlementPosition = campaignEntityWithId.GetGlobalFrame().origin;
                
                List<GameEntity> list = [];
                campaignEntityWithId.GetChildrenRecursive(ref list);
                
                foreach (var child in list)
                {
                    if (child.HasTag("main_map_city_gate"))
                    {
                        settlementPosition = child.GetGlobalFrame().origin;
                        break;
                    }
                }

                PathFaceRecord pathFaceRecord = new(-1, -1, -1);
                GameEntity.Scene.GetNavMeshFaceIndex(ref pathFaceRecord, settlementPosition.AsVec2, true, false);

                int num = -1;
                if (pathFaceRecord.IsValid())
                {
                    num = pathFaceRecord.FaceGroupIndex;
                }
                if(num < 0)
                {
                    TORCommon.Say($"Settlement: {settlementId} is on an invalid navmeshface!");
                    problemsDetected = true;
                }
                if ( num <= 0 || num == 7 || num == 8 || num == 10 || num == 11 || num == 13 || num == 14)
                {
                    TORCommon.Say($"Settlement: {settlementId} is on an non-navigable navmeshface!");
                    problemsDetected= true;
                    //MBEditor.ZoomToPosition(settlementPosition);
                    //break;
                }
            }
            if(!problemsDetected)
            {
                TORCommon.Say("All good.");
            }
        }

		protected override void OnInit()
		{
            try
            {
                Debug.Print("SettlementsDistanceCacheFilePath: " + SettlementsDistanceCacheFilePath, 0, Debug.DebugColor.White, 17592186044416UL);
                using BinaryReader binaryReader = new(File.Open(SettlementsDistanceCacheFilePath, FileMode.Open, FileAccess.Read));
                if (Campaign.Current.Models.MapDistanceModel is TORSettlementDistanceModel model)
                {
                    model.TORLoadCacheFromFile(binaryReader);
                }
            }
            catch
            {
                Debug.DisplayDebugMessage("SettlementsDistanceCacheFilePath could not be read!. Cache will be created right now, This may take few minutes!");
                Debug.FailedAssert("SettlementsDistanceCacheFilePath could not be read!. Cache will be created right now, This may take few minutes!");
                Debug.Print("SettlementsDistanceCacheFilePath could not be read!. Cache will be created right now, This may take few minutes!", 0, Debug.DebugColor.White, 17592186044416UL);
                SaveSettlementDistanceCache();
            }
        }

		private List<ToRSettlementRecord> LoadSettlementData(XmlDocument settlementDocument)
		{
            List<ToRSettlementRecord> list = [];
            GameEntity.RemoveAllChildren();

			foreach (XmlNode node in settlementDocument.DocumentElement.SelectNodes("Settlement"))
			{
				var name = node.Attributes["name"].Value;
				var id = node.Attributes["id"].Value;
                var campaignEntityWithId = Scene.GetCampaignEntityWithName(id);
				
				var position = campaignEntityWithId.GetGlobalFrame().origin.AsVec2;
				Vec2 gatePosition = default;

				List<GameEntity> entityList = [];
				campaignEntityWithId.GetChildrenRecursive(ref entityList);

				bool hasGate = false;
				foreach (GameEntity gameEntity in entityList)
				{
					if (gameEntity.HasTag("main_map_city_gate"))
					{
                        gatePosition = gameEntity.GetGlobalFrame().origin.AsVec2;
						hasGate = true;
					}
				}
                list.Add(new ToRSettlementRecord(name, id, position, hasGate ? gatePosition : position, node, hasGate));
			}
			return list;
		}

		private void SaveSettlementPositions()
		{
			XmlDocument xmlDocument = LoadXmlFile(SettlementsXmlPath);
			foreach (ToRSettlementRecord record in LoadSettlementData(xmlDocument))
			{
				if (record.Node.Attributes["posX"] == null)
				{
					XmlAttribute xPos = xmlDocument.CreateAttribute("posX");
					record.Node.Attributes.Append(xPos);
				}
				record.Node.Attributes["posX"].Value = record.Position.X.ToString();
				if (record.Node.Attributes["posY"] == null)
				{
					XmlAttribute yPos = xmlDocument.CreateAttribute("posY");
					record.Node.Attributes.Append(yPos);
				}
				record.Node.Attributes["posY"].Value = record.Position.Y.ToString();
				if (record.HasGate)
				{
					if (record.Node.Attributes["gate_posX"] == null)
					{
						XmlAttribute xGate = xmlDocument.CreateAttribute("gate_posX");
						record.Node.Attributes.Append(xGate);
					}
					record.Node.Attributes["gate_posX"].Value = record.GatePosition.X.ToString();
					if (record.Node.Attributes["gate_posY"] == null)
					{
						XmlAttribute yGate = xmlDocument.CreateAttribute("gate_posY");
						record.Node.Attributes.Append(yGate);
					}
					record.Node.Attributes["gate_posY"].Value = record.GatePosition.Y.ToString();
				}
			}
			xmlDocument.Save(SettlementsXmlPath);
		}

		private void SaveSettlementDistanceCache()
		{
            BinaryWriter binaryWriter = null;
            try
            {
                XmlDocument settlementDocument = LoadXmlFile(SettlementsXmlPath);
                List<ToRSettlementRecord> settlementRecords = LoadSettlementData(settlementDocument);
                
                int navigationMeshIndexOfTerrainType1 = MapScene.GetNavigationMeshIndexOfTerrainType(TerrainType.Mountain);
                int navigationMeshIndexOfTerrainType2 = MapScene.GetNavigationMeshIndexOfTerrainType(TerrainType.Lake);
                int navigationMeshIndexOfTerrainType3 = MapScene.GetNavigationMeshIndexOfTerrainType(TerrainType.Water);
                int navigationMeshIndexOfTerrainType4 = MapScene.GetNavigationMeshIndexOfTerrainType(TerrainType.River);
                int navigationMeshIndexOfTerrainType5 = MapScene.GetNavigationMeshIndexOfTerrainType(TerrainType.Canyon);
                int navigationMeshIndexOfTerrainType6 = MapScene.GetNavigationMeshIndexOfTerrainType(TerrainType.RuralArea);
                Scene.SetAbilityOfFacesWithId(navigationMeshIndexOfTerrainType1, false);
                Scene.SetAbilityOfFacesWithId(navigationMeshIndexOfTerrainType2, false);
                Scene.SetAbilityOfFacesWithId(navigationMeshIndexOfTerrainType3, false);
                Scene.SetAbilityOfFacesWithId(navigationMeshIndexOfTerrainType4, false);
                Scene.SetAbilityOfFacesWithId(navigationMeshIndexOfTerrainType5, false);
                Scene.SetAbilityOfFacesWithId(navigationMeshIndexOfTerrainType6, false);
                
                binaryWriter = new BinaryWriter(File.Open(SettlementsDistanceCacheFilePath, FileMode.Create));
                binaryWriter.Write(settlementRecords.Count);

                for (int i = 0; i < settlementRecords.Count; i++)
                {
                    binaryWriter.Write(settlementRecords[i].SettlementId);
                    
                    Vec2 gatePosition = settlementRecords[i].GatePosition;
                    PathFaceRecord pathFaceRecord = new(-1, -1, -1);
                    Scene.GetNavMeshFaceIndex(ref pathFaceRecord, gatePosition, false, false);

                    for (int j = i + 1; j < settlementRecords.Count; j++)
                    {
                        binaryWriter.Write(settlementRecords[j].SettlementId);

                        Vec2 gatePosition2 = settlementRecords[j].GatePosition;
                        PathFaceRecord pathFaceRecord2 = new(-1, -1, -1);

                        Scene.GetNavMeshFaceIndex(ref pathFaceRecord2, gatePosition2, false, false);
                        if(!pathFaceRecord.IsValid() || !pathFaceRecord2.IsValid())
                        {
                            throw new Exception($"Error when writing settlement distance cache. Pathface: {pathFaceRecord} or {pathFaceRecord2} is invalid.");
                        }
                        Scene.GetPathDistanceBetweenAIFaces(pathFaceRecord.FaceIndex, pathFaceRecord2.FaceIndex, gatePosition, gatePosition2, 0.1f, float.MaxValue, out float value);
                        if(value < 0 || value == float.NaN)
                        {
                            throw new Exception($"Error when writing settlement distance cache. Distance between {settlementRecords[i].SettlementId} and {settlementRecords[j].SettlementId} is {value} and thus, invalid.");
                        }

                        binaryWriter.Write(value);
                    }
                }

                int navMeshFaceCount = Scene.GetNavMeshFaceCount();
                for (int i = 0; i < navMeshFaceCount; i++)
                {
                    int idOfNavMeshFace = Scene.GetIdOfNavMeshFace(i);
                    if (idOfNavMeshFace != navigationMeshIndexOfTerrainType1 && 
                        idOfNavMeshFace != navigationMeshIndexOfTerrainType2 && 
                        idOfNavMeshFace != navigationMeshIndexOfTerrainType3 && 
                        idOfNavMeshFace != navigationMeshIndexOfTerrainType4 && 
                        idOfNavMeshFace != navigationMeshIndexOfTerrainType5 && 
                        idOfNavMeshFace != navigationMeshIndexOfTerrainType6)
                    {
                        Vec3 centerOfNavMeshFaceVec3 = Vec3.Zero;
                        Scene.GetNavMeshCenterPosition(i, ref centerOfNavMeshFaceVec3);
                        Vec2 centerOfNavMeshFace = centerOfNavMeshFaceVec3.AsVec2;

                        float distance = float.MaxValue;
                        string settlementId = string.Empty;

                        for (int j = 0; j < settlementRecords.Count; j++)
                        {
                            var gatePosition = settlementRecords[j].GatePosition;
                            PathFaceRecord pathFaceRecord = new(-1, -1, -1);
                            Scene.GetNavMeshFaceIndex(ref pathFaceRecord, gatePosition, false, false);

                            if ((distance.ApproximatelyEqualsTo(float.MaxValue) || 
                                centerOfNavMeshFace.DistanceSquared(gatePosition) < Math.Pow(distance, 2)) && 
                                Scene.GetPathDistanceBetweenAIFaces(i, pathFaceRecord.FaceIndex, centerOfNavMeshFace, gatePosition, 0.1f, distance, out float newDistance) && 
                                newDistance < distance)
                            {
                                distance = newDistance;
                                settlementId = settlementRecords[j].SettlementId;
                            }
                        }
                        if (!string.IsNullOrWhiteSpace(settlementId))
                        {
                            binaryWriter.Write(i);
                            binaryWriter.Write(settlementId);
                        }
                    }
                }
                binaryWriter.Write(-1);
                Debug.Print("Settlement distance cache successfully saved.");
            }
            catch
            {
                Debug.ShowError("Settlement Distance Cache creation failed.");
            }
            finally
            {
                Scene.SetAbilityOfFacesWithId(MapScene.GetNavigationMeshIndexOfTerrainType(TerrainType.Mountain), true);
                Scene.SetAbilityOfFacesWithId(MapScene.GetNavigationMeshIndexOfTerrainType(TerrainType.Lake), true);
                Scene.SetAbilityOfFacesWithId(MapScene.GetNavigationMeshIndexOfTerrainType(TerrainType.Water), true);
                Scene.SetAbilityOfFacesWithId(MapScene.GetNavigationMeshIndexOfTerrainType(TerrainType.River), true);
                Scene.SetAbilityOfFacesWithId(MapScene.GetNavigationMeshIndexOfTerrainType(TerrainType.Canyon), true);
                Scene.SetAbilityOfFacesWithId(MapScene.GetNavigationMeshIndexOfTerrainType(TerrainType.RuralArea), true);
                binaryWriter?.Close();
            }
        }

		private XmlDocument LoadXmlFile(string path)
		{
			Debug.Print("opening " + path, 0, Debug.DebugColor.White, 17592186044416UL);
            XmlDocument xmlDocument = new();
			xmlDocument.Load(path);
			return xmlDocument;
		}

		private readonly struct ToRSettlementRecord(string settlementName, string settlementId, Vec2 position, Vec2 gatePosition, XmlNode node, bool hasGate)
        {
            public readonly string SettlementName = settlementName;
            public readonly string SettlementId = settlementId;
            public readonly XmlNode Node = node;
            public readonly Vec2 Position = position;
            public readonly Vec2 GatePosition = gatePosition;
            public readonly bool HasGate = hasGate;
        }
    }
}
