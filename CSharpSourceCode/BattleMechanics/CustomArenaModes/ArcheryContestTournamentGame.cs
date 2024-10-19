﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.CampaignSystem.TournamentGames;
using TaleWorlds.Core;
using TOR_Core.Extensions;
using TOR_Core.Missions;

namespace TOR_Core.BattleMechanics.CustomArenaModes
{
    public class ArcheryContestTournamentGame : FightTournamentGame
    {
        public override int MaxTeamSize => 1;
        public override int MaxTeamNumberPerMatch => 2;

        public ArcheryContestTournamentGame(Town town) : base(town)
        {
            Mode = QualificationMode.IndividualScore;
        }

        public override bool CanBeAParticipant(CharacterObject character, bool considerSkills)
        {
            if (character.Race == FaceGen.GetRaceOrDefault("large_humanoid_monster") ||
                character.HasAttribute("HasAnimationTriggeredEffects") ||
                character.Culture?.StringId == "chaos_culture")
            {
                return false;
            }

            return base.CanBeAParticipant(character, considerSkills);
        }

        public override void OpenMission(Settlement settlement, bool isPlayerParticipating)
        {
            int upgradeLevel = settlement.IsTown ? settlement.Town.GetWallLevel() : 1;
            TorMissionManager.OpenArcheryContestMission(LocationComplex.Current.GetScene("arena", upgradeLevel), this, settlement, settlement.Culture, isPlayerParticipating);
        }
    }
}
