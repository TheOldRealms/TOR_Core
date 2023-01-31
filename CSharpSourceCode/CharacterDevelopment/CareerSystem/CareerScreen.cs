using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.View.Screens;
using TaleWorlds.ScreenSystem;

namespace TOR_Core.CharacterDevelopment.CareerSystem
{
    [GameStateScreen(typeof(CareerScreenGameState))]
    public class CareerScreen : ScreenBase, IGameStateListener
    {
        private GauntletLayer _gauntletLayer;
        private CareerScreenVM _vm;
        private CareerScreenGameState _state;

        public CareerScreen(CareerScreenGameState state)
        {
            _state = state;
            _state.RegisterListener(this);
        }

        protected override void OnFrameTick(float dt)
        {
            base.OnFrameTick(dt);
            LoadingWindow.DisableGlobalLoadingWindow();
            if (_gauntletLayer.Input.IsHotKeyDownAndReleased("Exit") || _gauntletLayer.Input.IsGameKeyDownAndReleased(41))
            {
                CloseScreen();
            }
        }

        void IGameStateListener.OnActivate()
        {
            base.OnActivate();
            _vm = new CareerScreenVM(CloseScreen);
            _gauntletLayer = new GauntletLayer(1, "GauntletLayer", true);
            _gauntletLayer.InputRestrictions.SetInputRestrictions(true, InputUsageMask.All);
            _gauntletLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericCampaignPanelsGameKeyCategory"));
            _gauntletLayer.LoadMovie("CareerScreen", _vm);
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
        }

        void IGameStateListener.OnInitialize()
        {
            base.OnInitialize();
        }

        private void CloseScreen()
        {
            Game.Current.GameStateManager.PopState(0);
        }
    }
}
