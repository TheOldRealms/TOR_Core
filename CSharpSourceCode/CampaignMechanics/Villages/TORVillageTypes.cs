using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TOR_Core.CharacterDevelopment;
using TOR_Core.Extensions;
using static TaleWorlds.CampaignSystem.Settlements.Workshops.WorkshopType;

namespace TOR_Core.CampaignMechanics
{
    public class TORVillageTypes : DefaultVillageTypes
        {
        public static TORVillageTypes Instance { get; private set; }


        public static VillageType GreenskinSwineFarm => Instance._greenskinSwineFarm;
        public static VillageType GreenskinWolfFarm => Instance._greenskinWolfFarm;
        public static VillageType DawiWheatFarm => Instance._dawiWheatFarm;
        public static VillageType EmpireHorseFarm => Instance._empireHorseFarm;
        public static VillageType BretonnianHorseFarm => Instance._bretonnianHorseFarm;
        
        
        private VillageType _greenskinSwineFarm;
        private VillageType _greenskinWolfFarm;
        private VillageType _dawiWheatFarm;
        private VillageType _empireHorseFarm;
        private VillageType _bretonnianHorseFarm;

        public TORVillageTypes()
        {
            Instance = this;
            RegisterAll();
            InitializeAll();
            AddProductions();
        }
        private void RegisterAll()
        {
            _greenskinSwineFarm = RegisterVillageTypeObject("greenskin_swine_farm");
            _greenskinWolfFarm = RegisterVillageTypeObject("greenskin_wolf_farm");
            _dawiWheatFarm = RegisterVillageTypeObject("dawi_wheat_farm");
            _empireHorseFarm = RegisterVillageTypeObject("empire_horse_farm");
            _bretonnianHorseFarm = RegisterVillageTypeObject("bretonnian_horse_farm");
        }
        private void InitializeAll()
        {
            _greenskinSwineFarm.Initialize(new TextObject("{=vqSHB7mJ}Swine Farm"), "swine_farm", "swine_farm_ucon", "swine_farm_burned", []);
            _greenskinWolfFarm.Initialize(TORTextHelper.GetTextObject("tor_villagetype_wolffarm", "Wolf Farm"), "trapper", "trapper_ucon", "trapper_burned", []);
            _dawiWheatFarm.Initialize(new TextObject("{=BPPG2XF7}Wheat Farm"), "wheat_farm", "wheat_farm_ucon", "wheat_farm_burned", []);
            _empireHorseFarm.Initialize(new TextObject("{=eEh752CZ}Horse Farm"), "europe_horse_ranch", "ranch_ucon", "europe_horse_ranch_burned", []);
            _bretonnianHorseFarm.Initialize(new TextObject("{=eEh752CZ}Horse Farm"), "vlandian_horse_ranch", "ranch_ucon", "desert_horse_ranch_burned", []);
        }
        
        private void AddProductions()
        {
            AddVillageProductions(_greenskinSwineFarm,
            [
                ("grain", 3f),
                ("hog", 8f),
                ("hides", 2f),
				("tor_greenskin_mount_boar_001", 0.4f),
				("butter", 2f),
				("cheese", 2f)
			]);
            AddVillageProductions(_greenskinWolfFarm,
            [
                ("grain", 3f),
                ("meat", 6f),
                ("hides", 1f),
                ("fur", 1f),
				("tor_greenskin_mount_wolf_001", 0.2f),
				("tor_greenskin_mount_wolf_004", 0.2f)
			]);
            AddVillageProductions(_dawiWheatFarm,
            [
                ("grain", 40f),
                ("beer", 5f)
			]);
            AddVillageProductions(_empireHorseFarm,
            [
                ("grain", 3f),
                ("tor_empire_mount_horse_001", 2.1f),
                ("tor_empire_mount_horse_002", 0.4f),
                ("tor_empire_mount_horse_003", 0.08f),
                ("sumpter_horse", 0.5f),
                ("mule", 0.5f),
                ("saddle_horse", 0.5f),
                ("old_horse", 0.5f)
			]);
            AddVillageProductions(_bretonnianHorseFarm,
            [
                ("grain", 3f),
                ("tor_bretonnia_mount_horse_001", 2.1f),
                ("tor_bretonnia_mount_horse_002", 0.4f),
                ("tor_bretonnia_mount_horse_003", 0.08f),
                ("sumpter_horse", 0.5f),
                ("mule", 0.5f),
                ("saddle_horse", 0.5f),
                ("old_horse", 0.5f)
			]);

            foreach (var villageType in Game.Current.ObjectManager.GetObjectTypeList<VillageType>())
            {
                if (villageType.PrimaryProduction.StringId == "grain") continue;

                AddVillageProductions(villageType, [("grain", 10f)]);
            }
        }

        private VillageType RegisterVillageTypeObject(string id)
        {
            return Game.Current.ObjectManager.RegisterPresumedObject<VillageType>(new VillageType(id));
        }

        public void AddVillageProductions(VillageType villageType, ValueTuple<string, float>[] productions)
        {
            villageType.AddProductions(productions.Select((ValueTuple<string, float> p) => new ValueTuple<ItemObject, float>(Game.Current.ObjectManager.GetObject<ItemObject>(p.Item1), p.Item2)));
        }
    }
}
