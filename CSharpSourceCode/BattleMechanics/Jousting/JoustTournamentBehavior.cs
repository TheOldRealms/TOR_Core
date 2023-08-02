using SandBox.Tournaments;
using SandBox.Tournaments.MissionLogics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.TournamentGames;

namespace TOR_Core.BattleMechanics.Jousting
{
    public class JoustTournamentBehavior : TournamentBehavior
    {
        public JoustTournamentBehavior(TournamentGame tournamentGame, Settlement settlement, ITournamentGameBehavior gameBehavior, bool isPlayerParticipating) : base(tournamentGame, settlement, gameBehavior, isPlayerParticipating)
        {
        }
    }
}
