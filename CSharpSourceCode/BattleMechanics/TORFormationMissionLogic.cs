using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TOR_Core.CampaignMechanics.Formations;

namespace TOR_Core.BattleMechanics
{
    public class TORFormationMissionLogic : MissionLogic
    {
        // native captain selection ignores saved hero assignments when deployment is skipped, f.e when the player helps an allied party
        public override void OnBattleSideSpawned(BattleSideEnum side)
        {
            if (!TORFormationAssignments.AppliesToPlayerTeam(side) ||
                MissionGameModels.Current.BattleInitializationModel.CanPlayerSideDeployWithOrderOfBattle())
            {
                return;
            }

            var team = Mission.PlayerTeam;
            var captainPreferences = Campaign.Current.GetCampaignBehavior<TORFormationCampaignBehavior>();
            var bannerLogic = Mission.GetMissionBehavior<BannerBearerLogic>();
            var mainAgent = Mission.InitialPlayerAgent;
            var savedMainHeroFormation = -1;
            for (var i = 0; i < 8; i++)
            {
                if (captainPreferences.GetCaptain(i) == Hero.MainHero)
                {
                    savedMainHeroFormation = i;
                }
            }

            foreach (var agent in team.ActiveAgents)
            {
                if (!agent.IsMainAgent && agent.IsHero && agent.Character is CharacterObject character &&
                    character.HeroObject.PartyBelongedTo == MobileParty.MainParty)
                {
                    var formation = team.GetFormation(TORFormationAssignments.GetFormation(character));
                    if (agent.Formation != formation)
                    {
                        agent.Formation = formation;
                    }
                }
            }

            for (var i = 0; i < 8; i++)
            {
                var formation = team.GetFormation((FormationClass)i);
                if (formation.Captain == mainAgent && savedMainHeroFormation >= 0 && savedMainHeroFormation != i)
                {
                    formation.Captain = null;
                    bannerLogic?.SetFormationBanner(formation, null);
                }
                else if (formation.Captain != null && formation.Captain != mainAgent &&
                    formation.Captain.Formation != formation)
                {
                    formation.Captain = null;
                    bannerLogic?.SetFormationBanner(formation, null);
                }

                var previousCaptain = captainPreferences.GetCaptain(i);
                var captain = previousCaptain == Hero.MainHero ? mainAgent :
                    team.ActiveAgents.FirstOrDefault(agent => agent.IsHero && !agent.IsMainAgent &&
                        agent.Formation == formation && agent.Character == previousCaptain?.CharacterObject &&
                        ((CharacterObject)agent.Character).HeroObject.PartyBelongedTo == MobileParty.MainParty);

                if (captain == null && formation.Captain != null)
                {
                    captain = formation.Captain;
                }

                if (captain == null)
                {
                    var candidates = team.ActiveAgents.Where(agent => agent.IsHero && !agent.IsMainAgent &&
                        agent.Formation == formation && agent.Character is CharacterObject character &&
                        character.HeroObject.PartyBelongedTo == MobileParty.MainParty).ToList();
                    if (candidates.Count > 0)
                    {
                        captain = candidates[MBRandom.RandomInt(candidates.Count)];
                    }
                }

                if (captain != formation.Captain)
                {
                    formation.Captain = captain;
                    bannerLogic?.SetFormationBanner(formation, captain?.FormationBanner);
                    team.TriggerOnFormationsChanged(formation);
                    formation.QuerySystem.Expire();
                }

                if (captain != null && captain.Character is CharacterObject captainCharacter &&
                    (captainCharacter.HeroObject == Hero.MainHero ||
                     captainCharacter.HeroObject.PartyBelongedTo == MobileParty.MainParty))
                {
                    captainPreferences.SetCaptain(i, captainCharacter.HeroObject);
                }
                else
                {
                    captainPreferences.ClearCaptain(i);
                }
            }
        }
    }
}
