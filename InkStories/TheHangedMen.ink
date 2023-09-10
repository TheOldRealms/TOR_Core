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

INCLUDE include.ink

//Variables setup

    //Party can raise departed
        VAR PartyCanRaiseDead = false
            ~ PartyCanRaiseDead = PartyHasNecromancer(false)
                
    //Spellcraft (Highest In Party)
        VAR PartySpellcraftCheckText = 0 //Not important initial value
            ~ PartySpellcraftCheckText = print_party_skill_chance("Spellcraft", RaiseDeadDifficulty)
                
        VAR PartySpellcraftCheckTest = 0 //Not important initial value
            ~ PartySpellcraftCheckTest = perform_party_skill_check("Spellcraft", RaiseDeadDifficulty)
                
    //Give Items
        VAR HaveSword = false
        VAR TookSword = false
        VAR LootedBody = false

    //Raise Dead
        VAR RaiseDeadDifficulty = 50
        VAR SkeletonSuccess = false

    //Grave Interaction
        VAR DugUpGrave = false
        VAR CryptGuardSuccess = false


-> Start

===Start===
    You come across a tree with three men hanging from it with a sword stuck in the ground beneath them. As you get closer you can see that the word "Traitors" is etched into the tree and that the sword has been used to mark a grave. ->choice1

    //What to do with the hanging bodies
    =choice1
        What will your party do with the hanging bodies?
        
            *[Do nothing]
                You decide to do nothing with the hanging bodies.
                ->choice1to2Intermission
        
            *[Bury the hanging bodies (Mercy+)]
                You cut down the bodies and lay them to rest.
                ~ AddTraitInfluence("Mercy", 20)
                ->choice1to2Intermission
        
            *[Loot the hanging bodies (Mercy-)]
                You cut down the bodies and loot the corpses, taking the tattered rags they were executed in.
                ~ AddTraitInfluence("Mercy", -20)
                ~ GiveItem("wrapped_headcloth",3)
                ~ GiveItem("ragged_robes",3)
                ~ GiveItem("leather_shoes",3)
                ->choice1to2Intermission
            
        //Raise the hanging bodies as skeletons
            *{PartyCanRaiseDead}[Raise the hanging bodies as skeletons (Mercy--) {print_party_skill_chance("Spellcraft", RaiseDeadDifficulty)}]
                ~ AddTraitInfluence("Mercy", -50)
                {perform_party_skill_check("Spellcraft", RaiseDeadDifficulty):
                    -true:
                        ~ ChangePartyTroopCount("tor_vc_skeleton",3)
                        ~ SkeletonSuccess = true
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
            
        *[Offer a prayer (Mercy+)]
            You say a prayer for the departed hoping they can find peace.
            ~ AddTraitInfluence("Mercy", 20)
            ->Leave


        *[Take the sword (1 tier 3 sword, Mercy-)]
            You take the sword into your hands.
            ~ AddTraitInfluence("Mercy", -20)
            ~ HaveSword = true
            ~ TookSword = true
            ->choice2
            
        *[Dig up the grave (Mercy-)]
            You dig up the grave to find a warrior buried in some armour. You can see some of the armour is damaged, most likely from the "traitors".
            ~ AddTraitInfluence("Mercy", -20)
            ~ DugUpGrave = true
            ->choice2
        
        *{DugUpGrave == true}[Loot the buried body (2 pieces of tier 3 armour, Mercy-)]
            You strip the body of all the armour that is still intact.
            ~LootedBody = true
            ~AddTraitInfluence("Mercy", -20)
            
                //Loot Rolls
                    {RANDOM(0,1):
                        -0: 
                            ~GiveItem("roundkettle_over_imperial_leather",1)
                        -1: 
                            ~GiveItem("imperial_padded_cloth",1)
                    }
                    {RANDOM(0,1):
                        -0: 
                            ~GiveItem("mail_mitten",1)
                        -1: 
                            ~GiveItem("mail_chausses",1)
                    }

            ->choice2
            
        *{DugUpGrave && PartyCanRaiseDead && not LootedBody}[Resurrect the buried body as a wight (+1 Crypt Guard, Mercy--) {print_party_skill_chance("Spellcraft", RaiseDeadDifficulty)}]
            ~AddTraitInfluence("Mercy", -50)
                
                //Raise Dead
                    {perform_party_skill_check("Spellcraft", RaiseDeadDifficulty):
                        -true:
                            ~ ChangePartyTroopCount("tor_vc_crypt_guard",1)
                            ~ CryptGuardSuccess = true
                            ~ HaveSword = false
                        -false:
                    }

                Your party attempts to resurrect the corpse as a wight {CryptGuardSuccess: and succeed. The wight stands up {TookSword: and holds out its hand as if to ask for its sword back. You give back the weapon} then it marches off to join the rest of your forces. ->Leave | and fail.->choice2}
            ->Leave

===Leave===
    Having made your decisions you go on your way.
    {HaveSword: 
        ~GiveItem("vlandia_sword_1_t2",1)
    }
-> END















