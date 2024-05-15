using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.View.Screens;
using TaleWorlds.ScreenSystem;
using TOR_Core.AbilitySystem.SpellBook;

namespace TOR_Core.AbilitySystem.Spells.Prayers
{
    [GameStateScreen(typeof(BattlePrayerBookState))]
    public class BattlePrayerScreen : ScreenBase, IGameStateListener
    {
        private readonly BattlePrayerBookState _state;
        private GauntletLayer _gauntletLayer;
        private BattlePrayersVM _vm;

        public BattlePrayerScreen(BattlePrayerBookState state)
        {
            _state = state;
            _state.RegisterListener(this);
        }

        protected override void OnFrameTick(float dt)
        {
            LoadingWindow.DisableGlobalLoadingWindow();
            base.OnFrameTick(dt);
        }

        void IGameStateListener.OnActivate()
        {
            base.OnActivate();
            _vm = new BattlePrayersVM(CloseScreen);
            _gauntletLayer = new GauntletLayer(1, "GauntletLayer", true);
            _gauntletLayer.LoadMovie("BattlePrayerBook", _vm);
            _gauntletLayer.InputRestrictions.SetInputRestrictions(true, InputUsageMask.All);
            _gauntletLayer.Input.RegisterHotKeyCategory(
                HotKeyManager.GetCategory("GenericCampaignPanelsGameKeyCategory"));
            _gauntletLayer.IsFocusLayer = true;

            AddLayer(_gauntletLayer);
            ScreenManager.TrySetFocus(_gauntletLayer);
        }

        void IGameStateListener.OnDeactivate()
        {
            base.OnDeactivate();
        }

        void IGameStateListener.OnInitialize()
        {
            base.OnInitialize();
        }

        void IGameStateListener.OnFinalize()
        {
            base.OnFinalize();
        }

        private void CloseScreen()
        {
            Game.Current.GameStateManager.PopState(0);
        }
    }
}