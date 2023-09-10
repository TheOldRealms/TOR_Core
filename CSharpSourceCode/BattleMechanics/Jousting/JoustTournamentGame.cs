using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.CampaignSystem.TournamentGames;
using TaleWorlds.SaveSystem;
using TOR_Core.Missions;

namespace TOR_Core.BattleMechanics.Jousting
{
    public class JoustTournamentGame : FightTournamentGame
    {
        public override int MaxTeamSize => 1;
        public override int MaxTeamNumberPerMatch => 2;

        public JoustTournamentGame(Town town) : base(town)
        {
            Mode = QualificationMode.IndividualScore;
        }

        public override void OpenMission(Settlement settlement, bool isPlayerParticipating)
        {
            int upgradeLevel = settlement.IsTown ? settlement.Town.GetWallLevel() : 1;
            TorMissionManager.OpenJoustingFightMission(LocationComplex.Current.GetScene("arena", upgradeLevel), this, settlement, settlement.Culture, isPlayerParticipating);
        }
    }
}
