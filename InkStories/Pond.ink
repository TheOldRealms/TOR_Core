//Global story tags
# title: The Pond
# frequency: Common
# development: false
# illustration: pond

//Important Irregular Characters
    //| (Vertical Bar)

//Scenarios notes
    //Rarity: COMMON
    //Repeatable: YES
    
    //Restrictions
        //Terrain: Not dessert,
    
    //Triggers:
        //While travelling on campaign map
    
    //Scenario Explanation (explain the main scenario and any major variations that you are planning to build in. If a variation is different enough consider making it its own file.)
    
        //Main: You are travelling and come across a pond.

        //Alts:
            //If encountered while in a chaos area it gets weird.
            //Something comes out of the water and attacks. (River troll or River troll hag for a mini boss fight)
        
        
    //Future Options/Additions
        //Make it so the player does not have to click through each time they do a loop.
        //More options
            //Search for animals (capture geese)
            //Spend some time training
        //Add choices/effects for nature gods
            //Fishing success chance improved by Priest of Manaan
            //Healing success chance improved by Pries of Rhya or Shallya
            //Foraging success chance improved by Priest of Rhya
        //Add choices/effects for wizards (Lore of Life, Lore of Beasts, ...)
            //Lore of Beast for fishing Bonus?
        //Add randomness to the amount of plant and animal life as well as the difficulty of success
            //Randomize elements of Foraging
            //Randomize elements of Fishing
        //Add scavenging option
            //can find jewerly that was lost in the pond
        
//Data Import/Export Section
    //Make sure you include this in all ink files to get access to integration functions
        INCLUDE include.ink
        
    //List of Data Being Imported (use this to help keep track of what data you are importing; will help with troubleshooting and testing.)
    
        //Scouting Highest In party
            //Used in Fishing skill check
                VAR PartyScoutingCheckText = 0
                    //~ PartyScoutingCheckText = print_party_skill_chance("Scouting", FishDifficulty) [Variable Update]
                
                VAR PartyScoutingCheckTest = 0
                    //~ PartyScoutingCheckTest = perform_party_skill_check("Scouting", FishDifficulty) [Variable Update]
        //Medicine (Highest in Party)
            //Used in party recovery skill check
                VAR WoundedCount = 1
                    ~ WoundedCount = GetTotalPartyWoundedCount() + 1

                VAR MedicineDifficulty = 1
                    ~ MedicineDifficulty = 3*WoundedCount
                    
                    
                VAR PartyMedicineCheckText = 2
                    ~ PartyMedicineCheckText = print_party_skill_chance("Medicine", MedicineDifficulty)
                
                VAR PartyMedicineCheckTest = 2
                    ~ PartyMedicineCheckTest = perform_party_skill_check("Medicine", MedicineDifficulty)
                    
        //Ranged Weapon Skill Highest In the party
            //Bows, Crossbows, Throwing, Gunnery
                //Bow
                    VAR BowHighestInParty = 0
                        ~ BowHighestInParty = GetPartySkillValue("Bow")

                //Crossbow
                    VAR CrossbowHighestInParty = 0
                        ~ CrossbowHighestInParty = GetPartySkillValue("Crossbow")

                //Bow
                    VAR ThrowingHighestInParty = 0
                        ~ ThrowingHighestInParty = GetPartySkillValue("Throwing")
                //Bow
                    VAR GunpowderHighestInParty = 0
                        ~ GunpowderHighestInParty = GetPartySkillValue("Gunpowder")
                        
            //Comparison
                VAR SkillText1 = ""
                VAR SkillText2 = ""
                VAR SkillTextFinal = ""
                
                VAR BowVsCrossbow = 0
                    {
                        - BowHighestInParty >= CrossbowHighestInParty:
                            ~ BowVsCrossbow = BowHighestInParty
                            ~ SkillText1 = "Bow"
                        - else:
                            ~ BowVsCrossbow = CrossbowHighestInParty
                            ~ SkillText1 = "Crossbow"
                    }
                    
                VAR ThrowingVsGunpowder = 0
                    {
                        - ThrowingHighestInParty >= GunpowderHighestInParty:
                            ~ ThrowingVsGunpowder = ThrowingHighestInParty
                            ~ SkillText2 = "Throwing"
                        - else:
                            ~ ThrowingVsGunpowder = GunpowderHighestInParty
                            ~ SkillText2 = "Gunpowder"
                    }
                    
                VAR FinalComparison = 0
                    {
                        - BowVsCrossbow >= ThrowingVsGunpowder:
                            ~ FinalComparison = BowVsCrossbow
                            ~ SkillTextFinal = SkillText1
                        - else:
                            ~ FinalComparison = ThrowingVsGunpowder
                            ~ SkillTextFinal = SkillText2
                    }
 
            //RangedSkillCheck
                VAR PartyRangedSkillCheckText = 1
                    //~ PartyRangedSkillCheckText = print_party_skill_chance(SkillTextFinal, 200) [Variable Update]
                    
            //Wizards in party
                //Lore of life
                    VAR LoreOfLifeInParty = false
                        ~ LoreOfLifeInParty = DoesPartyKnowSchoolOfMagic(false, "LoreOfLife")
                //Lore of life
                    VAR LoreOfBeastsInParty = false
                        ~ LoreOfBeastsInParty = DoesPartyKnowSchoolOfMagic(false, "LoreOfBeasts")
                        
    //Data Exported (use this to help keep track of what data you are exporting; will help with troubleshooting and testing.)
        //Give Items
            
        
//Variables setup
    //IMPORTANT! Initial values are mandatory, but they can only be primitives (number, string, boolean). If we want to assign the return value of a function to the variable, we must do it on a separate line, see one line below

    //Seed
        //~ SEED_RANDOM(100) //Uncomment to lock an RNG testing seed for the randomness. Change number inside () for different seed
        
    //Fish
        VAR FishDifficulty = 50
            
        VAR FishLoops = 3
        
        VAR HideSuccessful = false
        
    //Forage
        VAR ForageDifficulty = 75
        
        VAR ForageLoops = 5
        
    //Was X attempt successful
        VAR AttemptSuccessful = false
        
    //Reward Roll
        VAR RewardRoll = 0
        

//Variable Update (Update any variables here)
    ~ PartyScoutingCheckText = print_party_skill_chance("Scouting", FishDifficulty)
                
    //~ PartyScoutingCheckTest = perform_party_skill_check("Scouting", FishDifficulty) Needs to be done each loop

    ~ PartyRangedSkillCheckText = print_party_skill_chance(SkillTextFinal, FishDifficulty*2)
    
//Variable Check (Use for sanity check. Uncomment variables to see what they are)

-> Start

===Start===

    As your party is on the march you come across a refreshing looking pond.
        ->choice1
        
    =choice1
        What will you have your party do?
            *[Forage for wild plants (Multiple attempts at {LoreOfLifeInParty: 50% chance (Improved by Lore of Life)| 25% chance} to succeed at harvesting various wild plants; Party will enter Disorganized State)] You order your party to forage amongst the plants of the forest.
            
                //Lore of Life in Party Increases success chance
                    {LoreOfLifeInParty:
                        -true: 
                            A mage in your party calls upon the Wind of Ghyran to aid your men in their search.
                            ~ ForageDifficulty = ForageDifficulty - 25
                        -false:
                        -else: ERROR
                    }
                    
                ->ForageLoop

            *[Fish (Multiple chances to get Fish (50% success chance); Party will enter Disorganized State)] You have your men go fishing.
                
                ->FishLoop
                
            *[Have your men rest (All companions healed and all wounded troops recovered {PartyMedicineCheckText}; Party will enter Disorganized State)]
                
                You try to give your men a break hoping that the brief respite will help them get better.
                
                {PartyMedicineCheckTest:
                    -true: 
                        ~ HealPartyToFull()
                    -false:
                    -else: "ERROR"
                }
                
                {PartyMedicineCheckTest: Your party takes advantage of the break to take care of the wounded.| Unfortunately just as the men start to tend to the wounded, the pond gurgles and starts becoming a sink hole. You rush your men away as fast as you can.}
                
                ->Leave
                
            *[Leave immediately (Leave)] You decide your party has no time to rest and set out immediately.->END

    =ForageLoop
        //Decrease number of loops remaining
            ~ ForageLoops = ForageLoops - 1
        
        //Was attempt successful
            {RANDOM(0,100)>=ForageDifficulty:
                -true:
                    ~ AttemptSuccessful = true
                -false:
                    ~ AttemptSuccessful = false
                -else: ERROR
            }

        //Reward if successful
            {AttemptSuccessful:
                -true:
                    ~ RewardRoll = 1
                -false:
                    ~ RewardRoll = 0
            }
            
            {RewardRoll:
                -0:
                    Your men find nothing.
                -1:
                    Your men find some wild rice. (+1 Grain)
                    ~ GiveItem("grain",1)
            }
            
        //End of Loop
            {ForageLoops > 0 : ->ForageLoop | ->Leave}
        
    ->END
    
    =FishLoop
        //Decrease number of loops remaining
            ~ FishLoops = FishLoops - 1
        
        //Was attempt successful
            {RANDOM(0,100)>=ForageDifficulty:
                -true:
                    ~ AttemptSuccessful = true
                -false:
                    ~ AttemptSuccessful = false
                -else: ERROR
            }

        //Reward
            {
                - AttemptSuccessful == true:
                    Your men catch some fish
                    ~ GiveItem("fish",1)
                - AttemptSuccessful == false:
                     Your men catch nothing.
                -else: ERROR
            }
                    
        //End of Loop
            {FishLoops > 0 : ->FishLoop | ->Leave}
        
    ->END
    
===Leave===
    Having spent your time by the pond you decide to head off.

    ~ MakePartyDisorganized()
    
-> END
