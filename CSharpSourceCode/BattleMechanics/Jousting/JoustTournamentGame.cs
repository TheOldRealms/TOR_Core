using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.TournamentGames;
using TaleWorlds.SaveSystem;

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
            //TODO open a different mission here if we want to
            base.OpenMission(settlement, isPlayerParticipating);
        }
    }
}
