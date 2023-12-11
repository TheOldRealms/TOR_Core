//Global story tags
# title: The Meadow
# frequency: Common
# development: false
# illustration: meadow

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
    
        //Main: You are travelling and come across a meadow.

        //Alt: If encountered while in a chaos area it gets weird.
        
    //Future Options/Additions
        //Make it so the player does not have to click through each time they do a loop.
        //More options
            //Search for animals (capture animals like horses)
            //If a lore of life wizard is present they can channel to regain some magic
            //Spend some time training
        //Add choices/effects for nature gods
            //Hunting success chance improved by Priest of Tall
            //Healing success chance improved by Pries of Rhya or Shallya
            //Foraging success chance improved by Priest of Rhya
        //Add choices/effects for wizards (Lore of Life, Lore of Beasts, ...)
            //Hunting success chance improved by lore of beasts
            //Healing success chance improved by Lore of Life
            //Foraging improved by lore of Life
        //?Add olives as a future option
        //Add randomness to the amount of plant and animal life as well as the difficulty of success
            //Randomize elements of Foraging
            //Randomize elements of Hunting
        
//Data Import/Export Section
    //Make sure you include this in all ink files to get access to integration functions
        INCLUDE include.ink
        
    //List of Data Being Imported (use this to help keep track of what data you are importing; will help with troubleshooting and testing.)
    
        //Scouting Highest In party
            //Used in hunting skill check
                VAR PartyScoutingCheckText = 0
                    //~ PartyScoutingCheckText = print_party_skill_chance("Scouting", HuntDifficulty) [Variable Update]
                
                VAR PartyScoutingCheckTest = 0
                    //~ PartyScoutingCheckTest = perform_party_skill_check("Scouting", HuntDifficulty) [Variable Update]
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
        
    //Hunt
        VAR HuntDifficulty = 150
            
        VAR HuntLoops = 3
        
        VAR HideSuccessful = false
        
    //Forage
        VAR ForageDifficulty = 50
        
        VAR ForageLoops = 5
        
    //Was X attempt successful
        VAR AttemptSuccessful = false
        
    //Reward Roll
        VAR RewardRoll = 0
        

//Variable Update (Update any variables here)
    ~ PartyScoutingCheckText = print_party_skill_chance("Scouting", HuntDifficulty)
                
    //~ PartyScoutingCheckTest = perform_party_skill_check("Scouting", HuntDifficulty) Needs to be done each loop

    ~ PartyRangedSkillCheckText = print_party_skill_chance(SkillTextFinal, HuntDifficulty*2)
    
    
    
//Variable Check (Use for sanity check. Uncomment variables to see what they are)
    
     ~ SetTextVariable("IsNight",IsNight())

-> Start

===Start===

    You and your party have been trudging along a winding path, when the landscape gradually shifts. The air becomes gentler, the sounds more peaceful. #STR_Start1
    And then, you step into a clearing, and a gasp escapes your lips. Before you stretches a meadow unlike any you've seen in a long while. The grass is a rich tapestry of green, waving softly in the breeze. A multitude of wildflowers paints the scene with vibrant hues of red, purple, and yellow. #STR_Start2
    {IsNight(): Silver moonlight | Golden sunlight } filters through the tree canopy, bathing the meadow in warmth. A gentle aroma of blooming flowers perfumes the air. Birds serenade with their melodies, filling the clearing with a symphony that feels like a balm to your weary spirit. #STR_Start3
        ->choice1
        
    =choice1
        What will you have your party do? #STR_Start4
            *[Forage for wild plants (Multiple attempts at {LoreOfLifeInParty: 75% chance (Improved by Lore of Life)| 50% chance} to succeed at harvesting various wild plants)] 
            You order your party to forage amongst the plants of the meadow. #STR_Forage1
            
                //Lore of Life in Party Increases success chance
                    {LoreOfLifeInParty:
                        -true: 
                            A mage in your party calls upon the Wind of Ghyran to aid your men in their search. #STR_Forage_LoreOFLifeInParty
                            ~ ForageDifficulty = ForageDifficulty - 25
                        -false:
                        -else: ERROR
                    }
                    
                ->ForageLoop

            *[Hunt animals (Multiple chances to get meat, hide, and or fur {PartyRangedSkillCheckText})]
                
                //Bonus Attempts from Lore of Beasts
                    {LoreOfBeastsInParty:
                        -true: 
                            A mage in your party calls upon the Wind of Ghur to aid your men in their search. (+1 attempt) #STR_Hunt_LoreOFBeastInParty
                            ~ HuntLoops = HuntLoops + 1
                        -false:
                        -else: ERROR
                    }
                    
                //Bonus attempt from Scouting
                    {perform_party_skill_check("Scouting", HuntDifficulty):
                        -true: 
                            Your scouts manage to locate some extra animals. (Scouting)(+1 Attempt) #STR_Hunt_Scout
                            ~HuntLoops = HuntLoops + 1
                        -false:
                        -else: ERROR
                    }
                    
                ->HuntLoop
                
            *[Have your men rest (All companions healed and all wounded troops recovered {PartyMedicineCheckText})]
                
                You try to give your men a break hoping that the brief respite will help them get better. You set up a makeshift camp at the meadow's edge. #STR_Rest1
                
                {PartyMedicineCheckTest:
                    -true: 
                        ~ HealPartyToFull()
                    -false:
                    -else: "ERROR"
                }
                
                {PartyMedicineCheckTest: Your party takes advantage of the break to take care of the wounded.| Unfortunately just as the men start to try and rest, a large storm comes through and forces your party to try and move to find shelter.} #STR_Rest2
                
                ->Leave
                
            *[Leave] You decide your party has no time to rest and set out immediately.->END

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
                    ~ RewardRoll = RANDOM(1,4)
                -false:
                    ~ RewardRoll = 0
            }
            
            {RewardRoll:
                -0:
                    Foraging yields no results, your men return empty handed. #ForageLoop0
                -1:
                    Your men find some wild grain. (+1 Grain) #ForageLoop1
                    ~ GiveItem("grain",1)
                -2:
                    Your men find some wild berries. (+1 Grapes) #ForageLoop2
                    ~ GiveItem("grape",1)
                -3:
                    Your men find some wild flax. (+1 Flax) #ForageLoop3
                    ~ GiveItem("flax",1)
                -4:
                    Your men find some wild spices. (+1 Spice) #ForageLoop4
                    ~ GiveItem("spice",1)
            }
            
        //End of Loop
            {ForageLoops > 0 : ->ForageLoop | ->Leave}
        
    ->END
    
    =HuntLoop
        //Decrease number of loops remaining
            ~HuntLoops = HuntLoops - 1
        
        //Trouble assistance shooting each loop
            //{FinalComparison}
            //{print_party_skill_chance("Scouting", HuntDifficulty)}
            //{perform_party_skill_check("Scouting", HuntDifficulty)}
        
        //Was attempt successful
            {perform_party_skill_check(SkillTextFinal, HuntDifficulty):
                -true:
                    ~ AttemptSuccessful = true
                -false:
                    ~ AttemptSuccessful = false
                -else: ERROR
            }
            
            //Roll for bonus hide
                ~ HideSuccessful = perform_party_skill_check(SkillTextFinal, HuntDifficulty*2)
            
                {HideSuccessful:
                    -true:
                        ~ RewardRoll = RANDOM(1,2)
                    -false:
                        ~ RewardRoll = 0
                }
            
        //Reward
            {
                - AttemptSuccessful == true && RewardRoll == 0:
                    Your men are successful, they return with some pheasants. (+1 Meat) #HuntLoop0
                    ~ GiveItem("meat",1)
                    ~ GiveItem("hides",1)
                - AttemptSuccessful == true && RewardRoll == 1:
                    Your men are successful, they return with a deer. (+1 Meat, +1 Hide) #HuntLoop1
                    ~ GiveItem("meat",1)
                    ~ GiveItem("hides",1)
                - AttemptSuccessful == true && RewardRoll == 2:
                    Your men are successful, they return with a wild boar. (+1 Meat, +1 Fur) #HuntLoop2
                    ~ GiveItem("meat",1)
                    ~ GiveItem("fur",1)
                - AttemptSuccessful == false:
                     Wild game eludes you, your men return empty handed. #HuntLoop3
                -else: ERROR
            }
                    
        //End of Loop
            {HuntLoops > 0 : ->HuntLoop | ->Leave}
        
    ->END
    
===Leave===
    Having spent your time in the meadow you decide to head off. #STR_Leave

    ~ MakePartyDisorganized()
    
-> END

