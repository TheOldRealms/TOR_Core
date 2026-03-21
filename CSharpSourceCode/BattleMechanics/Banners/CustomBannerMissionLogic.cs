using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Source.Missions;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.ObjectSystem;
using TOR_Core.AbilitySystem;
using TOR_Core.Extensions;
using TOR_Core.Utilities;
using static TaleWorlds.Core.ItemObject;

namespace TOR_Core.BattleMechanics.Banners
{
    public class CustomBannerMissionLogic : MissionLogic
    {
        private readonly Queue<Agent> _unprocessedAgents = new();
        private bool _hasUnprocessedAgents;
        private int _indexOfCurrentAgent = 0;
        private readonly Dictionary<int, EquipmentIndex> _agentsWithBanners = [];


        /*Sly : could we use
         * public AgentData Equipment(Equipment equipment)
         * {
         * 	this.AgentOverridenEquipment = equipment;
         * 	return this;
         * }
         * to override equipment on specific agents before/during Mission.SpawnAgent which is prior to OnAgentBuild?
         * It goes MissionAgentSpawnLogic.SpawnTroops -> Mission.SpawnTroop -> Mission.SpawnAgent which calls Mission.BuildAgent and then all of the mission behaviours with OnAgentBuild which brings us here
         * 
         */
        public override void OnAgentBuild(Agent agent, Banner banner)
        {
            if (!Mission.Current.HasMissionBehavior<BattleSpawnLogic>() && Game.Current.GameType is Campaign) return;
            if (agent.IsHuman)
            {
                _hasUnprocessedAgents = true;
                _unprocessedAgents.Enqueue(agent);
            }
        }

        public override void OnMissionTick(float dt)
        {
            if (!Mission.Current.HasMissionBehavior<BattleSpawnLogic>() && Game.Current.GameType is Campaign) return;
            if (_hasUnprocessedAgents)
            {
                while (_unprocessedAgents.Count > 0)
                {
                    var agent = _unprocessedAgents.Dequeue();
                    var banner = DetermineBanner(agent);
                    if (agent == null || agent.State != AgentState.Active || agent.IsFadingOut()) continue;
                    if (banner == null)
                    {
                        TORCommon.Log("CustomBannerMissionLogic : Tried to assign null shield pattern to agent : " + agent.ToString() + ".", NLog.LogLevel.Warn);
                        continue;
                    }
                    try
                    {
                        SwitchTableauPatterns(agent, banner);
                        if (_indexOfCurrentAgent > TORConfig.FakeBannerFrequency)
                        {
                            if (CheckEligibleAndAddBanner(agent, banner))
                            {
                                _indexOfCurrentAgent = 0;
                            }
                        }
                        _indexOfCurrentAgent++;
                    }
                    catch(Exception e)
                    {
                        TORCommon.Log("CustomBannerMissionLogic : Tried to assign shield pattern to agent but failed. Error: " + e.Message, NLog.LogLevel.Warn);
                    }
                }
                _hasUnprocessedAgents = false;
            }
        }

        public override void OnEarlyAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
        {
            if (_agentsWithBanners.ContainsKey(affectedAgent.Index))
            {
                if (affectedAgent.Equipment != null &&
                    !affectedAgent.Equipment[_agentsWithBanners[affectedAgent.Index]].IsEmpty)
                {
                    //Sly : is this here because of some sort of crash? or is the intention to have far less shields be on the ground from agents dying?
                    affectedAgent.RemoveEquippedWeapon(_agentsWithBanners[affectedAgent.Index]);
                }
            }
        }

        private void SwitchTableauPatterns(Agent agent, Banner banner)
        {
            if (agent.State != AgentState.Active || agent.IsFadingOut()) return;
            if (banner != null)
            {
                for (int i = 0; i < 5; i++)
                {
                    var equipment = agent.Equipment[i];
                    if (!equipment.IsEmpty && equipment.Item != null)
                    {
                        if (equipment.Item.ItemType == ItemTypeEnum.Shield)
                        {
                            if (equipment.Item.IsUsingTableau)
                            {
                                agent.RemoveEquippedWeapon((EquipmentIndex)i);
                                var missionWeapon = new MissionWeapon(equipment.Item, equipment.ItemModifier, banner);
                                agent.EquipWeaponWithNewEntity((EquipmentIndex)i, ref missionWeapon);

                                //addresses an issue with the start of siege assaults where attacking formations attached to siege equipment won't equip their new shields until the siege equipment has finished its role
                                //issue with AIStateFlag.UseObjectWaiting that's preventing changing wielded items maybe?
                                //UsableMissionObject movingToOrDefendingObject + usedObject is StandingPoint ?
                                //_equipmentOnOffHandBeforeUsingObject
                                agent.TryToWieldWeaponInSlot((EquipmentIndex)i, Agent.WeaponWieldActionType.WithAnimation, false);
                            }
                        }
                    }
                }
            }
        }

        private bool CheckEligibleAndAddBanner(Agent agent, Banner banner)
        {
            if (agent.IsHero) return false;
            if (agent.IsSummoned()) return false;
            if (agent.IsTreeSpirit()) return false;
            if (agent.IsTroll()) return false;
            if (Game.Current.GameType is Campaign && Campaign.Current != null)
            {
                if (agent.Origin is PartyAgentOrigin origin)
                {
                    var party = origin.Party;
                    if (!party.IsMobile) return false;
                    if (!party.MobileParty.IsLordParty) return false;
                }
                else if (agent.Origin is PartyGroupAgentOrigin groupOrigin)
                {
                    var party = groupOrigin.Party;
                    if (!party.IsMobile) return false;
                    if (!party.MobileParty.IsLordParty) return false;
                }
                else return false;
            }

            var equipment = agent.Equipment;
            if (equipment.HasAnyWeaponWithFlags(WeaponFlags.NotUsableWithOneHand)) return false;
            var weaponList = GetWeaponItems(equipment).ToList();
            if (weaponList.Any(x => !x.IsEmpty && x.Item != null && x.Item.ItemType == ItemTypeEnum.TwoHandedWeapon)) return false;

            int freeSlotIndex = GetFirstFreeSlot(equipment);
            if (freeSlotIndex >= 0)
            {
                var fakeBanner = GetFakeBannerObject(agent.Character.GetBattleTier());
                var missionWeapon = new MissionWeapon(fakeBanner, null, banner);
                agent.EquipWeaponWithNewEntity((EquipmentIndex)freeSlotIndex, ref missionWeapon);
                _agentsWithBanners.Add(agent.Index, (EquipmentIndex)freeSlotIndex);
                return true;
            }
            TORCommon.Log("CustomBannerLogic : free slot index < 0", NLog.LogLevel.Info);
            return false;
        }

        private ItemObject GetFakeBannerObject(int agentTier)
        {
            if (agentTier > 6) return MBObjectManager.Instance.GetObject<ItemObject>("tor_fake_faction_banner_001_t3");
            else if (agentTier > 3) return MBObjectManager.Instance.GetObject<ItemObject>("tor_fake_faction_banner_001_t2");
            return MBObjectManager.Instance.GetObject<ItemObject>("tor_fake_faction_banner_001_t1");
        }

        private IEnumerable<MissionWeapon> GetWeaponItems(MissionEquipment equipment)
        {
            for (int i = 0; i < 5; i++)
            {
                yield return equipment[i];
            }
        }

        private int GetFirstFreeSlot(MissionEquipment equipment)
        {
            for (int i = 0; i < 5; i++)
            {
                if (equipment[i].IsEmpty) return i;
            }
            return -1;
        }

        //does not work, although it should. No error, just nothing happens
        private void ApplyTextureToMesh(Texture t, Mesh currentMesh)
        {
            if (currentMesh != null)
            {
                Material material = currentMesh.GetMaterial()?.CreateCopy();
                if (material != null)
                {
                    material.SetTexture(Material.MBTextureType.DiffuseMap2, t);
                    uint num = (uint)material.GetShader().GetMaterialShaderFlagMask("use_tableau_blending", true);
                    ulong shaderFlags = material.GetShaderFlags();
                    material.SetShaderFlags(shaderFlags | (ulong)num);
                    currentMesh.SetMaterial(material);
                }
            }
        }

        private Banner DetermineBanner(Agent agent)
        {
            string factionId = "";
            if (Game.Current.GameType is Campaign)
            {
                if (agent.Origin is PartyAgentOrigin origin)
                {
                    //var origin = agent.Origin as PartyAgentOrigin;
                    factionId = origin.Party.MapFaction.StringId;
                    if (origin.Party.MapFaction.Leader == Hero.MainHero)
                    {
                        return Hero.MainHero.ClanBanner;
                    }
                }
                if (agent.Origin is PartyGroupAgentOrigin groupOrigin)
                {
                    //var origin = agent.Origin as PartyGroupAgentOrigin;
                    factionId = groupOrigin.Party.MapFaction.StringId;
                    if (groupOrigin.Party.MapFaction.Leader == Hero.MainHero)
                    {
                        return Hero.MainHero.ClanBanner;
                    }
                }
            }
            return CustomBannerManager.GetRandomBannerFor(agent.Character.Culture.StringId, factionId);
        }
    }
}