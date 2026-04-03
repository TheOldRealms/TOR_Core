using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TOR_Core.CharacterDevelopment;
using static TaleWorlds.CampaignSystem.Settlements.Workshops.WorkshopType;

namespace TOR_Core.CampaignMechanics
{
    public class TORVillageTypes
    {
        public static TORVillageTypes Instance { get; private set; }

        private VillageType _villageTypeGreenskinSwineFarm;

        public static VillageType VillageTypeGreenskinSwineFarm => Instance._villageTypeGreenskinSwineFarm;

        public TORVillageTypes()
        {
            RegisterAll();
            InitializeAll();
            AddProductions();
        }
        private void RegisterAll()
        {
            _villageTypeGreenskinSwineFarm = Game.Current.ObjectManager.RegisterPresumedObject<VillageType>(new VillageType("greenskin_swine_farm"));
        }
        private void InitializeAll()
        {
            _villageTypeGreenskinSwineFarm.Initialize(new TextObject("{=vqSHB7mJ}Swine Farm"), "swine_farm", "swine_farm_ucon", "swine_farm_burned", new ValueTuple<ItemObject, float>[]
			{
				new ValueTuple<ItemObject, float>(DefaultItems.Grain, 3f)
			});
        }

        private void AddProductions()
        {
            AddVillageProductions(_villageTypeGreenskinSwineFarm, new ValueTuple<string, float>[]
			{
				new ValueTuple<string, float>("hog", 8f),
				new ValueTuple<string, float>("tor_greenskin_mount_boar_001", 0.4f),
				new ValueTuple<string, float>("butter", 2f),
				new ValueTuple<string, float>("cheese", 2f)
			});
        }

        public void AddVillageProductions(VillageType villageType, ValueTuple<string, float>[] productions)
        {
            villageType.AddProductions(productions.Select((ValueTuple<string, float> p) => new ValueTuple<ItemObject, float>(Game.Current.ObjectManager.GetObject<ItemObject>(p.Item1), p.Item2)));
        }
    }
}
