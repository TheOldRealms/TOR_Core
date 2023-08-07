//Global story tags
# title: The Hanged Men
# frequency: Common
# development: false
# illustration: hangedman

//Important Irregular Characters
    //| (Vertical Bar)

//Scenarios notes
    //Rarity: COMMON
    //Repeatable: YES
    
    //Triggers:
        //While Travelling on Campaign map
    
    //Scenario Explanation (explain the main scenario and any major variations that you are planning to build in. If a variation is different enough consider making it its own file.)
    
        //Main: You come across a bunch of hanged men with a sword in the ground underneath them. There is a body buried under the sword.

        //Alt:
        
    //Future Options/Additions
        //Add ability to gain relations or gain faith skill for people who have a death god (ex. Morr)
        //Remove certain choices if the player is not Order (Undead, Chaos, Greenskin)
        //Make sure the spellcraft skill used for raise dead comes from a necromancer in the party
        //Add in a murder mystery available by speaking to the dead
            //Necro can make Spirit hosts instead of zombies
        //Take skulls option for chaos
        //Change skeleton to zombie
        //Defile corpses
        
//Data Import/Export Section
    //Make sure you include this in all ink files to get access to integration functions
        INCLUDE include.ink
        
    //List of Data Being Imported (use this to help keep track of what data you are importing; will help with troubleshooting and testing.)
    
        //Party can raise departed
            VAR PartyCanRaiseDead = false
                ~ PartyCanRaiseDead = PartyHasNecromancer(false)
                
        //Spellcraft (Highest In Party)
            VAR PartySpellcraftCheckText = 0 //Not important initial value
                //~ PartySpellcraftCheckText = print_party_skill_chance("Spellcraft", RaiseDeadDifficulty) [Variable Update]
                
            VAR PartySpellcraftCheckTest = 0 //Not important initial value
                //~ PartySpellcraftCheckTest = perform_party_skill_check("Spellcraft", RaiseDeadDifficulty) [Variable Update]
        
    //Data Exported (use this to help keep track of what data you are exporting; will help with troubleshooting and testing.)
        
        //Mercy change
            //AddTraitInfluence("Mercy", changeByAmount)
            
        //Give Items
            //Rags
            //Sword
                VAR HaveSword = false
                VAR TookSword = false
                
            //Armour
                VAR LootedBody = false
            
//Variables setup
    //IMPORTANT! Initial values are mandatory, but they can only be primitives (number, string, boolean). If we want to assign the return value of a function to the variable, we must do it on a separate line, see one line below

    //Seed
        //~ SEED_RANDOM(100) //Uncomment to lock an RNG testing seed for the randomness. Change number inside () for different seed
        
    //Raise Dead
        VAR RaiseDeadDifficulty = 2 //Must start at least greater than 1
            //Skeleton
                //Spellcraft >= 25 for 100% chance
                VAR SkeletonSuccess = false
            //Skeleton Crypt Guard
                //Spellcraft >= 100 for 100% chance
                VAR CryptGuardSuccess = false

        
    //Grave Interaction
        VAR DugUpGrave = false


//Variable Update
    //~ PartySpellcraftCheckText = print_party_skill_chance("Spellcraft", RaiseDeadDifficulty)
    //~ PartySpellcraftCheckTest = perform_party_skill_check("Spellcraft", RaiseDeadDifficulty)


//Variable Check (Use for sanity check. Uncomment variables to see what they are)


-> Start

===Start===
    You come across a tree with three men hanging from it with a sword stuck in the ground beneath them. As you get closer you can see that the word "Traitors" is etched into the tree and that the sword has been used to mark a grave. ->choice1

    //What to do with the hanging bodies
    =choice1
            //Variable update
            ~ RaiseDeadDifficulty = 25

        What will your party do with the hanging bodies?
        
        //Do nothing
            *[Do nothing]
                You decide to do nothing with the hanging bodies.
                
            ->choice1to2Intermission
        
        //Cut down the bodies and bury them
            *[Bury the hanging bodies (Mercy+)]
                You cut down the bodies and lay them to rest.
                    
                    //Mercy change
                        {AddTraitInfluence("Mercy", 20)}
            
            ->choice1to2Intermission
        
        //Cut down the bodies and loot them
            *[Loot the hanging bodies (3 sets of ragged clothes, Mercy-)]
                You cut down the bodies and loot the corpses, taking the tattered rags they were executed in.
                
                    //Mercy change 
                        {AddTraitInfluence("Mercy", -20)}
                
                    //Give Loot (Rags)
                        {GiveItem("wrapped_headcloth",3)}
                        {GiveItem("ragged_robes",3)}
                        {GiveItem("leather_shoes",3)}
            ->choice1to2Intermission
            
        //Raise the hanging bodies as skeletons
            *{PartyCanRaiseDead == true}[Raise the hanging bodies as skeletons (+3 zombies, Mercy--){print_party_skill_chance("Spellcraft", RaiseDeadDifficulty)}]
                
                //Mercy change 
                    {AddTraitInfluence("Mercy", -50)}  
                
                //Raise Dead skeletons
                    {perform_party_skill_check("Spellcraft", RaiseDeadDifficulty):
                        -true:
                            {ChangePartyTroopCount("tor_vc_skeleton",3)}
                            ~ SkeletonSuccess = true
                        -false:
                    }
                
                
                
                Your party attempts to resurrect the corpses as skeletons {SkeletonSuccess: and succeeds. ->choice1to2Intermission | and fails.->choice1}


    //Needed for intermission text 
    =choice1to2Intermission
        Having decided what to do with the hanging bodies you turn your attention to the grave marked by the sword.
        ->choice2
        
    //What to do with the buried body
    =choice2
            //Variable Update
                ~ RaiseDeadDifficulty = 100
        What will you do with the grave?
        
        *[Leave this place (Leave)]
            ->Leave
            
        //Offer a Prayer
        *[Offer a prayer (Mercy+)]
            You say a prayer for the departed hoping they can find peace.
            
                //Mercy change 
                    {AddTraitInfluence("Mercy", 20)}
                
            ->Leave


        *[Take the sword (1 tier 3 sword, Mercy-)]
            You take the sword into your hands.
            
                //Mercy change 
                    {AddTraitInfluence("Mercy", -20)}
                    
                //Have Sword change
                    ~ HaveSword = true
                    ~ TookSword = true
            ->choice2
            
        *[Dig up the grave (Mercy-)]
            You did up the grave to find a warrior buried in some armour. You can see some of the armour is damaged, most likely from the "traitors".
            
                //Mercy change 
                    {AddTraitInfluence("Mercy", -20)}
                    
                //Dug up grave
                    ~ DugUpGrave = true
            ->choice2
        
        *{DugUpGrave == true}[Loot the buried body (2 pieces of tier 3 armour, Mercy-)]
            You strip the body of all the armour that is still intact.
            
                ~LootedBody = true
                
                //Mercy change 
                    {AddTraitInfluence("Mercy", -20)}
                    
                //Loot Rolls
                    {RANDOM(0,1):
                        -0: {GiveItem("roundkettle_over_imperial_leather",1)}
                        -1: {GiveItem("imperial_padded_cloth",1)}
                    }
                    {RANDOM(0,1):
                        -0: {GiveItem("mail_mitten",1)}
                        -1: {GiveItem("mail_chausses",1)}
                    }

            ->choice2
        *{DugUpGrave == true}{PartyCanRaiseDead == true}{LootedBody == false}[Resurrect the buried body as a wight (+1 Crypt Guard, Mercy--){print_party_skill_chance("Spellcraft", RaiseDeadDifficulty)}]
        
                //Mercy change 
                    {AddTraitInfluence("Mercy", -50)}  
                
                //Raise Dead skeletons
                    {perform_party_skill_check("Spellcraft", RaiseDeadDifficulty):
                        -true:
                            {ChangePartyTroopCount("tor_vc_skeleton_crypt_guard",1)}
                            ~ CryptGuardSuccess = true
                            ~ HaveSword = false
                        -false:
                    }
                
                
                
                Your party attempts to resurrect the corpse as a wight {CryptGuardSuccess: and succeed. The wight stands up {TookSword: and holds out its hand as if to ask for its sword back. You give back the weapon} then it marches off to join the rest of your forces. ->Leave | and fail.->choice2}

            ->Leave
===Leave===
    Having made your decisions you go on your way.
    {HaveSword == true: {GiveItem("vlandia_sword_1_t2",1)}}

-> END















