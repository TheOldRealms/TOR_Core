using System;
using SandBox.GauntletUI;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.ViewModelCollection.WeaponCrafting;
using TaleWorlds.CampaignSystem.ViewModelCollection.WeaponCrafting.Refinement;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.View;
using TOR_Core.Extensions;

namespace TOR_Core.Extensions.UI
{
    [ViewModelExtension(typeof(RefinementVM))]
    public class RefinementVMExtension : BaseViewModelExtension
    {
        private readonly ICraftingCampaignBehavior _craftingBehavior;
        private Func<CraftingAvailableHeroItemVM> _getCurrentCraftingHeroVM;
        private Action _onRefinementSelectionChange;
        private int _maxRefinementCount;
        private bool _canRefineAll;
        private string _refineAllText;

        public RefinementVMExtension(ViewModel vm) : base(vm)
        {
            _craftingBehavior = Campaign.Current.GetCampaignBehavior<ICraftingCampaignBehavior>();
            // Note: EnsureLazyFieldsInitialized() will retry if _getCurrentCraftingHeroVM is null
            // (which it will be here, since RefinementVM's constructor body hasn't run yet)
            RefreshValues();
        }

        private void EnsureLazyFieldsInitialized()
        {
            if (_getCurrentCraftingHeroVM != null)
                return;

            var refinementVM = (RefinementVM)_vm;

            // Get the _getCurrentHero func from the RefinementVM via reflection
            var heroField = typeof(RefinementVM).GetField("_getCurrentHero", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (heroField != null)
            {
                _getCurrentCraftingHeroVM = heroField.GetValue(refinementVM) as Func<CraftingAvailableHeroItemVM>;
            }

            // Get the _onRefinementSelectionChange callback to trigger parent UI refresh
            var callbackField = typeof(RefinementVM).GetField("_onRefinementSelectionChange", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (callbackField != null)
            {
                _onRefinementSelectionChange = callbackField.GetValue(refinementVM) as Action;
            }
        }

        private Hero GetCurrentHero()
        {
            EnsureLazyFieldsInitialized();
            return _getCurrentCraftingHeroVM?.Invoke()?.Hero ?? Hero.MainHero;
        }

        public override void RefreshValues()
        {
            UpdateRefineAllState();
        }

        private void UpdateRefineAllState()
        {
            var refinementVM = (RefinementVM)_vm;
            var currentAction = refinementVM.CurrentSelectedAction;
            var hero = GetCurrentHero();

            if (currentAction == null || hero == null || !currentAction.IsEnabled)
            {
                MaxRefinementCount = 0;
                CanRefineAll = false;
                RefineAllText = TORTextHelper.GetText("tor_refine_all_text", "Refine All");
                NotifyParentPropertiesChanged();
                return;
            }

            int maxCount = CalculateMaxRefinements(currentAction, hero);
            MaxRefinementCount = maxCount;
            CanRefineAll = maxCount > 1;
            RefineAllText = maxCount > 1
                ? $"{TORTextHelper.GetText("tor_refine_all_text", "Refine All")} ({maxCount})"
                : TORTextHelper.GetText("tor_refine_all_text", "Refine All");

            NotifyParentPropertiesChanged();
        }

        private void NotifyParentPropertiesChanged()
        {
            CraftingVMExtension.CurrentInstance?.NotifyRefineAllPropertiesChanged();
        }

        private int CalculateMaxRefinements(RefinementActionItemVM action, Hero hero)
        {
            if (action == null || hero == null)
                return 0;

            var formula = action.RefineFormula;
            var itemRoster = MobileParty.MainParty.ItemRoster;
            var smithingModel = Campaign.Current.Models.SmithingModel;

            // Calculate max by stamina
            int currentStamina = _craftingBehavior.GetHeroCraftingStamina(hero);
            int energyCost = smithingModel.GetEnergyCostForRefining(ref formula, hero);
            int maxByStamina = energyCost > 0 ? currentStamina / energyCost : 0;

            // Calculate max by materials
            int maxByMaterials = int.MaxValue;

            if (formula.Input1Count > 0)
            {
                var input1Item = smithingModel.GetCraftingMaterialItem(formula.Input1);
                int available1 = itemRoster.GetItemNumber(input1Item);
                maxByMaterials = Math.Min(maxByMaterials, available1 / formula.Input1Count);
            }

            if (formula.Input2Count > 0)
            {
                var input2Item = smithingModel.GetCraftingMaterialItem(formula.Input2);
                int available2 = itemRoster.GetItemNumber(input2Item);
                maxByMaterials = Math.Min(maxByMaterials, available2 / formula.Input2Count);
            }

            // Return the minimum of both constraints
            return Math.Min(maxByStamina, maxByMaterials);
        }

        /// <summary>
        /// Public method for CraftingVMExtension to call.
        /// </summary>
        public void ExecuteRefineAllFromParent()
        {
            ExecuteRefineAll();
        }

        private void ExecuteRefineAll()
        {
            EnsureLazyFieldsInitialized();

            var refinementVM = (RefinementVM)_vm;
            var currentAction = refinementVM.CurrentSelectedAction;
            var hero = GetCurrentHero();

            if (currentAction == null || hero == null || !CanRefineAll)
                return;

            int count = MaxRefinementCount;

            for (int i = 0; i < count; i++)
            {
                // Re-check if we can still refine (stamina and materials)
                if (refinementVM.CurrentSelectedAction == null || !refinementVM.CurrentSelectedAction.IsEnabled)
                    break;

                int currentStamina = _craftingBehavior.GetHeroCraftingStamina(hero);
                var formula = refinementVM.CurrentSelectedAction.RefineFormula;
                int energyCost = Campaign.Current.Models.SmithingModel.GetEnergyCostForRefining(ref formula, hero);

                if (currentStamina < energyCost)
                    break;

                // Use the native method which handles stamina, materials, and UI refresh
                refinementVM.ExecuteSelectedRefinement(hero);
            }

            // Play refinement success sound
            UISoundsHelper.PlayUISound("event:/ui/crafting/refine_success");

            // Trigger parent CraftingVM callbacks
            _onRefinementSelectionChange?.Invoke();

            // Refresh stamina bar on the hero VM
            _getCurrentCraftingHeroVM?.Invoke()?.RefreshStamina();

            // Trigger full UI update via CraftingVM
            TriggerCraftingVMUpdate();

            UpdateRefineAllState();
        }

        private void TriggerCraftingVMUpdate()
        {
            // Use the CraftingVMExtension to trigger UpdateAll on the parent CraftingVM
            CraftingVMExtension.CurrentInstance?.TriggerUpdateAll();
        }

        [DataSourceProperty]
        public int MaxRefinementCount
        {
            get => _maxRefinementCount;
            set
            {
                if (_maxRefinementCount != value)
                {
                    _maxRefinementCount = value;
                    _vm.OnPropertyChanged(nameof(MaxRefinementCount));
                }
            }
        }

        [DataSourceProperty]
        public bool CanRefineAll
        {
            get => _canRefineAll;
            set
            {
                if (_canRefineAll != value)
                {
                    _canRefineAll = value;
                    _vm.OnPropertyChanged(nameof(CanRefineAll));
                }
            }
        }

        [DataSourceProperty]
        public string RefineAllText
        {
            get => _refineAllText;
            set
            {
                if (_refineAllText != value)
                {
                    _refineAllText = value;
                    _vm.OnPropertyChanged(nameof(RefineAllText));
                }
            }
        }
    }
}
