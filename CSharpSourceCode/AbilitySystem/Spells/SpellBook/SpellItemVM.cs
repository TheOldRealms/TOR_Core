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

        public SpellItemVM(AbilityTemplate template, Hero currentHero, bool isTrainerMode = false) : base(template,currentHero)
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
            if(sugarDaddy.Gold >= _goldCost)
            {
                sugarDaddy.ChangeHeroGold(-_goldCost);
                Hero.AddAbility(Template.StringID);
                MBInformationManager.AddQuickInformation(new TextObject("Successfully learned spell: " + Template.Name));
            }
            else
            {
                MBInformationManager.AddQuickInformation(new TextObject("Not enough gold"));
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
            LearnText = "Learn " + _goldCost + "<img src=\"General\\Icons\\Coin@2x\"/>";
            
            IsSelected = !_isTrainerMode && info.IsAbilitySelected(Template.StringID);
            if (IsDisabled)
            {
                CanLearn = _isTrainerMode && Template.SpellTier <= (int)info.SpellCastingLevel && Hero.HasKnownLore(Template.BelongsToLoreID);
                if (!info.KnownLores.Any(x=>x.ID == Template.BelongsToLoreID))
                {
                    DisabledReason = "Unfamiliar lore";
                }
                else if(Template.SpellTier > (int)info.SpellCastingLevel)
                {
                    DisabledReason = "Insufficient caster level";
                }
                else
                {
                    DisabledReason = "Can learn";
                    CanLearn = _isTrainerMode && Template.SpellTier <= (int)info.SpellCastingLevel && Hero.HasKnownLore(Template.BelongsToLoreID);
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
