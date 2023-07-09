using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Source.Missions;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.ObjectSystem;
using TOR_Core.Extensions;
using TOR_Core.Utilities;
using static TaleWorlds.Core.ItemObject;

namespace TOR_Core.BattleMechanics.Banners
{
    public class CustomBannerMissionLogic : MissionLogic
    {
        private Queue<Agent> _unprocessedAgents = new Queue<Agent>();
        private bool _hasUnprocessedAgents;

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
                        SwitchTableauPatterns(agent, banner);
                    }
                    catch
                    {
                        TORCommon.Log("Tried to assign shield pattern to agent but failed.", NLog.LogLevel.Error);
                    }
                }
                _hasUnprocessedAgents = false;
            }
        }

        private void SwitchTableauPatterns(Agent agent, Banner banner)
        {
            if (agent.State != AgentState.Active) return;
            if (banner != null)
            {
                for (int i = 0; i < 5; i++)
                {
                    var equipment = agent.Equipment[i];
                    if (!equipment.IsEmpty && equipment.Item != null)
                    {
                        /* THIS DOES NOT WORK FOR SOME REASON
                        if (equipment.Item.IsBannerItem)
                        {
                            var multiMesh = equipment.GetMultiMesh(agent.IsFemale, false, true);
                            var visual = banner.BannerVisual as BannerVisual;

                            if (multiMesh != null && visual != null)
                            {
                                for (int j = 0; j < multiMesh.MeshCount; j++)
                                {
                                    var currentMesh = multiMesh.GetMeshAtIndex(j);
                                    bool noTableau = currentMesh.HasTag("dont_use_tableau");
                                    bool isBannerReplacementMesh = currentMesh.HasTag("banner_replacement_mesh");
                                    if (currentMesh != null && !noTableau && isBannerReplacementMesh)
                                    {
                                        visual.GetTableauTextureLarge(t => ApplyTextureToMesh(t, currentMesh));
                                        currentMesh.ManualInvalidate();
                                    }
                                }
                                multiMesh.ManualInvalidate();
                            }
                        }
                        */
                        if(equipment.Item.ItemType == ItemTypeEnum.Shield)
                        {
                            if (equipment.Item.IsUsingTableau)
                            {
                                agent.RemoveEquippedWeapon((EquipmentIndex)i);
                                var missionWeapon = new MissionWeapon(equipment.Item, equipment.ItemModifier, banner);
                                agent.EquipWeaponWithNewEntity((EquipmentIndex)i, ref missionWeapon);
                            }
                            /* THIS BLOODY DOESNT WORK EITHER
                            else if(equipment.Item.IsUsingTeamColor)
                            {
                                var multiMesh = equipment.GetMultiMesh(agent.IsFemale, false, true);

                                if (multiMesh != null)
                                {
                                    for (int j = 0; j < multiMesh.MeshCount; j++)
                                    {
                                        var currentMesh = multiMesh.GetMeshAtIndex(j);

                                        if (currentMesh != null)
                                        {
                                            currentMesh.Color = agent.Team.Color;
                                            currentMesh.Color2 = agent.Team.Color2;
                                            Material material = currentMesh.GetMaterial().CreateCopy();
                                            material.AddMaterialShaderFlag("use_double_colormap_with_mask_texture", false);
                                            currentMesh.SetMaterial(material);
                                            //material.ManualInvalidate();
                                            currentMesh.ManualInvalidate();
                                        }
                                    }
                                    multiMesh.ManualInvalidate();
                                }
                            }
                            */
                        }
                        */
                        if(equipment.Item.ItemType == ItemTypeEnum.Shield)
                        {
                            agent.RemoveEquippedWeapon((EquipmentIndex)i);
                            var missionWeapon = new MissionWeapon(equipment.Item, equipment.ItemModifier, banner);
                            agent.EquipWeaponWithNewEntity((EquipmentIndex)i, ref missionWeapon);
                        }
                    }
                }
                
            }
        }

        private void ApplyTextureToMesh(Texture t, Mesh currentMesh)
        {
            if (currentMesh != null)
            {
                Material material = currentMesh.GetMaterial()?.CreateCopy();
                if(material != null)
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
    }
}
