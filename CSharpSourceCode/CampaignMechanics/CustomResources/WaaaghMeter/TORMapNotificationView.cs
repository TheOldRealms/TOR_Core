using SandBox.View.Map;
using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ViewModelCollection.Map;
using TaleWorlds.CampaignSystem.ViewModelCollection.Map.MapNotificationTypes;
using TaleWorlds.Engine;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.ScreenSystem;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignMechanics.WaaaghMeter
{
    [OverrideView(typeof(MapNotificationView))]
    public class TORMapNotificationView : MapNotificationView
    {
        private const string DefaultSound = "event:/ui/default";
        private const string VanillaMovieName = "MapNotificationUI";
        private const string GreenskinMovieName = "TORGreenskinMapNotificationUI";

        private MapNotificationVM _dataSource;
        private GauntletMovieIdentifier _movie;
        private INavigationHandler _mapNavigationHandler;
        private GauntletLayer _layerAsGauntletLayer;
        private bool _isHoveringOnNotification;

        protected override void CreateLayout()
        {
            base.CreateLayout();

            _mapNavigationHandler = MapScreen.NavigationHandler;
            _dataSource = new MapNotificationVM(_mapNavigationHandler, MapScreen.FastMoveCameraToPosition);
            _dataSource.ReceiveNewNotification += OnReceiveNewNotification;
            _dataSource.SetRemoveInputKey(
                HotKeyManager.GetCategory("MapNotificationHotKeyCategory").GetHotKey("RemoveNotification"));

            Layer = new GauntletLayer("MapNotification", 100);
            _layerAsGauntletLayer = Layer as GauntletLayer;

            Layer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("MapNotificationHotKeyCategory"));
            Layer.InputRestrictions.SetInputRestrictions(isMouseVisible: false);

            bool isGreenskinPlayer = Hero.MainHero?.Culture?.StringId == TORConstants.Cultures.GREENSKIN;
            string movieName = isGreenskinPlayer ? GreenskinMovieName : VanillaMovieName;

            _movie = _layerAsGauntletLayer.LoadMovie(movieName, _dataSource);
            MapScreen.AddLayer(Layer);
        }

        public override void RegisterMapNotificationType(Type data, Type item)
        {
            _dataSource.RegisterMapNotificationType(data, item);
        }

        public override void ResetNotifications()
        {
            base.ResetNotifications();
            _dataSource?.RemoveAllNotifications();
        }

        protected override void OnFrameTick(float dt)
        {
            base.OnFrameTick(dt);
            _dataSource.OnFrameTick(dt);
            HandleInput();
        }

        protected override void OnMenuModeTick(float dt)
        {
            base.OnMenuModeTick(dt);
            _dataSource.OnMenuModeTick(dt);
            HandleInput();
        }

        protected override void OnMapConversationStart()
        {
            base.OnMapConversationStart();

            if (_layerAsGauntletLayer != null)
            {
                ScreenManager.SetSuspendLayer(_layerAsGauntletLayer, true);
            }
        }

        protected override void OnMapConversationOver()
        {
            base.OnMapConversationOver();

            if (_layerAsGauntletLayer != null)
            {
                ScreenManager.SetSuspendLayer(_layerAsGauntletLayer, false);
            }
        }

        protected override void OnFinalize()
        {
            base.OnFinalize();
            _dataSource?.OnFinalize();
        }

        private void OnReceiveNewNotification(MapNotificationItemBaseVM newNotification)
        {
            if (!string.IsNullOrEmpty(newNotification.SoundId))
            {
                SoundEvent.PlaySound2D(newNotification.SoundId);
            }
        }

        private void HandleInput()
        {
            if (!_isHoveringOnNotification && _dataSource.FocusedNotificationItem != null)
            {
                _isHoveringOnNotification = true;
                Layer.IsFocusLayer = true;
                ScreenManager.TrySetFocus(Layer);
            }
            else if (_isHoveringOnNotification && _dataSource.FocusedNotificationItem == null)
            {
                _isHoveringOnNotification = false;
                Layer.IsFocusLayer = false;
                ScreenManager.TryLoseFocus(Layer);
            }

            if (_isHoveringOnNotification
                && _dataSource.FocusedNotificationItem != null
                && Layer.Input.IsHotKeyReleased("RemoveNotification")
                && !_dataSource.FocusedNotificationItem.ForceInspection)
            {
                SoundEvent.PlaySound2D(DefaultSound);
                _dataSource.FocusedNotificationItem.ExecuteRemove();
            }
        }
    }
}