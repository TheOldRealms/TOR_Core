using System.Collections.Generic;

namespace TOR_Core.CampaignMechanics.SettlementPOIs
{
    public interface IEnchanterHost
    {
        string EnchanterTemplateId { get; }
        List<string> EnchantmentSuffixes { get; }
    }
}
