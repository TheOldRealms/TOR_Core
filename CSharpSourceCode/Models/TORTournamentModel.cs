using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.TournamentGames;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.ObjectSystem;
using TOR_Core.BattleMechanics.CustomArenaModes;
using TOR_Core.CampaignMechanics.Assimilation;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.Models
{
    public class TORTournamentModel : DefaultTournamentModel
    {
        public override TournamentGame CreateTournament(Town town)
        {
            var culture = AssimilationCampaignBehavior.GetOriginalCultureForSettlement(town.Settlement);
            if (culture != null)
            {
                if (culture.StringId == TORConstants.Cultures.BRETONNIA || culture.StringId == TORConstants.Cultures.MOUSILLON)
                {
                    return new JoustTournamentGame(town);
                }
                if (culture.StringId == TORConstants.Cultures.ASRAI)
                {
                    return new ArcheryContestTournamentGame(town);
                }
            }
            return base.CreateTournament(town);
        }

        public override float GetTournamentStartChance(Town town)
        {
            //return 1f; //DEBUG
            if (town.Settlement.SiegeEvent != null)
            {
                return 0f;
            }

            if (Math.Abs(town.StringId.GetHashCode() % 3) != CampaignTime.Now.GetWeekOfSeason)
            {
                return 0f;
            }

            return 0.1f * (float)(town.Settlement.Parties.Count((MobileParty x) => x.IsLordParty)) + 0.05f * (float)(town.Settlement.HeroesWithoutParty.WhereQ(x => x.IsWanderer).Count());
        }

        public override MBList<ItemObject> GetRegularRewardItems(Town town, int regularRewardMinValue, int regularRewardMaxValue)
        {
            //Sly : if the culture has no items, that should be looked into. Not a fan of building a 2nd list to store every other culture's items.
            MBList<ItemObject> mBList = new MBList<ItemObject>();
            MBList<ItemObject> mBList2 = new MBList<ItemObject>();
            foreach (ItemObject item in Game.Current.ObjectManager.GetObjectTypeList<ItemObject>())
            {
                if (!item.NotMerchandise && item.Tier > ItemObject.ItemTiers.Tier2 && item.Tier < ItemObject.ItemTiers.Tier5 && (item.IsCraftedWeapon || item.IsMountable || item.ArmorComponent != null) && !item.IsCraftedByPlayer && item.IsTorItem())
                {
                    if (item.Culture == town.Culture)
                    {
                        mBList.Add(item);
                    }
                }
            }

            //Sly : banners left out because native will collect all banners even unrelated cultures

            if (mBList.IsEmpty())
            {
                TORCommon.Log("TORTournamentModel : no t3 or 4 item found belonging to the " + town.Culture.StringId + " culture. They get the first item object instead.", NLog.LogLevel.Warn);
                mBList.Add(Game.Current.ObjectManager.GetFirstObject<ItemObject>());
            }

            return mBList;
        }

        public override MBList<ItemObject> GetEliteRewardItems(Town town, int regularRewardMinValue, int regularRewardMaxValue)
        {
            MBList<ItemObject> mBList = new MBList<ItemObject>();
            foreach (ItemObject item in Game.Current.ObjectManager.GetObjectTypeList<ItemObject>())
            {
                if (!item.NotMerchandise && item.Culture == town.Culture && item.Tier > ItemObject.ItemTiers.Tier4 && (item.IsCraftedWeapon || item.IsMountable || item.ArmorComponent != null) && !item.IsCraftedByPlayer && item.IsTorItem())
                {
                    mBList.Add(item);
                }
            }

            if (mBList.IsEmpty())
            {
                TORCommon.Log("TORTournamentModel : no t5 or 6 item found belonging to the " + town.Culture.StringId + " culture. They get the first item object instead.", NLog.LogLevel.Warn);
                mBList.Add(Game.Current.ObjectManager.GetFirstObject<ItemObject>());
            }

            return mBList;
        }

        public override Equipment GetParticipantArmor(CharacterObject participant)
        {
            // For practice arena fights (not tournaments), provide race-appropriate armor
            if (CampaignMission.Current != null && CampaignMission.Current.Mode != MissionMode.Tournament && Settlement.CurrentSettlement != null)
            {
                string[] armorRosterIds = null;

                // Check for Greenskin races (Orcs/Goblins)
                if (participant.IsOrc())
                {
                    armorRosterIds = new string[]
                    {
                        "tor_gs_orc_template",
                    };
                }
                else if (participant.IsGoblin())
                {
                    armorRosterIds = new string[]
                    {
                        "tor_gs_goblin_template"
                    };
                }
                // Check for Dwarf race
                else if (participant.IsDwarf())
                {
                    armorRosterIds = new string[]
                    {
                        "tor_dw_recruit_template"
                    };
                }

                // If we have roster IDs for armor, return random armor equipment
                if (armorRosterIds != null)
                {
                    Equipment armorEquipment = GetRandomEquipmentFromRoster(armorRosterIds);
                    if (armorEquipment != null)
                    {
                        return armorEquipment;
                    }
                }
            }

            // Fall back to base implementation for tournaments and other races
            return base.GetParticipantArmor(participant);
        }

        /// <summary>
        /// Gets practice weapons for non-human races.
        /// Called by ArenaPracticePatch to replace native weapon assignment.
        /// </summary>
        /// <remarks>
        /// This will need to receive an argument for the participant count in order to make use of the one and two participant sets as it is unable to differentiate between the contexts currently.
        /// </remarks>
        public Equipment GetParticipantWeapons(CharacterObject participant)
        {
            string[] weaponRosterIds = null;

            // Check for Greenskin races (Orcs/Goblins)
            if (participant.IsOrc())
            {
                weaponRosterIds = ["tor_gs_tournament_template_four_participant_v1"];
            }
            else if (participant.IsGoblin())
            {
                weaponRosterIds = ["tor_gs_goblin_tournament_template_four_participant_v1"];
            }
            // Check for Dwarf race
            else if (participant.IsDwarf())
            {
                weaponRosterIds = ["tor_dw_tournament_template_four_participant_v1"];
            }

            // If we have roster IDs for weapons, return random weapon equipment
            if (weaponRosterIds != null)
            {
                return GetRandomEquipmentFromRoster(weaponRosterIds);
            }

            return null;
        }

        private Equipment GetRandomEquipmentFromRoster(string[] rosterIds)
        {
            if (rosterIds == null || rosterIds.Length == 0)
                return null;

            string randomRosterId = rosterIds[MBRandom.RandomInt(rosterIds.Length)];
            MBEquipmentRoster roster = Game.Current.ObjectManager.GetObject<MBEquipmentRoster>(randomRosterId);

            if (roster != null && roster.AllEquipments.Count > 0)
            {
                return roster.AllEquipments[MBRandom.RandomInt(roster.AllEquipments.Count)].Clone();
            }

            return null;
        }
    }
}