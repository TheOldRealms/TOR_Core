using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Source.Missions;
using TaleWorlds.ObjectSystem;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.BattleMechanics.Banners
{
    public class CustomBannerMissionLogic : MissionLogic
    {
        private Queue<Agent> _unprocessedAgents = new Queue<Agent>();
        private bool _hasUnprocessedAgents;
        private int _counter = 0;
        private readonly int NthAgentToAddBannerTo = 15;

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
                    try
                    {
                        SwitchShieldPattern(agent, banner);
                        if(_counter > NthAgentToAddBannerTo)
                        {
                            _counter = 0;
                            AssignBanner(agent, banner);
                        }
                    }
                    catch
                    {
                        TORCommon.Log("Tried to assign shield pattern to agent but failed.", NLog.LogLevel.Error);
                    }
                    _counter++;
                }
                _hasUnprocessedAgents = false;
            }
        }

        private void SwitchShieldPattern(Agent agent, Banner banner)
        {
            if (agent.State != AgentState.Active) return;
            if (banner != null)
            {
                for (int i = 0; i < 5; i++)
                {
                    if (!agent.Equipment[i].IsEmpty && agent.Equipment[i].Item.Type == ItemObject.ItemTypeEnum.Shield)
                    {
                        var equipment = agent.Equipment[i];
                        if (equipment.Item.Type == ItemObject.ItemTypeEnum.Shield)
                        {
                            string stringId = equipment.Item.StringId;
                            var item = MBObjectManager.Instance.GetObject<ItemObject>(stringId);
                            if(item != null && item.IsUsingTableau)
                            {
                                agent.RemoveEquippedWeapon((EquipmentIndex)i);
                                var missionWeapon = new MissionWeapon(MBObjectManager.Instance.GetObject<ItemObject>(stringId), equipment.ItemModifier, banner);
                                agent.EquipWeaponWithNewEntity((EquipmentIndex)i, ref missionWeapon);
                            }
                        }
                    }
                }
                
            }
        }

        private void AssignBanner(Agent agent, Banner banner)
        {
            if (agent.State != AgentState.Active) return;
            if (_counter > NthAgentToAddBannerTo)
            {
                var equipment = agent.Equipment[EquipmentIndex.Weapon3];
                if (equipment.IsEmpty)
                {
                    _counter = 0;
                    var itemId = GetBannerNameForAgent(agent);
                    bool withBanner = itemId == "tor_empire_faction_banner_001" ? true : false;
                    var bannerWeapon = new MissionWeapon(MBObjectManager.Instance.GetObject<ItemObject>(itemId), null, withBanner ? banner : null);
                    agent.EquipWeaponWithNewEntity(EquipmentIndex.ExtraWeaponSlot, ref bannerWeapon);
                }
            }
            _counter++;
        }

        private Banner DetermineBanner(Agent agent)
        {
            string factionId = "";
            if (Game.Current.GameType is Campaign)
            {
                if (agent.Origin is PartyAgentOrigin)
                {
                    var origin = agent.Origin as PartyAgentOrigin;
                    factionId = origin.Party.MapFaction.StringId;
                    if(origin.Party.MapFaction.Leader == Hero.MainHero)
                    {
                        return Hero.MainHero.ClanBanner;
                    }
                }
                if (agent.Origin is PartyGroupAgentOrigin)
                {
                    var origin = agent.Origin as PartyGroupAgentOrigin;
                    factionId = origin.Party.MapFaction.StringId;
                    if (origin.Party.MapFaction.Leader == Hero.MainHero)
                    {
                        return Hero.MainHero.ClanBanner;
                    }
                }
            }
            return CustomBannerManager.GetRandomBannerFor(agent.Character.Culture.StringId, factionId);
        }

        private string GetBannerNameForAgent(Agent agent)
        {
            List<string> list = new List<string>();
            list.Add("tor_empire_faction_banner_001");
            if (agent.IsUndead())
            {
                list.Add("tor_vc_weapon_banner_002");
                list.Add("tor_vc_weapon_banner_003");
                list.Add("tor_vc_weapon_banner_undead_001");
            }
            else if (agent.Origin is PartyAgentOrigin)
            {
                var origin = agent.Origin as PartyAgentOrigin;
                if (origin.Party.Owner.Clan.StringId == "chaos_clan_1")
                {
                    list.Add("tor_chaos_weapon_banner_001");
                    list.Add("tor_chaos_weapon_banner_002");
                }
                else if (origin.Party.MapFaction.StringId == "averland" && (origin.Troop.IsHero || origin.Troop.Level >= 26))
                {
                    list.Add("tor_empire_weapon_banner_002");
                    list.Add("tor_empire_weapon_banner_003");
                }
                else if (origin.Party.MapFaction.StringId == "stirland" && (origin.Troop.IsHero || origin.Troop.Level >= 26))
                {
                    list.Add("tor_empire_weapon_banner_001");
                }
            }
            return list.TakeRandom(1).FirstOrDefault();
        }
    }
}
