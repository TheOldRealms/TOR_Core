using SandBox.Tournaments;
using SandBox.Tournaments.MissionLogics;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.TournamentGames;

namespace TOR_Core.BattleMechanics.CustomArenaModes
{
    public class JoustTournamentBehavior(TournamentGame tournamentGame, Settlement settlement, ITournamentGameBehavior gameBehavior, bool isPlayerParticipating) : TournamentBehavior(tournamentGame, settlement, gameBehavior, isPlayerParticipating)
    {
    }

    public class ArcheryContestTournamentBehavior(TournamentGame tournamentGame, Settlement settlement, ITournamentGameBehavior gameBehavior, bool isPlayerParticipating) : TournamentBehavior(tournamentGame, settlement, gameBehavior, isPlayerParticipating)
    {
    }
}