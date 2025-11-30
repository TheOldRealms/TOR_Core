using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Engine.Options;
using TaleWorlds.Engine.Screens;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.Screens;
using TaleWorlds.MountAndBlade.ViewModelCollection.GameOptions;
using TaleWorlds.MountAndBlade.ViewModelCollection.InitialMenu;
using TaleWorlds.ScreenSystem;

namespace TOR_Core.Extensions.UI
{
    [GameStateScreen(typeof(InitialState))]
    public class TORInitialScreen : ScreenBase, IGameStateListener
    {
        private InitialState _initialState;
        private InitialMenuVM _dataSource;
        private GauntletLayer _gauntletLayer;
        private GauntletLayer _gauntletBrightnessLayer;
        private GauntletLayer _gauntletExposureLayer;
        private BrightnessOptionVM _brightnessOptionDataSource;
        private ExposureOptionVM _exposureOptionDataSource;
        private GauntletMovieIdentifier _brightnessOptionMovie;
        private GauntletMovieIdentifier _exposureOptionMovie;
        private SceneLayer _scenelayer;
        private Camera _camera;
        private Scene _scene;
        private readonly List<string> _menuSceneNames = ["TOR_menuscene_01", "TOR_menuscene_02", "TOR_menuscene_03"];

        public TORInitialScreen(InitialState initialState)
        {
            _initialState = initialState; 
        }

        protected override void OnInitialize()
        {
            base.OnInitialize();
            Common.MemoryCleanupGC(false);
            if (Game.Current != null)
            {
                Game.Current.Destroy();
            }
            MBMusicManager.Initialize();

            _dataSource = new InitialMenuVM(_initialState);
            _gauntletLayer = new GauntletLayer("MainMenu", 1, false);
            _gauntletLayer.LoadMovie("InitialScreen", _dataSource);
            _gauntletLayer.InputRestrictions.SetInputRestrictions(true, InputUsageMask.Mouse);
            AddLayer(_gauntletLayer);
            _gauntletLayer.IsFocusLayer = true;
            ScreenManager.TrySetFocus(_gauntletLayer);

            SetupScene();

            _gauntletLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
            _scenelayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));

            if (NativeOptions.GetConfig(NativeOptions.NativeOptionsType.BrightnessCalibrated) < 4f)
            {
                _brightnessOptionDataSource = new BrightnessOptionVM(new Action<bool>(OnCloseBrightness))
                {
                    Visible = true
                };
                _gauntletBrightnessLayer = new GauntletLayer("MainMenuBrightness", 2, false);
                _gauntletBrightnessLayer.InputRestrictions.SetInputRestrictions(true, InputUsageMask.Mouse);
                _brightnessOptionMovie = _gauntletBrightnessLayer.LoadMovie("BrightnessOption", _brightnessOptionDataSource);
                AddLayer(_gauntletBrightnessLayer);
            }

            InformationManager.ClearAllMessages();
            SetGainNavigationAfterFrames(3);
        }

        private void SetupScene()
        {
            _scene = Scene.CreateNewScene(true, true, DecalAtlasGroup.All, "mono_renderscene");
            _scene.SetName("MainMenuScene");
            SceneInitializationData sceneInitializationData = new SceneInitializationData(true);
            _scene.Read(_menuSceneNames.GetRandomElementInefficiently(), ref sceneInitializationData);
            _scene.DisableStaticShadows(true);
            _scene.SetShadow(true);
            _scene.SetClothSimulationState(true);
            _scene.SetOcclusionMode(true);
            _scene.SetDynamicShadowmapCascadesRadiusMultiplier(0.1f);
            _scene.SetDoNotWaitForLoadingStatesToRender(false);
            _scene.PreloadForRendering();
            _scene.Tick(0f);

            _camera = Camera.CreateCamera();
            var cameraEntity = _scene.FindEntityWithTag("mainmenu_camera");
            Vec3 dofParams = default;
            cameraEntity.GetCameraParamsFromCameraScript(_camera, ref dofParams);
            float fovVertical = _camera.GetFovVertical();
            float aspectRatio = Screen.AspectRatio;
            float near = _camera.Near;
            float far = _camera.Far;
            _camera.SetFovVertical(fovVertical, aspectRatio, near, far);
            _camera.Frame = cameraEntity.GetGlobalFrame();

            _scenelayer = new SceneLayer(true, true);
            _scenelayer.InputRestrictions.SetInputRestrictions(true, InputUsageMask.Mouse);
            _scenelayer.IsFocusLayer = true;
            AddLayer(_scenelayer);
            _scenelayer.SceneView.SetScene(_scene);
            _scenelayer.SceneView.SetCamera(_camera);
            _scenelayer.SceneView.SetSceneUsesShadows(true);
            _scenelayer.SceneView.SetRenderWithPostfx(true);
            _scenelayer.SceneView.SetPostfxFromConfig();
            _scenelayer.SceneView.SetSceneUsesSkybox(true);
            _scenelayer.SceneView.SetResolutionScaling(true);
        }

        protected override void OnActivate()
        {
            base.OnActivate();
            if (TaleWorlds.Engine.Utilities.renderingActive && _scenelayer?.SceneView?.ReadyToRender() == true)
            {
                TaleWorlds.Engine.Utilities.DisableGlobalLoadingWindow();
            }
            if (NativeConfig.DoLocalizationCheckAtStartup)
            {
                LocalizedTextManager.CheckValidity(new List<string>());
            }
            Module.CurrentModule.SetCanLoadModules(true);

            if(_dataSource != null) _dataSource.RefreshMenuOptions();
            SetGainNavigationAfterFrames(3);
        }

        protected override void OnDeactivate()
        {
            base.OnDeactivate();
            Module.CurrentModule.SetCanLoadModules(false);
        }

        protected override void OnPause()
        {
            LoadingWindow.DisableGlobalLoadingWindow();
            base.OnPause();
        }

        protected override void OnFinalize()
        {
            base.OnFinalize();
            if (_gauntletLayer != null)
            {
                RemoveLayer(_gauntletLayer);
            }
            _gauntletLayer = null;
            _scene?.ManualInvalidate();
            _scenelayer?.SceneView?.ClearAll(true, true);
            if (_scenelayer != null)
            {
                RemoveLayer(_scenelayer);
            }
            _camera?.ReleaseCamera();
            _camera = null;
            _scenelayer = null;
            _scene = null;
            if (_dataSource != null)
            {
                _dataSource.OnFinalize();
            }
            _dataSource = null;
        }

        protected override void OnFrameTick(float dt)
        {
            base.OnFrameTick(dt);
            if(_scenelayer?.SceneView?.ReadyToRender() == true)
            {
                LoadingWindow.DisableGlobalLoadingWindow();
            }
            
            if (Input.IsKeyDown(InputKey.LeftControl) && Input.IsKeyReleased(InputKey.E))
            {
                MBInitialScreenBase.OnEditModeEnterPress();
            }
            if (ScreenManager.TopScreen == this)
            {
                HandleInput();
            }
            _scene.Tick(dt);
        }

        private void HandleInput()
        {
            if (ScreenManager.IsMouseCursorHidden())
            {
                MouseManager.ShowCursor(false);
                MouseManager.ShowCursor(true);
            }
            if (_gauntletLayer.Input.IsHotKeyReleased("Exit"))
            {
                if (_brightnessOptionDataSource != null && _brightnessOptionDataSource.Visible)
                {
                    UISoundsHelper.PlayUISound("event:/ui/default");
                    _brightnessOptionDataSource.ExecuteCancel();
                    return;
                }
                if (_exposureOptionDataSource != null && _exposureOptionDataSource.Visible)
                {
                    UISoundsHelper.PlayUISound("event:/ui/default");
                    _exposureOptionDataSource.ExecuteCancel();
                    return;
                }
            }
            else if (_gauntletLayer.Input.IsHotKeyReleased("Confirm"))
            {
                if (_brightnessOptionDataSource != null && _brightnessOptionDataSource.Visible)
                {
                    UISoundsHelper.PlayUISound("event:/ui/default");
                    _brightnessOptionDataSource.ExecuteConfirm();
                    return;
                }
                if (_exposureOptionDataSource != null && _exposureOptionDataSource.Visible)
                {
                    UISoundsHelper.PlayUISound("event:/ui/default");
                    _exposureOptionDataSource.ExecuteConfirm();
                }
            }
        }

        private void OnCloseBrightness(bool isConfirm)
        {
            _gauntletBrightnessLayer.ReleaseMovie(_brightnessOptionMovie);
            RemoveLayer(_gauntletBrightnessLayer);
            _brightnessOptionDataSource = null;
            _gauntletBrightnessLayer = null;
            NativeOptions.SaveConfig();
            OpenExposureControl();
        }

        private void OpenExposureControl()
        {
            _exposureOptionDataSource = new ExposureOptionVM(new Action<bool>(OnCloseExposureControl))
            {
                Visible = true
            };
            _gauntletExposureLayer = new GauntletLayer("MainMenuExposure", 2, false);
            _gauntletExposureLayer.InputRestrictions.SetInputRestrictions(true, InputUsageMask.Mouse);
            _exposureOptionMovie = _gauntletExposureLayer.LoadMovie("ExposureOption", _exposureOptionDataSource);
            AddLayer(_gauntletExposureLayer);
        }

        private void OnCloseExposureControl(bool isConfirm)
        {
            _gauntletExposureLayer.ReleaseMovie(_exposureOptionMovie);
            RemoveLayer(_gauntletExposureLayer);
            _exposureOptionDataSource = null;
            _gauntletExposureLayer = null;
            NativeOptions.SaveConfig();
        }

        private void SetGainNavigationAfterFrames(int frameCount)
        {
            _gauntletLayer.UIContext.GamepadNavigation.GainNavigationAfterFrames(frameCount, delegate
            {
                BrightnessOptionVM brightnessOptionDataSource = _brightnessOptionDataSource;
                if (brightnessOptionDataSource == null || !brightnessOptionDataSource.Visible)
                {
                    ExposureOptionVM exposureOptionDataSource = _exposureOptionDataSource;
                    return exposureOptionDataSource == null || !exposureOptionDataSource.Visible;
                }
                return false;
            });
        }

        void IGameStateListener.OnActivate() { }

        void IGameStateListener.OnDeactivate() { }

        void IGameStateListener.OnInitialize() { }

        void IGameStateListener.OnFinalize() { }
    }
}
