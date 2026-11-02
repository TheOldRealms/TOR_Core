using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;

namespace TOR_Core.CampaignMechanics.MapNotifications
{
    /// <summary>
    /// Specialized <see cref="TORMapNotification"/> for the player's
    /// ambush detection pass. Carries the <see cref="Vec2"/> position
    /// where the stealthed AI party was spotted so the matching VM
    /// (<see cref="AmbushDetectedNotificationItemVM"/>) can pan the
    /// campaign-map camera to that spot on click.
    ///
    /// Position only — not a party reference. The party may have moved
    /// (or re-stealthed, or been destroyed) by the time the player
    /// clicks; what matters is "where I last saw them." Mirrors the
    /// pattern vanilla uses for hideout-spotted notifications.
    /// </summary>
    public class AmbushDetectedMapNotification : TORMapNotification
    {
        [SaveableField(10)]
        private Vec2 _targetPosition;

        public Vec2 TargetPosition => _targetPosition;

        public AmbushDetectedMapNotification(TextObject description,
            string notificationIdentifier, string soundEventPath, Vec2 targetPosition)
            : base(description, notificationIdentifier, soundEventPath)
        {
            _targetPosition = targetPosition;
        }
    }
}
