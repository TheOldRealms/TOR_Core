using TaleWorlds.CampaignSystem;
using TaleWorlds.ObjectSystem;
using TOR_Core.Extensions;

namespace TOR_Core.CampaignMechanics
{
    public class TORRecruitmentHelpers
    {
        public static CharacterObject GetMousillonEquivalent(CharacterObject bretonnianTroop)
        {
            if (bretonnianTroop.IsHero) return null;

            if (!bretonnianTroop.IsEliteTroop())
            {
                if (bretonnianTroop.StringId.Contains("warden") || bretonnianTroop.StringId.Contains("foot_squire"))
                    return null;

                var reducedId = bretonnianTroop.StringId.Substring(7);

                var mousillonID = "tor_m_" + reducedId;

                var troop = MBObjectManager.Instance.GetObject<CharacterObject>(mousillonID);

                return troop;
            }

            var knightID = "";
            switch (bretonnianTroop.StringId)
            {
                case "tor_br_noble":
                    knightID = "tor_m_illfated_squire";
                    break;
                case "tor_br_knight_errant":
                    knightID = "tor_m_outcast_errant";
                    break;
                case "tor_br_realm_knight":
                    knightID = "tor_m_knight_of_misfortune";
                    break;
                case "tor_br_quest_knight":
                    knightID = "tor_m_doomed_questing_knight";
                    break;
                case "tor_br_grail_knight":
                    knightID = "tor_m_knight_of_the_black_grail";
                    break;
            }

            if (knightID == "") return null;

            var knight = MBObjectManager.Instance.GetObject<CharacterObject>(knightID);
            return knight;
        }

        public static CharacterObject GetBretonnianEquivalent(CharacterObject mousillonTroop)
        {
            if (mousillonTroop.IsHero) return null;

            if (!mousillonTroop.IsEliteTroop())
            {
                var reducedId = mousillonTroop.StringId.Substring(7);

                var bretonnianID = "tor_br_" + reducedId;

                var troop = MBObjectManager.Instance.GetObject<CharacterObject>(bretonnianID);

                return troop;
            }

            var knightID = "";
            switch (mousillonTroop.StringId)
            {
                case "tor_m_illfated_squire":
                    knightID = "tor_br_noble";
                    break;
                case "tor_m_outcast_errant":
                    knightID = "tor_br_knight_errant";
                    break;
                case "tor_br_realm_knight":
                    knightID = "tor_m_knight_of_misfortune";
                    break;
                case "tor_br_quest_knight":
                    knightID = "tor_m_doomed_questing_knight";
                    break;
                case "tor_br_grail_knight":
                    knightID = "tor_m_knight_of_the_black_grail";
                    break;
            }

            if (knightID == "") return null;

            var knight = MBObjectManager.Instance.GetObject<CharacterObject>(knightID);
            return knight;
        }
    }
}