using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;

namespace TOR_Core.CampaignMechanics.MapNotifications
{
    public class TORMapNotification : InformationData
    {
        [SaveableField(1)]
        private string _notificationIdentifier;

        [SaveableField(2)]
        private string _soundEventPath;

        public string NotificationIdentifier => _notificationIdentifier;

        public override TextObject TitleText => DescriptionText;

        public override string SoundEventPath => _soundEventPath;

        public TORMapNotification(TextObject description, string notificationIdentifier, string soundEventPath = "")
            : base(description)
        {
            _notificationIdentifier = notificationIdentifier;
            _soundEventPath = soundEventPath;
        }
    }
}
