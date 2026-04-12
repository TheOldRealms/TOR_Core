using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TOR_Core.CampaignMechanics.Religion;

namespace TOR_Core.Utilities
{
    public class TORCampaignEvents
    {
        public static TORCampaignEvents Instance;
        public event EventHandler<HeroExtendedInfoCreatedEventArgs> HeroExtendedInfoCreated;
        public event EventHandler<DevotionLevelChangedEventArgs> DevotionLevelChanged;
        public event EventHandler<ChaosUprisingStartedEventArgs> ChaosUprisingStarted;
        public event EventHandler<ItemDuplicatedEventArgs> ItemDuplicated;

        public event EventHandler<EnchantmentLearnedEventArgs> EnchantmentLearned;

        public event EventHandler<AbilitylearnedEventArgs> AbilityLearned;

        public event EventHandler<OathLevelChangedEventArgs> OathLevelChanged;

        public event EventHandler<OnUseInventoryScriptActivatedEventArgs> OnUseInventoryScriptActivated;

        public event EventHandler<BrawlWonEventArgs> BrawlWon;

        public event EventHandler<TournamentWonEventArgs> TournamentWon;

        public event EventHandler<TeefTransferredEventArgs> TeefTransferred;

        public event EventHandler<LordDuelWonEventArgs> LordDuelWon;

        public event EventHandler<ShrinePrayerEventArgs> ShrinePrayer;

        public event EventHandler<ShrineLootedEventArgs> ShrineLooted;

        public event EventHandler<PracticeFightWonEventArgs> PracticeFightWon;

        public TORCampaignEvents()
        {
            Instance = this;
        }

        public void OnHeroExtendedInfoCreated(Hero hero)
        {
            var args = new HeroExtendedInfoCreatedEventArgs(hero);
            var heroInfoEvent = HeroExtendedInfoCreated;
            if (heroInfoEvent != null)
            {
                heroInfoEvent(this, args);
            }
        }

        public void OnDevotionLevelChanged(Hero hero, ReligionObject religion, DevotionLevel oldDevotionLevel, DevotionLevel newDevotionLevel)
        {
            var args = new DevotionLevelChangedEventArgs(hero, religion, oldDevotionLevel, newDevotionLevel);
            var devotionEvent = DevotionLevelChanged;
            if (devotionEvent != null)
            {
                devotionEvent(this, args);
            }
        }

        public void OnChaosUprisingStarted(Settlement settlement)
        {
            var args = new ChaosUprisingStartedEventArgs(settlement);
            var uprisingEvent = ChaosUprisingStarted;
            if (uprisingEvent != null)
            {
                uprisingEvent(this, args);
            }
        }

        public void OnItemDuplicated(ItemObject newItem, ItemObject oldItem, List<string> traits)
        {
            var args = new ItemDuplicatedEventArgs(newItem, oldItem, traits);
            var itemDuplicatedEvent = ItemDuplicated;
            if (itemDuplicatedEvent != null)
            {
                itemDuplicatedEvent(this, args);
            }
        }

        public void OnEnchantmentLearned(Hero hero, string blueprintId)
        {
            var args = new EnchantmentLearnedEventArgs(hero, blueprintId);
            var enchantmentLearnedEvent = EnchantmentLearned;
            if (enchantmentLearnedEvent != null)
            {
                enchantmentLearnedEvent(this, args);
            }
        }

        public void OnAbilityLearned(Hero hero, string blueprintId)
        {
            var args = new AbilitylearnedEventArgs(hero, blueprintId);
            var abilityLearned = AbilityLearned;
            if (abilityLearned != null)
            {
                abilityLearned(this, args);
            }
        }

        public void OnGuildOathLevelChanged(int newValue, string guild)
        {
            var args = new OathLevelChangedEventArgs(newValue, guild);
            var oathLevelChanged = OathLevelChanged;
            if (oathLevelChanged != null)
            {
                oathLevelChanged(this, args);
            }
        }


        public void OnUseInventoryUseScriptObject(ItemObject item, string scriptType, MobileParty party, string[] arguments)
        {
            var args = new OnUseInventoryScriptActivatedEventArgs(item, scriptType, party, arguments);
            var OnuseInventoryScript = OnUseInventoryScriptActivated;
            if (OnuseInventoryScript != null)
            {
                OnuseInventoryScript(this, args);
            }
        }

        public void OnBrawlWon(Hero hero)
        {
            var args = new BrawlWonEventArgs(hero);
            var brawlWon = BrawlWon;
            if (brawlWon != null)
            {
                brawlWon(this, args);
            }
        }

        public void OnTournamentWon(Hero hero)
        {
            var args = new TournamentWonEventArgs(hero);
            var arenaFightWon = TournamentWon;
            if (arenaFightWon != null)
            {
                arenaFightWon(this, args);
            }
        }

        public void OnTeefTransferred(Hero hero, int amount)
        {
            var args = new TeefTransferredEventArgs(hero, amount);
            var teefTransferred = TeefTransferred;
            if (teefTransferred != null)
            {
                teefTransferred(this, args);
            }
        }

        public void OnLordDuelWon(Hero hero, Hero opponent)
        {
            var args = new LordDuelWonEventArgs(hero, opponent);
            var lordDuelWon = LordDuelWon;
            if (lordDuelWon != null)
            {
                lordDuelWon(this, args);
            }
        }

        public void OnShrinePrayer(Hero hero, ReligionObject religion, Settlement shrine)
        {
            var args = new ShrinePrayerEventArgs(hero, religion, shrine);
            var shrinePrayer = ShrinePrayer;
            if (shrinePrayer != null)
            {
                shrinePrayer(this, args);
            }
        }

        public void OnShrineLooted(Hero hero, ReligionObject religion, Settlement shrine)
        {
            var args = new ShrineLootedEventArgs(hero, religion, shrine);
            var shrineLooted = ShrineLooted;
            if (shrineLooted != null)
            {
                shrineLooted(this, args);
            }
        }

        public void OnPracticeFightWon(Hero hero, int opponentsDefeated)
        {
            var args = new PracticeFightWonEventArgs(hero, opponentsDefeated);
            var practiceFightWon = PracticeFightWon;
            if (practiceFightWon != null)
            {
                practiceFightWon(this, args);
            }
        }
    }

    public class HeroExtendedInfoCreatedEventArgs(Hero hero) : EventArgs
    {
        public Hero Hero { get; private set; } = hero;
    }

    public class OnUseInventoryScriptActivatedEventArgs(ItemObject item, string scriptType, MobileParty user, string[] arguments) : EventArgs
    {
        public ItemObject Item { get; private set; } = item;
        public MobileParty Party { get; private set; } = user;

        public string ScriptType { get; private set; } = scriptType;
        public string[] Arguments { get; private set; } = arguments;
    }

    public class OathLevelChangedEventArgs(int newValue, string guild) : EventArgs
    {
        public int NewValue { get; private set; } = newValue;
        public string Guild { get; private set; } = guild;
    }

    public class AbilitylearnedEventArgs(Hero hero, string abilityId) : EventArgs
    {
        public Hero Hero { get; set; } = hero;
        public string AbilityId { get; set; } = abilityId;
    }

    public class DevotionLevelChangedEventArgs : EventArgs
    {
        public DevotionLevelChangedEventArgs(Hero hero, ReligionObject religion, DevotionLevel oldDevotionLevel, DevotionLevel newDevotionLevel)
        {
            Hero = hero;
            Religion = religion;
            OldDevotionLevel = oldDevotionLevel;
            NewDevotionLevel = newDevotionLevel;
        }

        public Hero Hero { get; set; }
        public ReligionObject Religion { get; set; }
        public DevotionLevel OldDevotionLevel { get; set; }
        public DevotionLevel NewDevotionLevel { get; set; }
    }

    public class ChaosUprisingStartedEventArgs : EventArgs
    {
        public ChaosUprisingStartedEventArgs(Settlement settlement)
        {
            Settlement = settlement;
        }

        public Settlement Settlement { get; set; }
    }

    public class ItemDuplicatedEventArgs : EventArgs
    {
        public ItemDuplicatedEventArgs(ItemObject newItem, ItemObject oldItem, List<string> traits)
        {
            NewItem = newItem;
            OldItem = oldItem;
            Traits = traits;
        }

        public ItemObject NewItem { get; set; }
        public ItemObject OldItem { get; set; }
        public List<string> Traits { get; set; }
    }

    public class EnchantmentLearnedEventArgs(Hero hero, string enchantmentTraitId) : EventArgs
    {
        public Hero Hero { get; set; } = hero;
        public string EnchantmentTrait { get; set; } = enchantmentTraitId;
    }

    public class BrawlWonEventArgs(Hero hero) : EventArgs
    {
        public Hero Hero { get; set; } = hero;
    }

    public class TournamentWonEventArgs(Hero hero) : EventArgs
    {
        public Hero Hero { get; set; } = hero;
    }

    public class TeefTransferredEventArgs(Hero hero, int amount) : EventArgs
    {
        public Hero Hero { get; set; } = hero;
        public int Amount { get; set; } = amount;
    }

    public class LordDuelWonEventArgs(Hero hero, Hero opponent) : EventArgs
    {
        public Hero Hero { get; set; } = hero;
        public Hero Opponent { get; set; } = opponent;
    }

    public class ShrinePrayerEventArgs(Hero hero, ReligionObject religion, Settlement shrine) : EventArgs
    {
        public Hero Hero { get; set; } = hero;
        public ReligionObject Religion { get; set; } = religion;
        public Settlement Shrine { get; set; } = shrine;
    }

    public class ShrineLootedEventArgs(Hero hero, ReligionObject religion, Settlement shrine) : EventArgs
    {
        public Hero Hero { get; set; } = hero;
        public ReligionObject Religion { get; set; } = religion;
        public Settlement Shrine { get; set; } = shrine;
    }

    public class PracticeFightWonEventArgs(Hero hero, int opponentsDefeated) : EventArgs
    {
        public Hero Hero { get; set; } = hero;
        public int OpponentsDefeated { get; set; } = opponentsDefeated;
    }
}