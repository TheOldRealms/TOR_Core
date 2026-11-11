using TaleWorlds.CampaignSystem;

namespace TOR_Core.CampaignMechanics.Crafting
{
    /// <summary>
    /// Persists the two save-state flags <see cref="EnchanterHelper"/> owns:
    /// <c>SpokeToEnchanter</c> (player ever talked to any enchanter) and
    /// <c>LearnedEnchantment</c> (received the beginner-blueprint freebie).
    ///
    /// Lives on its own so the helper can stay a pure static utility —
    /// Bannerlord's save system needs a <see cref="CampaignBehaviorBase"/>
    /// instance to hook <see cref="SyncData"/>.
    /// </summary>
    public class EnchanterProgressBehavior : CampaignBehaviorBase
    {
        public override void RegisterEvents() { }

        public override void SyncData(IDataStore dataStore)
        {
            bool spoke = EnchanterHelper.SpokeToEnchanter;
            bool learned = EnchanterHelper.LearnedEnchantment;
            dataStore.SyncData("_enchanter_spokeToEnchanter", ref spoke);
            dataStore.SyncData("_enchanter_learnedEnchantment", ref learned);
            EnchanterHelper.SpokeToEnchanter = spoke;
            EnchanterHelper.LearnedEnchantment = learned;
        }
    }
}
