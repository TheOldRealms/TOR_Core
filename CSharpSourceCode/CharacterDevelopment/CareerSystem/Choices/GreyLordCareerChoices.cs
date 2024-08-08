using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TOR_Core.AbilitySystem;
using TOR_Core.BattleMechanics;
using TOR_Core.BattleMechanics.DamageSystem;
using TOR_Core.BattleMechanics.StatusEffect;
using TOR_Core.BattleMechanics.TriggeredEffect;
using TOR_Core.CampaignMechanics;
using TOR_Core.CampaignMechanics.Choices;
using TOR_Core.Extensions;
using TOR_Core.Extensions.ExtendedInfoSystem;
using TOR_Core.Models;
using TOR_Core.Utilities;


    namespace TOR_Core.CharacterDevelopment.CareerSystem.Choices
    {
        public class GreyLordCareerChoices(CareerObject id) : TORCareerChoicesBase(id)
        {
            private CareerChoiceObject _greyLordRoot;
            
            private CareerChoiceObject _caelithsWisdomPassive1;
            private CareerChoiceObject _caelithsWisdomPassive2;
            private CareerChoiceObject _caelithsWisdomPassive3;
            private CareerChoiceObject _caelithsWisdomPassive4;
            private CareerChoiceObject _caelithsWisdomKeystone;

            private CareerChoiceObject _secretOfForestDragonPassive1;
            private CareerChoiceObject _secretOfForestDragonPassive2;
            private CareerChoiceObject _secretOfForestDragonPassive3;
            private CareerChoiceObject _secretOfForestDragonPassive4;
            private CareerChoiceObject _secretOfForestDragonKeystone;

            private CareerChoiceObject _legendsOfMalokPassive1;
            private CareerChoiceObject _legendsOfMalokPassive2;
            private CareerChoiceObject _legendsOfMalokPassive3;
            private CareerChoiceObject _legendsOfMalokPassive4;
            private CareerChoiceObject _legendsOfMalokKeystone;

            private CareerChoiceObject _secretOfSunDragonPassive1;
            private CareerChoiceObject _secretOfSunDragonPassive2;
            private CareerChoiceObject _secretOfSunDragonPassive3;
            private CareerChoiceObject _secretOfSunDragonPassive4;
            private CareerChoiceObject _secretOfSunDragonKeystone;

            private CareerChoiceObject _secretOfStarDragonPassive1;
            private CareerChoiceObject _secretOfStarDragonPassive2;
            private CareerChoiceObject _secretOfStarDragonPassive3;
            private CareerChoiceObject _secretOfStarDragonPassive4;
            private CareerChoiceObject _secretOfStarDragonKeystone;

            private CareerChoiceObject _secretOfMoonDragonPassive1;
            private CareerChoiceObject _secretOfMoonDragonPassive2;
            private CareerChoiceObject _secretOfMoonDragonPassive3;
            private CareerChoiceObject _secretOfMoonDragonPassive4;
            private CareerChoiceObject _secretOfMoonDragonKeystone;

            private CareerChoiceObject _secretOfFellfangPassive1;
            private CareerChoiceObject _secretOfFellfangPassive2;
            private CareerChoiceObject _secretOfFellfangPassive3;
            private CareerChoiceObject _secretOfFellfangPassive4;
            private CareerChoiceObject _secretOfFellfangKeystone;


            protected override void RegisterAll()
            {
                _greyLordRoot = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("GreyLordRoot"));
                
                _caelithsWisdomPassive1 =
                    Game.Current.ObjectManager.RegisterPresumedObject(
                        new CareerChoiceObject(nameof(_caelithsWisdomPassive1).UnderscoreFirstCharToUpper()));
                _caelithsWisdomPassive2 =
                    Game.Current.ObjectManager.RegisterPresumedObject(
                        new CareerChoiceObject(nameof(_caelithsWisdomPassive2).UnderscoreFirstCharToUpper()));
                _caelithsWisdomPassive3 =
                    Game.Current.ObjectManager.RegisterPresumedObject(
                        new CareerChoiceObject(nameof(_caelithsWisdomPassive3).UnderscoreFirstCharToUpper()));
                _caelithsWisdomPassive4 =
                    Game.Current.ObjectManager.RegisterPresumedObject(
                        new CareerChoiceObject(nameof(_caelithsWisdomPassive4).UnderscoreFirstCharToUpper()));
                _caelithsWisdomKeystone =
                    Game.Current.ObjectManager.RegisterPresumedObject(
                        new CareerChoiceObject(nameof(_caelithsWisdomKeystone).UnderscoreFirstCharToUpper()));

                _secretOfForestDragonPassive1 =
                    Game.Current.ObjectManager.RegisterPresumedObject(
                        new CareerChoiceObject(nameof(_secretOfForestDragonPassive1).UnderscoreFirstCharToUpper()));
                _secretOfForestDragonPassive2 =
                    Game.Current.ObjectManager.RegisterPresumedObject(
                        new CareerChoiceObject(nameof(_secretOfForestDragonPassive2).UnderscoreFirstCharToUpper()));
                _secretOfForestDragonPassive3 =
                    Game.Current.ObjectManager.RegisterPresumedObject(
                        new CareerChoiceObject(nameof(_secretOfForestDragonPassive3).UnderscoreFirstCharToUpper()));
                _secretOfForestDragonPassive4 =
                    Game.Current.ObjectManager.RegisterPresumedObject(
                        new CareerChoiceObject(nameof(_secretOfForestDragonPassive4).UnderscoreFirstCharToUpper()));
                _secretOfForestDragonKeystone =
                    Game.Current.ObjectManager.RegisterPresumedObject(
                        new CareerChoiceObject(nameof(_secretOfForestDragonKeystone).UnderscoreFirstCharToUpper()));

                _legendsOfMalokPassive1 =
                    Game.Current.ObjectManager.RegisterPresumedObject(
                        new CareerChoiceObject(nameof(_legendsOfMalokPassive1).UnderscoreFirstCharToUpper()));
                _legendsOfMalokPassive2 =
                    Game.Current.ObjectManager.RegisterPresumedObject(
                        new CareerChoiceObject(nameof(_legendsOfMalokPassive2).UnderscoreFirstCharToUpper()));
                _legendsOfMalokPassive3 =
                    Game.Current.ObjectManager.RegisterPresumedObject(
                        new CareerChoiceObject(nameof(_legendsOfMalokPassive3).UnderscoreFirstCharToUpper()));
                _legendsOfMalokPassive4 =
                    Game.Current.ObjectManager.RegisterPresumedObject(
                        new CareerChoiceObject(nameof(_legendsOfMalokPassive4).UnderscoreFirstCharToUpper()));
                _legendsOfMalokKeystone =
                    Game.Current.ObjectManager.RegisterPresumedObject(
                        new CareerChoiceObject(nameof(_legendsOfMalokKeystone).UnderscoreFirstCharToUpper()));

                _secretOfSunDragonPassive1 =
                    Game.Current.ObjectManager.RegisterPresumedObject(
                        new CareerChoiceObject(nameof(_secretOfSunDragonPassive1).UnderscoreFirstCharToUpper()));
                _secretOfSunDragonPassive2 =
                    Game.Current.ObjectManager.RegisterPresumedObject(
                        new CareerChoiceObject(nameof(_secretOfSunDragonPassive2).UnderscoreFirstCharToUpper()));
                _secretOfSunDragonPassive3 =
                    Game.Current.ObjectManager.RegisterPresumedObject(
                        new CareerChoiceObject(nameof(_secretOfSunDragonPassive3).UnderscoreFirstCharToUpper()));
                _secretOfSunDragonPassive4 =
                    Game.Current.ObjectManager.RegisterPresumedObject(
                        new CareerChoiceObject(nameof(_secretOfSunDragonPassive4).UnderscoreFirstCharToUpper()));
                _secretOfSunDragonKeystone =
                    Game.Current.ObjectManager.RegisterPresumedObject(
                        new CareerChoiceObject(nameof(_secretOfSunDragonKeystone).UnderscoreFirstCharToUpper()));

                _secretOfStarDragonPassive1 =
                    Game.Current.ObjectManager.RegisterPresumedObject(
                        new CareerChoiceObject(nameof(_secretOfStarDragonPassive1).UnderscoreFirstCharToUpper()));
                _secretOfStarDragonPassive2 =
                    Game.Current.ObjectManager.RegisterPresumedObject(
                        new CareerChoiceObject(nameof(_secretOfStarDragonPassive2).UnderscoreFirstCharToUpper()));
                _secretOfStarDragonPassive3 =
                    Game.Current.ObjectManager.RegisterPresumedObject(
                        new CareerChoiceObject(nameof(_secretOfStarDragonPassive3).UnderscoreFirstCharToUpper()));
                _secretOfStarDragonPassive4 =
                    Game.Current.ObjectManager.RegisterPresumedObject(
                        new CareerChoiceObject(nameof(_secretOfStarDragonPassive4).UnderscoreFirstCharToUpper()));
                _secretOfStarDragonKeystone =
                    Game.Current.ObjectManager.RegisterPresumedObject(
                        new CareerChoiceObject(nameof(_secretOfStarDragonKeystone).UnderscoreFirstCharToUpper()));

                _secretOfMoonDragonPassive1 =
                    Game.Current.ObjectManager.RegisterPresumedObject(
                        new CareerChoiceObject(nameof(_secretOfMoonDragonPassive1).UnderscoreFirstCharToUpper()));
                _secretOfMoonDragonPassive2 =
                    Game.Current.ObjectManager.RegisterPresumedObject(
                        new CareerChoiceObject(nameof(_secretOfMoonDragonPassive2).UnderscoreFirstCharToUpper()));
                _secretOfMoonDragonPassive3 =
                    Game.Current.ObjectManager.RegisterPresumedObject(
                        new CareerChoiceObject(nameof(_secretOfMoonDragonPassive3).UnderscoreFirstCharToUpper()));
                _secretOfMoonDragonPassive4 =
                    Game.Current.ObjectManager.RegisterPresumedObject(
                        new CareerChoiceObject(nameof(_secretOfMoonDragonPassive4).UnderscoreFirstCharToUpper()));
                _secretOfMoonDragonKeystone =
                    Game.Current.ObjectManager.RegisterPresumedObject(
                        new CareerChoiceObject(nameof(_secretOfMoonDragonKeystone).UnderscoreFirstCharToUpper()));

                _secretOfFellfangPassive1 =
                    Game.Current.ObjectManager.RegisterPresumedObject(
                        new CareerChoiceObject(nameof(_secretOfFellfangPassive1).UnderscoreFirstCharToUpper()));
                _secretOfFellfangPassive2 =
                    Game.Current.ObjectManager.RegisterPresumedObject(
                        new CareerChoiceObject(nameof(_secretOfFellfangPassive2).UnderscoreFirstCharToUpper()));
                _secretOfFellfangPassive3 =
                    Game.Current.ObjectManager.RegisterPresumedObject(
                        new CareerChoiceObject(nameof(_secretOfFellfangPassive3).UnderscoreFirstCharToUpper()));
                _secretOfFellfangPassive4 =
                    Game.Current.ObjectManager.RegisterPresumedObject(
                        new CareerChoiceObject(nameof(_secretOfFellfangPassive4).UnderscoreFirstCharToUpper()));
                _secretOfFellfangKeystone =
                    Game.Current.ObjectManager.RegisterPresumedObject(
                        new CareerChoiceObject(nameof(_secretOfFellfangKeystone).UnderscoreFirstCharToUpper()));


            }

            protected override void InitializeKeyStones()
            {
                
                _greyLordRoot.Initialize(CareerID, "root", null, true,
                    ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                    {
 
                    });
                _caelithsWisdomKeystone.Initialize(CareerID, "Caelith's Wisdom grants unparalleled insight and strategic advantage.", "CaelithsWisdom", true,
                    ChoiceType.Passive);
                _secretOfForestDragonKeystone.Initialize(CareerID, "Secret of the Forest Dragon enhances agility and resilience.", "SecretOfForestDragon", true,
                    ChoiceType.Passive);
                _legendsOfMalokKeystone.Initialize(CareerID, "Legends of Malok imbue the bearer with ancient strength.", "LegendsOfMalok", true, ChoiceType.Passive);
                _secretOfSunDragonKeystone.Initialize(CareerID, "Secret of the Sun Dragon bestows fiery power and endurance.", "SecretOfSunDragon", true,
                    ChoiceType.Passive);
                _secretOfStarDragonKeystone.Initialize(CareerID, "Secret of the Star Dragon grants cosmic wisdom and clarity.", "SecretOfStarDragon", true,
                    ChoiceType.Passive);
                _secretOfMoonDragonKeystone.Initialize(CareerID, "Secret of the Moon Dragon offers mystical protection and serenity.", "SecretOfMoonDragon", true,
                    ChoiceType.Passive);
                
                _secretOfFellfangKeystone.Initialize(CareerID, "Secret of the Moon Dragon offers mystical protection and serenity.", "SecretOfFellfang", true,
                    ChoiceType.Passive);
            }

            protected override void InitializePassives()
            {

            }
        }
    }