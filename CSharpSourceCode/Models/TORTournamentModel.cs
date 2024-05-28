using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.TournamentGames;
using TOR_Core.BattleMechanics.Jousting;
using TOR_Core.CampaignMechanics.Assimilation;
using TOR_Core.Utilities;

namespace TOR_Core.Models
{
    public class TORTournamentModel : DefaultTournamentModel
    {
        public override TournamentGame CreateTournament(Town town)
        {
            var culture = AssimilationCampaignBehavior.GetOriginalCultureForSettlement(town.Settlement);
            if (culture != null && culture.StringId == TORConstants.Cultures.BRETONNIA || culture.StringId == "mousillon")
            {
                return new JoustTournamentGame(town);
            }
            else return base.CreateTournament(town);
        }

        public override float GetTournamentStartChance(Town town)
        {
            //return 1f; //DEBUG
            return base.GetTournamentStartChance(town);
        }
    }
}
