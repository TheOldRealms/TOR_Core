using TaleWorlds.CampaignSystem;

namespace TOR_Core.Extensions
{
    public static class CultureObjectExtensions
    {
        public static bool IsSuitableForMarriage(this CultureObject self, CultureObject comparable)
        {
            // If both are same culture
            if (self.Id == comparable.Id)
                return true;

            if (self.IsSameRace(comparable))
                return true;

            return false;
        }

        public static bool IsSameRace(this CultureObject self, CultureObject comparable)
        {
            // If cultures are both human
            if ((self.StringId == "empire" && comparable.StringId == "vlandia") ||
                (comparable.StringId == "empire" && self.StringId == "vlandia"))
                return true;

            // If cultures are both vampires
            if ((self.StringId == "khuzait" && comparable.StringId == "blooddragons") ||
                (comparable.StringId == "khuzait" && self.StringId == "blooddragons"))
                return true;

            return false;
        }
    }
}
