using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.SaveSystem;

namespace TOR_Core.CampaignMechanics.Assimilation
{
    public class AssimilationCampaignBehavior : CampaignBehaviorBase
    {
        private Dictionary<Settlement, CultureObject> _settlementCulturePairs = new Dictionary<Settlement, CultureObject>();

        public override void RegisterEvents()
        {
            CampaignEvents.OnSettlementOwnerChangedEvent.AddNonSerializedListener(this, SettlementOwnerChanged);
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
            CampaignEvents.OnNewGameCreatedPartialFollowUpEvent.AddNonSerializedListener(this, OnNewGameStart);
        }

        private void OnNewGameStart(CampaignGameStarter arg1, int arg2)
        {
            if(arg2 == 0)
            {
                foreach (var settlement in Settlement.All)
                {
                    _settlementCulturePairs.Add(settlement, settlement.Culture);
                }
            }
        }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            foreach(var settlement in _settlementCulturePairs.Keys)
            {
                CultureObject settlementCulture = _settlementCulturePairs[settlement];
                if (settlement.Culture != settlementCulture) settlement.Culture = settlementCulture;
            }
        }

        private void SettlementOwnerChanged(Settlement settlement, bool openToClaim, Hero newOwner, Hero oldOwner, Hero capturerHero, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail detail)
        {
            if(newOwner.MapFaction != null && oldOwner.MapFaction != null)
            {
                if (newOwner.MapFaction.Culture != settlement.Culture)
                {
                    settlement.Culture = newOwner.MapFaction.Culture;
                    foreach(var notable in settlement.Notables)
                    {
                        if (notable.Culture != settlement.Culture) notable.Culture = settlement.Culture;
                    }
                }
                if (_settlementCulturePairs.ContainsKey(settlement))
                {
                    _settlementCulturePairs[settlement] = settlement.Culture;
                }
                else
                {
                    _settlementCulturePairs.Add(settlement, settlement.Culture);
                }
            }
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("_settlementCulturePairs", ref _settlementCulturePairs);
        }
    }

    public class AssimilationSaveableTypeDefiner : SaveableTypeDefiner
    {
        public AssimilationSaveableTypeDefiner() : base(519011) { }

        protected override void DefineContainerDefinitions()
        {
            ConstructContainerDefinition(typeof(Dictionary<Settlement, CultureObject>));
        }
    }
}
