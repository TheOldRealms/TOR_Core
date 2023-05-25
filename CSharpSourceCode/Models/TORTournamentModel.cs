using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.TournamentGames;
using TOR_Core.BattleMechanics.Jousting;

namespace TOR_Core.Models
{
    public class TORTournamentModel : DefaultTournamentModel
    {
        public override TournamentGame CreateTournament(Town town)
        {
            if(town.Culture.StringId == "vlandia")
            {
                return new JoustTournamentGame(town);
            }
            else return base.CreateTournament(town);
        }

        public override float GetTournamentStartChance(Town town)
        {
            //return 0.8f; //DEBUG
            return base.GetTournamentStartChance(town);
        }
    }
}
