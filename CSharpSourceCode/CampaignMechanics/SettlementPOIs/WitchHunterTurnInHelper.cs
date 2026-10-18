using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.ViewModelCollection;
using TaleWorlds.Core;
using TaleWorlds.Core.ImageIdentifiers;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignMechanics.SettlementPOIs
{
    /// <summary>
    /// Shared helpers for the Witch Hunter Lodge turn-in branch — prisoner filters, dialog
    /// conditions, and consequence callbacks. Lives in its own file so
    /// <see cref="WitchHunterLodgePOI"/> stays a thin dialog-registration surface.
    ///
    /// Reward constants are intentional placeholders; expect to tune after playtest.
    /// </summary>
    public static class WitchHunterTurnInHelper
    {
        public const int ChaosGoldPerHead = 50;
        public const int ChaosPrestigePerHead = 1;

        public const int UndeadGoldPerHead = 75;
        public const int UndeadPrestigePerHead = 2;

        public const int HeroGoldPerHead = 1000;
        public const int HeroPrestigePerHead = 25;
        public const int HeroInquisitorRelation = 2;

        public static int CountChaosPrisoners() =>
            PrisonerElements().Where(x => !x.Character.IsHero && x.Character.IsChaos())
                              .Sum(x => x.Number);

        public static int CountUndeadPrisoners() =>
            PrisonerElements().Where(x => !x.Character.IsHero && (x.Character.IsUndead() || x.Character.IsVampire()))
                              .Sum(x => x.Number);

        public static List<Hero> HereticHeroPrisoners() =>
            PrisonerElements().Where(x => x.Character.IsHero && x.Character.HeroObject != null
                                          && (x.Character.HeroObject.IsVampire() || x.Character.HeroObject.IsChaos()))
                              .Select(x => x.Character.HeroObject)
                              .ToList();

        public static bool AnyTurnInAvailable() =>
            CountChaosPrisoners() > 0 || CountUndeadPrisoners() > 0 || HereticHeroPrisoners().Count > 0;

        // Dialog-line conditions: set the display count variable, hide the line when empty.
        public static bool UpdateChaosLine()
        {
            int n = CountChaosPrisoners();
            MBTextManager.SetTextVariable("WHL_CHAOS_COUNT", n);
            return n > 0;
        }

        public static bool UpdateUndeadLine()
        {
            int n = CountUndeadPrisoners();
            MBTextManager.SetTextVariable("WHL_UNDEAD_COUNT", n);
            return n > 0;
        }

        public static bool UpdateHeroesLine()
        {
            int n = HereticHeroPrisoners().Count;
            MBTextManager.SetTextVariable("WHL_HERO_COUNT", n);
            return n > 0;
        }

        public static void TurnInChaos() =>
            TurnInTroops(x => !x.Character.IsHero && x.Character.IsChaos(),
                         ChaosGoldPerHead, ChaosPrestigePerHead);

        public static void TurnInUndead() =>
            TurnInTroops(x => !x.Character.IsHero && (x.Character.IsUndead() || x.Character.IsVampire()),
                         UndeadGoldPerHead, UndeadPrestigePerHead);

        public static void OpenHereticHeroSelection()
        {
            // Snapshot the inquisitor the player is currently speaking to — ShowMultiSelectionInquiry
            // runs after the dialog closes, so the OneToOne reference will be gone by then.
            var inquisitor = CharacterObject.OneToOneConversationCharacter?.HeroObject;

            var heroes = HereticHeroPrisoners();
            if (heroes.Count == 0) return;

            var elements = heroes.Select(h => new InquiryElement(
                h,
                h.Name.ToString(),
                new CharacterImageIdentifier(CampaignUIHelper.GetCharacterCode(h.CharacterObject)))).ToList();

            var title = TORTextHelper.GetText("tor_poi_witchhunter_lodge.heroes_popup_title",
                "Put on Trial");
            var desc = TORTextHelper.GetText("tor_poi_witchhunter_lodge.heroes_popup_desc",
                "Select which captives to hand to the Inquisition. They will not be seen again.");
            var accept = TORTextHelper.GetText("tor_poi_witchhunter_lodge.heroes_popup_accept", "Execute");
            var cancel = TORTextHelper.GetText("tor_poi_witchhunter_lodge.heroes_popup_cancel", "Cancel");

            var inq = new MultiSelectionInquiryData(
                title, desc, elements, true, 0, elements.Count, accept, cancel,
                picked => ExecuteSelected(picked, inquisitor), null);
            MBInformationManager.ShowMultiSelectionInquiry(inq, true);
        }

        // ---- internals

        private static IEnumerable<TaleWorlds.CampaignSystem.Roster.TroopRosterElement> PrisonerElements() =>
            MobileParty.MainParty?.PrisonRoster?.GetTroopRoster()
                ?? Enumerable.Empty<TaleWorlds.CampaignSystem.Roster.TroopRosterElement>();

        private static void TurnInTroops(
            System.Func<TaleWorlds.CampaignSystem.Roster.TroopRosterElement, bool> match,
            int goldPerHead, int prestigePerHead)
        {
            var matched = PrisonerElements().Where(match).ToList();
            int total = 0;
            foreach (var el in matched)
            {
                total += el.Number;
                MobileParty.MainParty.PrisonRoster.RemoveTroop(el.Character, el.Number);
            }
            if (total == 0) return;

            GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, total * goldPerHead);
            Hero.MainHero.AddCustomResource("Prestige", total * prestigePerHead);
        }

        private static void ExecuteSelected(List<InquiryElement> picked, Hero inquisitor)
        {
            if (picked == null || picked.Count == 0) return;

            int paid = 0;
            foreach (var el in picked)
            {
                var hero = el.Identifier as Hero;
                if (hero == null || !hero.IsAlive) continue;

                KillCharacterAction.ApplyByExecution(hero, Hero.MainHero, true);
                paid++;
            }
            if (paid == 0) return;

            GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, paid * HeroGoldPerHead);
            Hero.MainHero.AddCustomResource("Prestige", paid * HeroPrestigePerHead);
            if (inquisitor != null)
                ChangeRelationAction.ApplyPlayerRelation(inquisitor, paid * HeroInquisitorRelation);
        }
    }
}
