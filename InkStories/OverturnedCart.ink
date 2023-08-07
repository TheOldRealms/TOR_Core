//Global story tags
# title: Roadside Accident
# frequency: Common
# development: false
# illustration: cart_accident

//Important Irregular Characters
    //| (Vertical Bar)

//Scenarios notes
    //Rarity: COMMON
    //Repeatable: YES
    
    
    //Triggers:
        //While travelling on the campaign map
    
    //Scenario Explanation (explain the main scenario and any major variations that you are planning to build in. If a variation is different enough consider making it its own file.)
    
        //Main: You come across an overturned cart with a person stuck underneath

        //Alt1: The person is dead and its an ambush
        //Alt2: The person has been dead for a long time but the person has become a spirit haunting the location. There will be checks to see if the party notices, noticing drastically changes the options.
        
    //Future Options/Additions
        //More options for healing the man if he is injured (Lore of Nurgle, Prayers to Shallya or Rhea)
        
//Data Import/Export Section
    //Make sure you include this in all ink files to get access to integration functions
        INCLUDE include.ink
        
    //List of Data Being Imported (use this to help keep track of what data you are importing; will help with troubleshooting and testing.)
    
        //Settlement and Notable Info
            VAR Settlement = ""
                ~ Settlement = GetNearestSettlement("town")
                
            VAR Notable = ""
                ~ Notable = GetRandomNotableFromSpecificSettlement(Settlement)
                
            VAR NotableChange = false
                
        //Can party raise dead
            VAR PartyCanRaiseDead = false
                ~ PartyCanRaiseDead = PartyHasNecromancer(false)
                
        //Necromancy skill check
            VAR RaiseDeadSkillCheckText = ""
                ~ RaiseDeadSkillCheckText = print_party_skill_chance("Spellcraft", 25)
                
            VAR RaiseDeadSkillCheckTest = false
                ~ RaiseDeadSkillCheckTest = perform_party_skill_check("Spellcraft", 25)
                
        //Medicine skill check
            VAR MedicineSkillCheckText = ""
                //~ MedicineSkillCheckText = print_party_skill_chance("Medicine", InjuryDifficulty) [Variable Update]
                
            VAR MedicineSkillCheckTest = false
                //~ MedicineSkillCheckTest = perform_party_skill_check("Medicine", InjuryDifficulty) [Variable Update]
                
        //Spellcraft Healing check
            VAR SpellcraftSkillCheckText = ""
                //~ SpellcraftSkillCheckText = print_party_skill_chance("Spellcraft", InjuryDifficulty) [Variable Update]
                
            VAR SpellcraftSkillCheckTest = false
                //~ SpellcraftSkillCheckTest = perform_party_skill_check("Spellcraft", InjuryDifficulty) [Variable Update]
                
        //Lore of Life Caster in party
            VAR LoreOfLifeInParty = false
                ~ LoreOfLifeInParty = DoesPartyKnowSchoolOfMagic(false, "LoreOfLife")
        
    //Data Exported (use this to help keep track of what data you are exporting; will help with troubleshooting and testing.)
        
        //Relationship change with Notable
            //{ChangeRelations(Notable, 5)}
            
        //Give Horses
            VAR Give1Horse = 0
                ~ Give1Horse = GiveItem("vlandia_horse_tournament",1)
            
            VAR Give2Horse = 0
                ~ Give2Horse = GiveItem("vlandia_horse_tournament",2)
                

        
//Variables setup
    //IMPORTANT! Initial values are mandatory, but they can only be primitives (number, string, boolean). If we want to assign the return value of a function to the variable, we must do it on a separate line, see one line below

    //Seed
        //~ SEED_RANDOM(100) //Uncomment to lock an RNG testing seed for the randomness. Change number inside () for different seed
        
    //Injury Setup
        VAR InjuryRoll = 2
            //~ InjuryRoll = RANDOM(0,2)
            
        VAR InjuryText1 = ""
            {InjuryRoll:
                -0: ~InjuryText1 = "uninjured"
                -1: ~InjuryText1 = "mildly injured"
                -2: ~InjuryText1 = "severely injured"
            }
        
        VAR InjuryText2 = ""
            {InjuryRoll:
                -0: ~InjuryText2 = "asks"
                -1: ~InjuryText2 = "begs"
                -2: ~InjuryText2 = "gasps"
            }
        
        VAR InjuryText3 = ""
            {InjuryRoll:
                -0: ~InjuryText3 = "gets up"
                -1: ~InjuryText3 = "barely gets up"
                -2: ~InjuryText3 = "lays there trying not to die"
            }
            
        VAR InjuryText4 = ""
            {InjuryRoll:
                -0: ~InjuryText4 = ""
                -1: ~InjuryText4 = "seems to get a bit depressed knowing that he will be crippled for at least some time."
                -2: ~InjuryText4 = "dies."
            }
            
        VAR InjuryDifficulty = 2
            {InjuryRoll:
                -1: ~InjuryDifficulty = 100
                -2: ~InjuryDifficulty = 250
            }
        
            
    //Horses Around
        VAR HorsesAround = false
            //~HorsesAround = "{~true|false}"
            
            VAR HorseReward = 0
                {HorsesAround:
                    -true:
                        ~ HorseReward = Give2Horse
                    -false:
                        ~ HorseReward = ""
                }
            
    //Ask for info
        VAR IsAsked = false
        
    //Profession of the stuck man
        VAR ProfessionRoll = 0
            ~ ProfessionRoll = RANDOM(0,2)
            
    //Reward
        VAR Profession = ""
            {ProfessionRoll:
                -0: ~Profession = "merchant"
                -1: ~Profession = "farmer"
                -2: ~Profession = "blacksmith"
            }
        
        VAR RewardGive = 0
            {ProfessionRoll:
                -0: ~RewardGive = GiveGold(500)
                -1: ~RewardGive = GiveItem("grain", 5)
                -2: ~RewardGive = GiveItem("ironIngot4", 2)
            }        
        
        VAR RewardText = ""
            {ProfessionRoll:
                -0: ~RewardText = "500 gold"
                -1: ~RewardText = "5 grain"
                -2: ~RewardText = "2 steel ingots"
            }

    //Extored the stuck man
        VAR IsExtorted = false
        
    //Bonus Reward
        VAR BonusRoll = 0
        
    //Man Alive
        VAR ManAlive = true

//Variable update

~ MedicineSkillCheckText = print_party_skill_chance("Medicine", InjuryDifficulty)
~ MedicineSkillCheckTest = true//perform_party_skill_check("Medicine", InjuryDifficulty)

~ SpellcraftSkillCheckText = print_party_skill_chance("Spellcraft", InjuryDifficulty) 
~ SpellcraftSkillCheckTest = perform_party_skill_check("Spellcraft", InjuryDifficulty)

//Variable Check (Use for sanity check. Uncomment variables to see what they are)

-> Start

===Start===
    As your party is travelling along you see a cart in the distance.

    As you get closer you can see that it had broken down and tipped over. {HorsesAround == true: You can also see some horses grazing on grass in a nearby field, presumably these were pulling the cart prior to its incident.}

    *[Approach the cart]->Approach
    *[Go on your way] You decide to ignore the overturned cart and continue your journey. ->END


===Approach===

    You approach the cart and find a man stuck underneath. When he sees you approaching he calls out for help.
    
    You notice that the man trapped under the cart is {InjuryText1}.

    As you get close he {InjuryText2} to you, "Please help me". ->choice1
    
    =choice1
        What will you do?
        *[Ask what he can do for you if you help him]
            You ask the man what he can do for you.
            
            The man replies, "I am just a simple {Profession} from {Settlement}, I cannot give you a reward other than my thanks."
            
            After a moment he says, "I am a friend of {Notable} and I will put in a good word for you."
           
            While he is talking you can't help but notice there sill seems to be some cargo in the cart.
            
            ~IsAsked = true
            
            ->choice1
        
        //Help Him
            *{IsAsked == false}[Help him immediately (Mercy++)]
                You decide to immediately help him.
                
                {AddTraitInfluence("Mercy", 40)}
                
                ->AfterLift
                
            *{IsAsked == true}[Help him (+Relations with {Notable}, Mercy+)]
                You decide to help him.
                
                {AddTraitInfluence("Mercy", 20)}
                
                ~ NotableChange = true
                
                
                ->AfterLift
        
        //Extort him
            *{IsAsked == true}[Extort him for a reward ({RewardText}, Mercy-)]
                
                You tell the {Profession} that he shouldn't be so modest. He is clearly talented and can easily spare {RewardText} as compensation for the assistance.
                
                The man, believing he has no other option, agrees.
                
                {AddTraitInfluence("Mercy", -20)}
                
                ~ IsExtorted = true
                
                ->AfterLift
            
            *{IsAsked == true}{HorsesAround == true}[Demand one of the horses (+1 tier 0 horse, Mercy-)]
                You say that since he is clearly incapable of controlling two horses and therefore should be fine giving you one as payment.
                
                The man, seeing as he has no other option, agrees.
                
                {AddTraitInfluence("Mercy", -20)}
                ~ RewardGive = Give1Horse
                ~ IsExtorted = true
                
                ->AfterLift
        
        //Rob the man
            *{HorsesAround == true}[Take the horses and leave(+2 tier 0 horses, Mercy--)]
                You decide that rather than help the man you would rather go and tame the two horses, as they are clearly wild horses, who in no way have had any previous owner this is perfectly legal.
                
                After you have gotten a handle on the horses and are heading off, you can hear the cries of the trapped man begging you to come back and help, fade into the distance.
                
                {AddTraitInfluence("Mercy", -40)}
                
                {Give2Horse}
                
                ->END
                
        //Necromancer option
            *{PartyCanRaiseDead}[Kill the man, raise his corpse as a skeleton, {HorsesAround == true: take the horses,} and loot his cart (+1 Skeleton, {HorsesAround == true: +2 tier 1 horses, }{RewardText}, Mercy---){RaiseDeadSkillCheckText}]
                
                A brilliant idea comes to your mind. Since the man is clearly worthless as a cart driver, perhaps he can find value by becoming one of your undead minions.
                
                In one swift motion you kill the man and go about raising him as a skeleton. Yout party makes an attempt and {RaiseDeadSkillCheckTest: succeeds| fails}.
                
                {RaiseDeadSkillCheckTest:
                    -true: Having successfully raised the dead, you decide to celebrate by taking all the man's possessions.
                        {ChangePartyTroopCount("tor_vc_skeleton",1)}
                        {RewardGive}
                        {HorseReward}
                    -false: Having failed you decide to take all the dead man's possessions as compensation for wasting your time.
                        {RewardGive}
                        {HorseReward}
                }
                
            ->END

===AfterLift===

    Your party lifts the cart off the man and he {InjuryText3}.

    //Is Injured?
        {InjuryRoll:
            -0: ->Reward
            -else: ->Injury
        }
        
    //Injury
        =Injury
            How will you treat his injury?
                *[Treat him with medicine {MedicineSkillCheckText}]
                    
                    Your best doctor goes to work attempting to fix the man up.
                        {MedicineSkillCheckTest:
                            -true:  ->Success
                            -false: ->Fail
                        }
                
                *{LoreOfLifeInParty}[Treat him with magic {SpellcraftSkillCheckText}]
                    
                    A spellcaster in your party calls upon the winds of Ghyran to mend the man's wounds.
                        {MedicineSkillCheckTest:
                            -true:  ->Success
                            -false: ->Fail
                        }
                    
        =Success
            Your treatment succeeds and the man will now be fine.
                    ~ BonusRoll = RANDOM(0,100)
            ->Reward
            
        =Fail
            Your treatment fails and the man {InjuryText4}.
                {InjuryRoll:
                    -2:
                        ~ ManAlive = false
                }
            ->Reward
            
===Reward===

    {ManAlive:->LiveReward|->DeadReward}

    =LiveReward
        Having been saved, the man {IsExtorted == true: begrudgingly} thanks you for your help{IsExtorted == true: and gives you the promised reward}.
        
        {NotableChange == true: As he starts gathering his things he says, "I will tell {Notable} of your deeds as soon as I am home."}
        
        {IsExtorted == false && BonusRoll >=50: The man pausing for a moment says, "I know I said I didn't have much but please take this ({RewardText}). It's the least I can do for your kindness."}
            
            {RewardGive:
                - IsExtorted == true: {RewardGive}
                - IsExtorted == false && BonusRoll >=50: {RewardGive}
            }
            {NotableChange:
                -true: {ChangeRelations(Notable, 5)}
            }
        
        ->END
        
    =DeadReward
        What will your party do next?
            *[Bury the man (Mercy+)]
                You decide to bury the man, hoping that he can find peace.
                {AddTraitInfluence("Mercy", 40)}
                ->DeadReward
            *[Loot the cart {HorsesAround: and take the horses} ({RewardText}{HorsesAround:, +2 tier 0 horses})]
                Now that the man has passed he obviously will not need the supplies anymore.
                {RewardGive}
                {HorsesAround:
                    -true: {Give2Horse}
                }
                ->DeadReward
            *{PartyCanRaiseDead}[Raise him as a skeleton (+1 skeleton){RaiseDeadSkillCheckText}]
                Since a dead man has no use for his body you decide to raise it as a skeleton.
                
                Yout party makes an attempt and {RaiseDeadSkillCheckTest: succeeds| fails}.
                
                {RaiseDeadSkillCheckTest:
                    -true: The man's body stands up and shambles off to join the rest of your army.
                        {ChangePartyTroopCount("tor_vc_skeleton",1)}
                }
                ->DeadReward
            *[Move along (leave)]
                You decide that it is time to move on and continue your journey.
                ->END
        ->END



-> END
















