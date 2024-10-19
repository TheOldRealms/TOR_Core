using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TOR_Core.Models
{
    internal class TORSettlementDistanceModel : MapDistanceModel
    {
        private readonly Dictionary<(Settlement, Settlement), float> _settlementDistanceCache = [];
        private readonly Dictionary<int, Settlement> _navigationMeshClosestSettlementCache = [];
        private readonly List<Settlement> _settlementsToConsider = [];
        public override float MaximumDistanceBetweenTwoSettlements { get; set; }

        public void TORLoadCacheFromFile(System.IO.BinaryReader reader)
        {
            _settlementDistanceCache.Clear();
            int count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                Settlement fromSettlement = Settlement.Find(reader.ReadString());
                _settlementsToConsider.Add(fromSettlement);
                for (int j = i + 1; j < count; j++)
                {
                    Settlement toSettlement = Settlement.Find(reader.ReadString());
                    float distance = reader.ReadSingle();
                    if (fromSettlement.Id.InternalValue <= toSettlement.Id.InternalValue)
                    {
                        AddNewPairToDistanceCache((fromSettlement, toSettlement), distance);
                    }
                    else
                    {
                        AddNewPairToDistanceCache((toSettlement, fromSettlement), distance);
                    }
                }
            }
            for (int navfaceId = reader.ReadInt32(); navfaceId >= 0; navfaceId = reader.ReadInt32())
            {
                Settlement settlement = Settlement.Find(reader.ReadString());
                _navigationMeshClosestSettlementCache[navfaceId] = settlement;
            }
        }

        public override float GetDistance(Settlement fromSettlement, Settlement toSettlement)
        {
            float result;
            if (fromSettlement == toSettlement) result = 0f;
            else if (fromSettlement.Id.InternalValue <= toSettlement.Id.InternalValue)
            {
                (Settlement, Settlement) tuple = (fromSettlement, toSettlement);
                if (!_settlementDistanceCache.TryGetValue(tuple, out result))
                {
                    result = GetDistanceNonCached(fromSettlement.GatePosition, toSettlement.GatePosition, fromSettlement.CurrentNavigationFace, toSettlement.CurrentNavigationFace);
                    AddNewPairToDistanceCache(tuple, result);
                }
            }
            else
            {
                (Settlement, Settlement) tuple2 = (toSettlement, fromSettlement);
                if (!_settlementDistanceCache.TryGetValue(tuple2, out result))
                {
                    result = GetDistanceNonCached(toSettlement.GatePosition, fromSettlement.GatePosition, toSettlement.CurrentNavigationFace, fromSettlement.CurrentNavigationFace);
                    AddNewPairToDistanceCache(tuple2, result);
                }
            }
            return result;
        }

        public override float GetDistance(MobileParty fromParty, Settlement toSettlement)
        {
            if (fromParty.CurrentSettlement != null)
            {
                return GetDistance(fromParty.CurrentSettlement, toSettlement);
            }
            if (fromParty.CurrentNavigationFace.FaceIndex == toSettlement.CurrentNavigationFace.FaceIndex)
            {
                return fromParty.Position2D.Distance(toSettlement.GatePosition);
            }
            Settlement closestSettlementForNavigationMesh = GetClosestSettlementForNavigationMesh(fromParty.CurrentNavigationFace);
            return fromParty.Position2D.Distance(toSettlement.GatePosition) - closestSettlementForNavigationMesh.GatePosition.Distance(toSettlement.GatePosition) + GetDistance(closestSettlementForNavigationMesh, toSettlement);
        }

        public override float GetDistance(MobileParty fromParty, MobileParty toParty)
        {
            if (fromParty.CurrentNavigationFace.FaceIndex == toParty.CurrentNavigationFace.FaceIndex)
            {
                return fromParty.Position2D.Distance(toParty.Position2D);
            }
            Settlement settlement = fromParty.CurrentSettlement ?? GetClosestSettlementForNavigationMesh(fromParty.CurrentNavigationFace);
            Settlement settlement2 = toParty.CurrentSettlement ?? GetClosestSettlementForNavigationMesh(toParty.CurrentNavigationFace);
            return fromParty.Position2D.Distance(toParty.Position2D) - settlement.GatePosition.Distance(settlement2.GatePosition) + GetDistance(settlement, settlement2);
        }

        public override bool GetDistance(Settlement fromSettlement, Settlement toSettlement, float maximumDistance, out float distance)
        {
            bool result;
            if (fromSettlement == toSettlement)
            {
                distance = 0f;
                result = true;
            }
            else if (fromSettlement.CurrentNavigationFace.FaceIndex == toSettlement.CurrentNavigationFace.FaceIndex)
            {
                distance = fromSettlement.GatePosition.Distance(toSettlement.GatePosition);
                result = distance <= maximumDistance;
            }
            else if (fromSettlement.Id.InternalValue <= toSettlement.Id.InternalValue)
            {
                (Settlement, Settlement) tuple = (fromSettlement, toSettlement);
                if (_settlementDistanceCache.TryGetValue(tuple, out distance))
                {
                    result = distance <= maximumDistance;
                }
                else
                {
                    result = GetDistanceWithDistanceLimitNonCached(fromSettlement.GatePosition, toSettlement.GatePosition, Campaign.Current.MapSceneWrapper.GetFaceIndex(fromSettlement.GatePosition), Campaign.Current.MapSceneWrapper.GetFaceIndex(toSettlement.GatePosition), maximumDistance, out distance);
                    if (result)
                    {
                        AddNewPairToDistanceCache(tuple, distance);
                    }
                }
            }
            else
            {
                (Settlement, Settlement) tuple = (toSettlement, fromSettlement);
                if (_settlementDistanceCache.TryGetValue(tuple, out distance))
                {
                    result = distance <= maximumDistance;
                }
                else
                {
                    result = GetDistanceWithDistanceLimitNonCached(toSettlement.GatePosition, fromSettlement.GatePosition, Campaign.Current.MapSceneWrapper.GetFaceIndex(toSettlement.GatePosition), Campaign.Current.MapSceneWrapper.GetFaceIndex(fromSettlement.GatePosition), maximumDistance, out distance);
                    if (result)
                    {
                        AddNewPairToDistanceCache(tuple, distance);
                    }
                }
            }
            return result;
        }

        public override bool GetDistance(MobileParty fromParty, Settlement toSettlement, float maximumDistance, out float distance)
        {
            bool result = false;
            if (fromParty.CurrentSettlement != null)
            {
                result = GetDistance(fromParty.CurrentSettlement, toSettlement, maximumDistance, out distance);
            }
            else if (fromParty.CurrentNavigationFace.FaceIndex == toSettlement.CurrentNavigationFace.FaceIndex)
            {
                distance = fromParty.Position2D.Distance(toSettlement.GatePosition);
                result = distance <= maximumDistance;
            }
            else
            {
                Settlement closestSettlementForNavigationMesh = GetClosestSettlementForNavigationMesh(fromParty.CurrentNavigationFace);
                if (GetDistance(closestSettlementForNavigationMesh, toSettlement, maximumDistance, out distance))
                {
                    distance += fromParty.Position2D.Distance(toSettlement.GatePosition) - closestSettlementForNavigationMesh.GatePosition.Distance(toSettlement.GatePosition);
                    result = distance <= maximumDistance;
                }
            }
            return result;
        }

        public override bool GetDistance(IMapPoint fromMapPoint, MobileParty toParty, float maximumDistance, out float distance)
        {
            bool result = false;
            if (fromMapPoint.CurrentNavigationFace.FaceIndex == toParty.CurrentNavigationFace.FaceIndex)
            {
                distance = fromMapPoint.Position2D.Distance(toParty.Position2D);
                result = distance <= maximumDistance;
            }
            else
            {
                Settlement closestSettlementForNavigationMesh = GetClosestSettlementForNavigationMesh(fromMapPoint.CurrentNavigationFace);
                Settlement settlement = toParty.CurrentSettlement ?? GetClosestSettlementForNavigationMesh(toParty.CurrentNavigationFace);
                if (GetDistance(closestSettlementForNavigationMesh, settlement, maximumDistance, out distance))
                {
                    distance += fromMapPoint.Position2D.Distance(toParty.Position2D) - closestSettlementForNavigationMesh.GatePosition.Distance(settlement.GatePosition);
                    result = distance <= maximumDistance;
                }
            }
            return result;
        }

        public override bool GetDistance(IMapPoint fromMapPoint, Settlement toSettlement, float maximumDistance, out float distance)
        {
            bool result = false;
            if (fromMapPoint.CurrentNavigationFace.FaceIndex == toSettlement.CurrentNavigationFace.FaceIndex)
            {
                distance = fromMapPoint.Position2D.Distance(toSettlement.GatePosition);
                result = distance <= maximumDistance;
            }
            else
            {
                distance = 100f;
                Settlement closestSettlementForNavigationMesh = GetClosestSettlementForNavigationMesh(fromMapPoint.CurrentNavigationFace);
                if (GetDistance(closestSettlementForNavigationMesh, toSettlement, maximumDistance, out distance))
                {
                    distance += fromMapPoint.Position2D.Distance(toSettlement.GatePosition) - closestSettlementForNavigationMesh.GatePosition.Distance(toSettlement.GatePosition);
                    result = distance <= maximumDistance;
                }
            }
            return result;
        }

        public override bool GetDistance(IMapPoint fromMapPoint, in Vec2 toPoint, float maximumDistance, out float distance)
        {
            bool result = false;
            PathFaceRecord faceIndex = Campaign.Current.MapSceneWrapper.GetFaceIndex(toPoint);
            if (fromMapPoint.CurrentNavigationFace.FaceIndex == faceIndex.FaceIndex)
            {
                distance = fromMapPoint.Position2D.Distance(toPoint);
                result = distance <= maximumDistance;
            }
            else
            {
                Settlement closestSettlementForNavigationMesh = GetClosestSettlementForNavigationMesh(fromMapPoint.CurrentNavigationFace);
                Settlement closestSettlementForNavigationMesh2 = GetClosestSettlementForNavigationMesh(faceIndex);
                if (GetDistance(closestSettlementForNavigationMesh, closestSettlementForNavigationMesh2, maximumDistance, out distance))
                {
                    distance += fromMapPoint.Position2D.Distance(toPoint) - closestSettlementForNavigationMesh.GatePosition.Distance(closestSettlementForNavigationMesh2.GatePosition);
                    result = distance <= maximumDistance;
                }
            }
            return result;
        }

        private float GetDistanceNonCached(Vec2 pos1, Vec2 pos2, PathFaceRecord faceIndex1, PathFaceRecord faceIndex2)
        {
            Campaign.Current.MapSceneWrapper.GetPathDistanceBetweenAIFaces(faceIndex1, faceIndex2, pos1, pos2, 0.1f, float.MaxValue, out var distance);
            return distance;
        }

        private bool GetDistanceWithDistanceLimitNonCached(Vec2 pos1, Vec2 pos2, PathFaceRecord faceIndex1, PathFaceRecord faceIndex2, float distanceLimit, out float distance)
        {
            if (pos1.DistanceSquared(pos2) > distanceLimit * distanceLimit)
            {
                distance = float.MaxValue;
                return false;
            }
            return Campaign.Current.MapSceneWrapper.GetPathDistanceBetweenAIFaces(faceIndex1, faceIndex2, pos1, pos2, 0.1f, distanceLimit, out distance);
        }

        public override Settlement GetClosestSettlementForNavigationMesh(PathFaceRecord face)
        {
            if (!_navigationMeshClosestSettlementCache.TryGetValue(face.FaceIndex, out var value))
            {
                if (!face.IsValid())
                {
                    return Settlement.GetFirst;
                }

                Vec2 navigationMeshCenterPosition = Campaign.Current.MapSceneWrapper.GetNavigationMeshCenterPosition(face);
                float num = float.MaxValue;
                foreach (Settlement item in _settlementsToConsider)
                {
                    float num2 = item.GatePosition.DistanceSquared(navigationMeshCenterPosition);
                    if (num > num2)
                    {
                        num = num2;
                        value = item;
                    }
                }
                _navigationMeshClosestSettlementCache[face.FaceIndex] = value;
            }
            return value;
        }

        private void AddNewPairToDistanceCache((Settlement, Settlement) pair, float distance)
        {
            try
            {
                _settlementDistanceCache.Add(pair, distance);
                if (distance > MaximumDistanceBetweenTwoSettlements)
                {
                    MaximumDistanceBetweenTwoSettlements = distance;
                    Campaign.Current.UpdateMaximumDistanceBetweenTwoSettlements();
                }
            }
            catch (ArgumentException ex)
            {
                Debug.ShowError($"Duplicate settlement IDs detected. Settlement: {pair.Item2.StringId}. Exception: {ex.Message}.");
            }
        }
    }
}
