using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignMechanics.RaiseDead
{
    public class PostBattleCampaignBehavior : CampaignBehaviorBase
    {
        private List<CharacterObject> _raiseableCharacters = new List<CharacterObject>();

        public override void RegisterEvents()
        {
            CampaignEvents.OnAfterSessionLaunchedEvent.AddNonSerializedListener(this, InitializeRaiseableCharacters);
            CampaignEvents.OnPlayerBattleEndEvent.AddNonSerializedListener(this, RaiseDead);                //Those events are never executed when the player lose a battle!
            CampaignEvents.OnPlayerBattleEndEvent.AddNonSerializedListener(this, CheckWarriorPriestPerks);
        }
        

        private void CheckWarriorPriestPerks(MapEvent mapEvent)
        {
            if (Hero.MainHero.HasCareerChoice("BookOfSigmarPassive3"))
            {
                var playerParty = mapEvent.PartiesOnSide(mapEvent.PlayerSide).FirstOrDefault(x => x.Party.LeaderHero == Hero.MainHero); //TODO Main party check might suffies
                if (playerParty == null) return;
                var heroes = playerParty.Party.MobileParty.GetMemberHeroes();
                foreach (var hero in heroes.Where(hero => hero.IsWounded))  //TODO not applied if heal is low
                {
                    hero.Heal(20,false);
                }
            }
        }


        private void RaiseDead(MapEvent mapEvent)
        {
            if (mapEvent.PlayerSide == mapEvent.WinningSide && Hero.MainHero.CanRaiseDead())
            {
                var troops = CalculateTroops(mapEvent);
                for (int i = 0; i < troops.Count; i++)
                {
                    PlayerEncounter.Current.RosterToReceiveLootMembers.AddToCounts(troops[i], 1);
                }
            }
        }

        private void InitializeRaiseableCharacters(CampaignGameStarter starter)
        {
            var characters = MBObjectManager.Instance.GetObjectTypeList<CharacterObject>();
            var basic = characters.Where(character => character.IsBasicTroop).ToList();
            foreach(var c in basic)
            {
                var info = c.GetAttributes();
            }
            _raiseableCharacters = characters.Where(character => character.IsUndead() && character.IsBasicTroop).ToList();
        }

        private List<CharacterObject> CalculateTroops(MapEvent mapEvent)
        {
            List<CharacterObject> elements = new List<CharacterObject>();
            var num = mapEvent.GetMapEventSide(mapEvent.DefeatedSide).Casualties;
            double raiseDeadChance = Hero.MainHero.GetRaiseDeadChance();
            for (int i = 0; i <= num; i++)
            {
                if(MBRandom.RandomFloat <= raiseDeadChance)
                {
                    elements.Add(_raiseableCharacters.GetRandomElement());
                }
            }
            return elements;
        }

        public override void SyncData(IDataStore dataStore)
        {
            //throw new NotImplementedException();
        }
    }
}
