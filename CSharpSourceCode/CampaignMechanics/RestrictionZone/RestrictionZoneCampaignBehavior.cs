using HarmonyLib;
using SandBox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.Localization;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignMechanics.RestrictionZone
{
    public class RestrictionZoneCampaignBehavior : CampaignBehaviorBase
    {
        private readonly Dictionary<int, RestrictionZone> _restrictionZones = [];

        public override void RegisterEvents()
        {
            CampaignEvents.OnAfterSessionLaunchedEvent.AddNonSerializedListener(this, OnAfterSessionLaunched);
            CampaignEvents.HourlyTickPartyEvent.AddNonSerializedListener(this, OnAIHourlyTick);
            CampaignEvents.OnQuarterDailyPartyTick.AddNonSerializedListener(this, OnQuarterDailyPartyTick);
            CampaignEvents.OnSettlementOwnerChangedEvent.AddNonSerializedListener(this, OnSettlementOwnerChanged);
        }

        private void OnSettlementOwnerChanged(Settlement settlement, bool arg2, Hero hero1, Hero hero2, Hero hero3, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail detail)
        {
            foreach(var zone in _restrictionZones.Values)
            {
                zone.UpdateSettlementsAndFactions();
            }
        }

        private void OnAfterSessionLaunched(CampaignGameStarter starter)
        {
            var atherLoren = new RestrictionZone { Name = new TextObject("Athel Loren"), NavMeshFaceId = 50, CanPartyEnter = CanPartyEnterAthelLoren };
            
            _restrictionZones.Clear();
            _restrictionZones.Add(atherLoren.NavMeshFaceId, atherLoren);

            foreach(var zone in _restrictionZones.Values) zone.UpdateSettlementsAndFactions();
        }

        private bool CanPartyEnterAthelLoren(MobileParty party, RestrictionZone zone)
        {
            if (RestrictionZone.CanPartyEnterCommonConditions(party, zone)) return true;
            if (party.Owner?.Culture?.StringId == "battania") return true;
            return false;
        }

        private void OnAIHourlyTick(MobileParty party)
        {
            if(!party.IsDisbanding && party.MapEvent == null && party.CurrentSettlement == null && party.TargetSettlement != null && party.Army == null)
            {
                if(_restrictionZones.ContainsKey(party.CurrentNavigationFace.FaceGroupIndex))
                {
                    var zone = _restrictionZones[party.CurrentNavigationFace.FaceGroupIndex];
                    if (!zone.CanPartyEnter(party, zone))
                    {
                        var selectedVillage = TORCommon.FindNearestSettlement(party, 500f, x => x.IsVillage && !_restrictionZones.Keys.ContainsQ(x.CurrentNavigationFace.FaceGroupIndex));
                        if (selectedVillage != null)
                        {
                            party.Position2D = selectedVillage.Position2D;
                            party.Ai.RethinkAtNextHourlyTick = true;
                            party.Ai.SetDoNotMakeNewDecisions(false);
                        }
                    }
                }
            }
        }

        private void OnQuarterDailyPartyTick(MobileParty party)
        {
            if (!party.IsDisbanding && party.MapEvent == null && party.CurrentSettlement == null && party.TargetSettlement != null)
            {
                if (party.IsCaravan && !party.CanReachSettlement(party.TargetSettlement))
                {
                    var selectedTown = TORCommon.FindNearestSettlement(party, 500f, x => x.IsTown && !_restrictionZones.Keys.ContainsQ(x.CurrentNavigationFace.FaceGroupIndex));
                    if (selectedTown != null)
                    {
                        party.Position2D = selectedTown.GatePosition;
                        party.Ai.SetMoveGoToSettlement(selectedTown);
                    }
                }
            }
        }

        public int[] GetExclusionFaceIdsFor(MobileParty party)
        {
            List<int> result = [];
            foreach (var zone in _restrictionZones.Values)
            {
                if (!zone.CanPartyEnter(party, zone))
                {
                    result.Add(zone.NavMeshFaceId);
                }
            }
            return result.ToArray();
        }

        public bool IsNavMeshFaceIdRestrictedForParty(int faceId, MobileParty party)
        {
            if (_restrictionZones.ContainsKey(faceId))
            {
                var zone = _restrictionZones[faceId];
                return !zone.CanPartyEnter(party, zone);
            }
            return false;
        }

        public override void SyncData(IDataStore dataStore) { }
    }
}
