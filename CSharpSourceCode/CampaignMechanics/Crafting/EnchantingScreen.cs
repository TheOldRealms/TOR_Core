using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.View.Screens;
using TaleWorlds.ScreenSystem;
using TOR_Core.AbilitySystem.SpellBook;
using TaleWorlds.TwoDimension;

namespace TOR_Core.CampaignMechanics.Crafting
{
    [GameStateScreen(typeof(EnchantingState))]
    public class EnchantingScreen : ScreenBase, IGameStateListener
    {
        private GauntletLayer _gauntletLayer;
        private EnchantingState _state;
        private EnchantingVM _vm;
        private SpriteCategory _craftingSpriteCategory;

        public EnchantingScreen(EnchantingState state)
        {
            _state = state;
            _state.RegisterListener(this);
        }

        public static void Open()
        {
            var state = Game.Current.GameStateManager.CreateState<EnchantingState>();
            Game.Current.GameStateManager.PushState(state);
        }

        protected override void OnFrameTick(float dt)
        {
            base.OnFrameTick(dt);
            LoadingWindow.DisableGlobalLoadingWindow();
            if (_gauntletLayer.Input.IsHotKeyReleased("Exit"))
            {
                CloseScreen();
            }
        }

        private void CloseScreen()
        {
            Game.Current.GameStateManager.PopState(0);
        }

        void IGameStateListener.OnActivate()
        {
            base.OnActivate();
            SpriteData spriteData = UIResourceManager.SpriteData;
            TwoDimensionEngineResourceContext resourceContext = UIResourceManager.ResourceContext;
            ResourceDepot uiresourceDepot = UIResourceManager.ResourceDepot;
            _craftingSpriteCategory = spriteData.SpriteCategories["ui_crafting"];
            _craftingSpriteCategory.Load(resourceContext, uiresourceDepot);
            _vm = new EnchantingVM(CloseScreen);
            _gauntletLayer = new GauntletLayer(1, "GauntletLayer", true);
            _gauntletLayer.InputRestrictions.SetInputRestrictions(true, InputUsageMask.All);
            _gauntletLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericCampaignPanelsGameKeyCategory"));
            _gauntletLayer.LoadMovie("Enchanting", _vm);
            _gauntletLayer.IsFocusLayer = true;
            AddLayer(_gauntletLayer);
            ScreenManager.TrySetFocus(_gauntletLayer);
        }

        void IGameStateListener.OnDeactivate()
        {
            base.OnDeactivate();
            RemoveLayer(_gauntletLayer);
            _gauntletLayer.IsFocusLayer = false;
            ScreenManager.TryLoseFocus(_gauntletLayer);
        }

        void IGameStateListener.OnFinalize()
        {
            _gauntletLayer = null;
            _vm = null;
            _craftingSpriteCategory.Unload();
            _craftingSpriteCategory = null;
        }

        void IGameStateListener.OnInitialize()
        {
            base.OnInitialize();
        }
    }
}
