using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignMechanics.SettlementPOIs
{
    public class EmpireEngineerHallPOI : SettlementPOI, ISkillTrainerHost
    {
        public override string LocationId => "tor_empire_engineer_hall";
        public override string ParentMenuId => "town_artisan";
        public override string AnchorEntryId => "town_artisan_leave";
        public override bool AnchorAbove => true;

        public override string EnterLabelId => "tor_poi_empire_engineer_hall.EnterLabel";
        public override string EnterLabelDefault => "Visit the Empire Engineer Hall";

        public string SkillTrainerTemplateId => "tor_nulnengineernpc_empire";

        // POI is "built" per settlement at runtime — Nuln has it via XML scene (auto-seeded
        // on load); other towns need an explicit EstablishAt call (quest reward, etc.).
        public override bool RequiresEstablishment => true;

        public override bool CheckAccess(Settlement s, Hero h, out TextObject denialReason)
        {
            if (!IsDiscovered(s))
            {
                denialReason = R("Undiscovered", "You have not yet found this place.");
                return false;
            }
            if (!AppliesTo(s))
            {
                denialReason = R("Sealed", "This place is sealed.");
                return false;
            }
            if (s.IsUnderSiege)
            {
                denialReason = R("SiegeSealed", "The gates of the Engineer Hall are sealed during the siege.");
                return false;
            }
            if (h.CharacterObject.Race != FaceGen.GetRaceOrDefault("human"))
            {
                denialReason = R("Rejected", "The engineers turn you away.");
                return false;
            }
            if (h.Culture.StringId != TORConstants.Cultures.EMPIRE)
            {
                denialReason = R("WrongCulture", "The Imperial engineers only serve those of the Empire.");
                return false;
            }

            denialReason = null;
            return true;
        }
    }
}
