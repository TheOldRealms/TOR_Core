using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TOR_Core.Extensions;

namespace TOR_Core.AbilitySystem.SpellBook
{
    public class SpellItemVM : AbilityItemVM
    {
        private bool _isTrainerMode;
        private bool _canLearn = false;
        private string _learnText;
        private int _goldCost;

        public SpellItemVM(AbilityTemplate template, Hero currentHero, bool isTrainerMode = false) : base(template, currentHero)
        {
            _isTrainerMode = isTrainerMode;
            RefreshValues();
        }

        private void ExecuteLearnSpell()
        {
            // Deduct gold from the party leader if possible. Needed because
            // companions in a party do not actually own any gold.
            var sugarDaddy = Hero.IsPartyLeader ? Hero
                : Hero.PartyBelongedTo != null ? Hero.PartyBelongedTo.Owner
                    : Hero;
            if (sugarDaddy.Gold >= _goldCost)
            {
                sugarDaddy.ChangeHeroGold(-_goldCost);
                Hero.AddAbility(Template.StringID);
                var learnedSpellText = TORTextHelper.GetTextObject("tor_learned_spell_text", "Successfully learned spell: {SPELL_NAME}");
                learnedSpellText.SetTextVariable("SPELL_NAME", Template.Name);
                MBInformationManager.AddQuickInformation(learnedSpellText);
            }
            else
            {
                MBInformationManager.AddQuickInformation(TORTextHelper.GetTextObject("tor_not_enough_gold_text", "Not enough gold"));
            }
            RefreshValues();
        }

        protected override void ExecuteSelectAbility()
        {
            if (!_isTrainerMode)
            {
                base.ExecuteSelectAbility();
            }
            RefreshValues();
        }

        public override void RefreshValues()
        {
            base.RefreshValues();
            _goldCost = Template.GoldCost;
            var model = Campaign.Current.Models.GetAbilityModel();
            var info = Hero.GetExtendedInfo();
            if (model != null)
            {
                _goldCost = model.GetSpellGoldCostForHero(Hero, Template);
            }
            var learnTextObject = new TextObject("{=tor_learnSpell}Learn {GOLDCOST} {COINIMAGE}");
            MBTextManager.SetTextVariable("COINIMAGE", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"4\">", false);
            MBTextManager.SetTextVariable("GOLDCOST", _goldCost);
            LearnText = learnTextObject.ToString(); //this does result in a localized string in the UI - to be determined if this is actually how we want to localize that UI

            IsSelected = !_isTrainerMode && info.IsAbilitySelected(Template.StringID);
            if (IsDisabled)
            {
                CanLearn = _isTrainerMode && Template.SpellTier <= (int)info.SpellCastingLevel && Hero.HasKnownLore(Template.BelongsToLoreID);
                if (!info.KnownLores.Any(x => x.ID == Template.BelongsToLoreID))
                {
                    var disabledReasonTextObject = new TextObject("{=tor_learnSpellDisabled_lore}Unfamiliar lore");
                    DisabledReason = disabledReasonTextObject.ToString();
                }
                else if (Template.SpellTier > (int)info.SpellCastingLevel)
                {
                    var disabledReasonTextObject = new TextObject("{=tor_learnSpellDisabled_spellTier}Insufficient caster level");
                    DisabledReason = disabledReasonTextObject.ToString();
                }
                else
                {
                    var disabledReasonTextObject = new TextObject("{=tor_learnSpellDisabled_canLearn}Can learn");
                    DisabledReason = disabledReasonTextObject.ToString();
                }
            }
        }

        [DataSourceProperty]
        public bool CanLearn
        {
            get
            {
                return this._canLearn;
            }
            set
            {
                if (value != this._canLearn)
                {
                    this._canLearn = value;
                    base.OnPropertyChangedWithValue(value, "CanLearn");
                }
            }
        }

        [DataSourceProperty]
        public string LearnText
        {
            get
            {
                return this._learnText;
            }
            set
            {
                if (value != this._learnText)
                {
                    this._learnText = value;
                    base.OnPropertyChangedWithValue(value, "LearnText");
                }
            }
        }

    }
}