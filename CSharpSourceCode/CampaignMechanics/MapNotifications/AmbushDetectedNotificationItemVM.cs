using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ViewModelCollection.Map.MapNotificationTypes;
using TaleWorlds.Core;

namespace TOR_Core.CampaignMechanics.MapNotifications
{
    /// <summary>
    /// VM for <see cref="AmbushDetectedMapNotification"/>. When the
    /// player clicks the notification the campaign-map camera pans
    /// to the position where the stealthed party was spotted —
    /// same gesture vanilla uses for the hideout-spotted / army
    /// notifications. Position is pulled from the Data payload and
    /// delivered via the protected
    /// <c>MapNotificationItemBaseVM.FastMoveCameraToPosition</c>
    /// callback the base class wires at construction time.
    /// </summary>
    public class AmbushDetectedNotificationItemVM : MapNotificationItemBaseVM
    {
        public AmbushDetectedNotificationItemVM(InformationData data) : base(data)
        {
            _onInspect = PanCameraToDetectedPosition;
        }

        private void PanCameraToDetectedPosition()
        {
            if (Data is not AmbushDetectedMapNotification detected) return;
            FastMoveCameraToPosition?.Invoke(new CampaignVec2(detected.TargetPosition, true));
        }
    }
}
