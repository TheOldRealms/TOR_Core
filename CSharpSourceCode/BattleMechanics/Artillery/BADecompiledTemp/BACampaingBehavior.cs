using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.CampaignSystem.Settlements.Workshops;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace TOR_Core.BattleMechanics.Artillery.BA
{
   public class BACampaignBehavior : CampaignBehaviorBase
  {
    private int _minTier;
    private string _cmSettlement;
    private ItemObject _cannonItem;
    private CharacterObject _cannonMaster;

    // public BACampaignBehavior(ConfigParam cfp)
    // {
    //   this._minTier = cfp.MinTier;
    //   this._cmSettlement = cfp.CMSettlement;
    // }

    public override void RegisterEvents()
    {
      CampaignEvents.LocationCharactersAreReadyToSpawnEvent.AddNonSerializedListener((object) this, new Action<Dictionary<string, int>>(this.LocationCharactersAreReadyToSpawn));
      CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener((object) this, new Action<CampaignGameStarter>(this.OnSessionLaunched));
    }

    private void DailyTick(Town town)
    {
    }

    private void LocationCharactersAreReadyToSpawn(Dictionary<string, int> unusedUsablePointCount)
    {
      Settlement settlement = PlayerEncounter.LocationEncounter.Settlement;
      if (!settlement.IsTown || CampaignMission.Current == null)
        return;
      string str = settlement.Name.ToString();
      Location location = CampaignMission.Current.Location;
      if (location == null || !(location.StringId == "tavern") || !(str == this._cmSettlement))
        return;
      LocationCharacter cannonMaster = BACampaignBehavior.CreateCannonMaster(settlement.Culture, LocationCharacter.CharacterRelations.Neutral);
      location.AddCharacter(cannonMaster);
    }

    private static LocationCharacter CreateCannonMaster(
      CultureObject culture,
      LocationCharacter.CharacterRelations relation)
    {
      CharacterObject characterObject = MBObjectManager.Instance.GetObject<CharacterObject>("munir_cm");
      int minimumAge;
      int maximumAge;
      Campaign.Current.Models.AgeModel.GetAgeLimitForLocation(characterObject, out minimumAge, out maximumAge);
      Monster monsterWithSuffix = FaceGen.GetMonsterWithSuffix(characterObject.Race, "_settlement");
      AgentData agentData = new AgentData((IAgentOriginBase) new SimpleAgentOrigin((BasicCharacterObject) characterObject)).Monster(monsterWithSuffix).Age(MBRandom.RandomInt(minimumAge, maximumAge));
      return new LocationCharacter(agentData, new LocationCharacter.AddBehaviorsDelegate(SandBoxManager.Instance.AgentBehaviorManager.AddWandererBehaviors), "sp_tavern_townsman", true, relation, (string) null, true)
      {
        PrefabNamesForBones = {
          {
            agentData.AgentMonster.OffHandItemBoneIndex,
            "kitchen_pitcher_b_tavern"
          }
        }
      };
    }

    private void OnSessionLaunched(CampaignGameStarter starter)
    {
      this._cannonItem = MBObjectManager.Instance.GetObject<ItemObject>("bt_cannon");
      this._cannonMaster = MBObjectManager.Instance.GetObject<CharacterObject>("munir_cm");
      this.AddDialogs(starter);
    }

    private void AddDialogs(CampaignGameStarter starter)
    {
      starter.AddDialogLine("artisan_brewer_talk", "start", "artisan_brewer", "As-Salaam-Alaikum, what brings you here?", (ConversationSentence.OnConditionDelegate) (() => CharacterObject.OneToOneConversationCharacter == this._cannonMaster), (ConversationSentence.OnConsequenceDelegate) null);
      starter.AddPlayerLine("artisan_brewer_buy", "artisan_brewer", "artisan_brewer_purchaced", "Good sir, I seek your skill, To craft for me, weapons that can kill. I have need for cannons, And only you can answer my call.", (ConversationSentence.OnConditionDelegate) null, (ConversationSentence.OnConsequenceDelegate) (() => Hero.MainHero.ChangeHeroGold(-200)), clickableConditionDelegate: (ConversationSentence.OnClickableConditionDelegate) ((out TextObject explanation) =>
      {
        if (Clan.PlayerClan.Tier < this._minTier)
        {
          explanation = new TextObject("Your clan is not famous enough.");
          return false;
        }
        explanation = TextObject.Empty;
        return true;
      }));
      starter.AddDialogLine("artisan_brewer_buy_cannon_ask", "artisan_brewer_purchaced", "artisan_brewer_cannon_ask", "The price I ask for, Is fifty thousand denars or more. For each cannon that I'll create, This is my price, it is not up for debate.", (ConversationSentence.OnConditionDelegate) null, (ConversationSentence.OnConsequenceDelegate) null);
      starter.AddPlayerLine("artisan_brewer_buy_cannon_bought", "artisan_brewer_cannon_ask", "artisan_brewer_cannon_purchase", "Excellent, I'll pay the price, without a quirk. For these cannons, that you shall make, Will make my enemies tremble and shake.", (ConversationSentence.OnConditionDelegate) null, (ConversationSentence.OnConsequenceDelegate) (() =>
      {
        Hero.MainHero.ChangeHeroGold(-50000);
        MobileParty.MainParty.ItemRoster.AddToCounts(this._cannonItem, 1);
      }), clickableConditionDelegate: (ConversationSentence.OnClickableConditionDelegate) ((out TextObject explanation) =>
      {
        if (Hero.MainHero.Gold < 50000)
        {
          explanation = new TextObject("You do not have enough denars.");
          return false;
        }
        explanation = TextObject.Empty;
        return true;
      }));
      starter.AddDialogLine("artisan_brewer_buy_cannon_bought", "artisan_brewer_cannon_purchase", "end", "It was a pleasure meeting you. May your army be victorious in all its endeavors.", (ConversationSentence.OnConditionDelegate) null, (ConversationSentence.OnConsequenceDelegate) null);
      starter.AddPlayerLine("artisan_brewer_buy_refuse", "artisan_brewer", "artisan_brewer_declined", "Leave.", (ConversationSentence.OnConditionDelegate) null, (ConversationSentence.OnConsequenceDelegate) null);
      starter.AddDialogLine("artisan_brewer_your_loss", "artisan_brewer_declined", "end", "I am but a humble man, A craftman, that is all I am. I cannot give you what you desire, Without knowing the purpose of your ire..", (ConversationSentence.OnConditionDelegate) null, (ConversationSentence.OnConsequenceDelegate) null);
      starter.AddPlayerLine("artisan_brewer_buy_cannon_refuse", "artisan_brewer_cannon_ask", "artisan_brewer_cannon_declined", "One hundred thousand denars, is it? That is a hefty price, for your wit..", (ConversationSentence.OnConditionDelegate) null, (ConversationSentence.OnConsequenceDelegate) null);
      starter.AddDialogLine("artisan_brewer_your_cannon_loss", "artisan_brewer_cannon_declined", "end", "I am but a humble man, A craftman, that is all I am. I cannot give you what you desire, Without knowing the purpose of your ire..", (ConversationSentence.OnConditionDelegate) null, (ConversationSentence.OnConsequenceDelegate) null);
    }

    private void OnWorkshopChangedEvent(Workshop workshop, Hero oldOwningHero, WorkshopType type)
    {
    }

    public override void SyncData(IDataStore dataStore)
    {
    }
  }
}
