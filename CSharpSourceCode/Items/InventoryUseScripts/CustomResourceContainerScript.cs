using Helpers;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TOR_Core.CampaignMechanics.CustomResources;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.Items.InventoryUseScripts;

public class CustomResourceContainerScript : BaseInventoryUseScript
{
    public CustomResourceContainerScript(string[] arguments) : base(arguments)
    {
        switch (arguments.Length)
        {
            case < 2:
                throw new TORUseScriptArgumentException($"Invalid Script argument setup. Requires at least 2 argument.");
            case >= 2:
                {
                    if (!int.TryParse(arguments[0], out _))
                    {
                        throw new TORUseScriptArgumentException($"Amount is not of type int");
                    }

                    if (!CustomResourceManager.DoesResourceObjectExist(arguments[1]))
                    {
                        throw new TORUseScriptArgumentException($"Custom Resource does not exist");
                    }

                    break;
                }
        }
    }

    public override void OnUse(MobileParty userParty, ItemObject item)
    {
        var trait = item.GetTraits().FirstOrDefault(x => x.OnInventoryUseScript.InventoryScriptName == this.GetType().FullName);
        if (trait == null)
            return;

        var arguments = trait.OnInventoryUseScript.InventoryScriptArguments;

        if (!int.TryParse(arguments[0], out var amount))
        {
            throw new TORUseScriptArgumentException($"Amount is not of type int");
        }

        var cr = arguments[1];

        if (Hero.MainHero.GetCultureSpecificCustomResourceValue() + amount > TORConfig.MaximumCustomResourceValue)
        {
            //TODO  maybe some text to tell the player overshoots limit and the operation was therefore not applied.
            return;
        }



        userParty.ItemRoster.AddToCounts(item, -1);
        InventoryScreenHelper.CloseScreen(true);
        InventoryScreenHelper.OpenScreenAsInventory();
        userParty.LeaderHero.AddCustomResource(cr, amount);
    }
}