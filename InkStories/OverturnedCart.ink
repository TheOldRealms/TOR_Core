//Global story tags
# title: Roadside Accident
# frequency: Common
# development: false
# illustration: cart_accident

INCLUDE include.ink

        VAR InjuryDifficulty = 2
            {InjuryRoll:
                -1: 
                    ~InjuryDifficulty = 100
                -2: 
                    ~InjuryDifficulty = 250
            }
        
        VAR Settlement = ""
            ~ Settlement = GetNearestSettlement("town")
                
        VAR Notable = ""
            ~ Notable = GetRandomNotableFromSpecificSettlement(Settlement)
                
        VAR NotableChange = false
                
        VAR PartyCanRaiseDead = false
            ~ PartyCanRaiseDead = PartyHasNecromancer(false)
                
        VAR RaiseDeadSkillCheckText = ""
            ~ RaiseDeadSkillCheckText = print_party_skill_chance("Spellcraft", 25)
                
        VAR RaiseDeadSkillCheckTest = false
            ~ RaiseDeadSkillCheckTest = perform_party_skill_check("Spellcraft", 25)
                
        VAR MedicineSkillCheckText = ""
            ~ MedicineSkillCheckText = print_party_skill_chance("Medicine", InjuryDifficulty)
                
        VAR MedicineSkillCheckTest = false
            ~ MedicineSkillCheckTest = perform_party_skill_check("Medicine", InjuryDifficulty)
                
        VAR SpellcraftSkillCheckText = ""
            ~ SpellcraftSkillCheckText = print_party_skill_chance("Spellcraft", InjuryDifficulty)
                
        VAR SpellcraftSkillCheckTest = false
            ~ SpellcraftSkillCheckTest = perform_party_skill_check("Spellcraft", InjuryDifficulty)
                
        VAR LoreOfLifeInParty = false
                ~ LoreOfLifeInParty = DoesPartyKnowSchoolOfMagic(false, "LoreOfLife")

        VAR InjuryRoll = 2
            ~ InjuryRoll = RANDOM(0,2)
            
        VAR InjuryText1 = ""
            {InjuryRoll:
                -0: 
                    ~InjuryText1 = "uninjured"
                -1: 
                    ~InjuryText1 = "mildly injured"
                -2: 
                    ~InjuryText1 = "severely injured"
            }
        
        VAR InjuryText2 = ""
            {InjuryRoll:
                -0: 
                    ~InjuryText2 = "asks"
                -1: 
                    ~InjuryText2 = "begs"
                -2: 
                    ~InjuryText2 = "gasps"
            }
        
        VAR InjuryText3 = ""
            {InjuryRoll:
                -0: 
                    ~InjuryText3 = "gets up"
                -1: 
                    ~InjuryText3 = "barely gets up"
                -2: 
                    ~InjuryText3 = "lays there trying not to die"
            }
            
        VAR InjuryText4 = ""
            {InjuryRoll:
                -0: 
                    ~InjuryText4 = ""
                -1: 
                    ~InjuryText4 = "seems to get a bit depressed knowing that he will be crippled for at least some time"
                -2: 
                    ~InjuryText4 = "dies"
            }
    
        VAR HorsesAround = 0
            ~HorsesAround = RANDOM(0,1)

        //Ask for info
        VAR HasAsked = false
        
        //Profession of the stuck man
        VAR ProfessionRoll = 0
            ~ ProfessionRoll = RANDOM(0,2)
            
        VAR Profession = ""
            {ProfessionRoll:
                -0: 
                    ~Profession = "merchant"
                -1: 
                    ~Profession = "farmer"
                -2: 
                    ~Profession = "blacksmith"
            }
        
        VAR RewardText = ""
            {ProfessionRoll:
                -0: 
                    ~RewardText = "500 gold"
                -1: 
                    ~RewardText = "5 grain"
                -2: 
                    ~RewardText = "2 steel ingots"
            }

        VAR HasExtorted = false
        
        //Bonus Reward
        VAR BonusRoll = 0

        VAR ManAlive = true

-> Start

===Start===
    As your party is travelling along you see a cart in the distance.
    As you get closer you can see that it had broken down and tipped over. 
    {HorsesAround: You can also see some horses grazing on grass in a nearby field, presumably these were pulling the cart prior to the incident.}

    *[Approach the cart]->Approach
    *[Go on your way] You decide to ignore the overturned cart and continue your journey. ->END

===Approach===

    You approach the cart and find a man stuck underneath. When he sees you approaching he calls out for help.
    You notice that the man trapped under the cart is {InjuryText1}.
    As you get close he {InjuryText2} to you, "Please help me". 
    What will you do?
    ->choices
    
    =choices
        *[Ask what he can do for you if you help him]
            You ask the man what he can do for you.
            The man replies, "I am just a simple {Profession} from {Settlement}, I cannot give you a reward other than my thanks."
            After a moment he says, "I am a friend of {Notable} and I will put in a good word for you."
            While he is talking you can't help but notice there still seems to be some cargo in the cart.
            ~HasAsked = true
            ->choices
        
            *{not HasAsked}[Help him (Mercy++)]
                You decide to help him.
                ~ AddTraitInfluence("Mercy", 40)
                ->AfterLift
                
            *{HasAsked}[Help him (+Relations with {Notable}, Mercy+)]
                You decide to help him.
                ~ AddTraitInfluence("Mercy", 20)
                ~ NotableChange = true
                ->AfterLift
        
            *{HasAsked}[Extort him for a reward (Mercy-)]
                You tell the {Profession} that he shouldn't be so modest. He is clearly a man of some means and can easily spare {RewardText} as compensation for the assistance.
                The man, believing he has no other option, agrees.
                ~ AddTraitInfluence("Mercy", -20)
                ~ HasExtorted = true
                ->AfterLift
            
            *{HasAsked && HorsesAround}[Demand one of the horses (Mercy-)]
                You say that since he is clearly incapable of controlling two horses and therefore should be fine giving you one as payment.
                The man, seeing as he has no other option, agrees.
                ~ AddTraitInfluence("Mercy", -20)
                ~ HasExtorted = true
                ->AfterLift
        
            *{HorsesAround}[Take the horses and leave (Mercy--)]
                You decide that rather than help the man you would rather go and tame the two horses, as they are clearly wild horses, who in no way have had any previous owner this is perfectly legal.
                After you have gotten a handle on the horses and are heading off, you can hear the cries of the trapped man begging you to come back and help, fade into the distance. 
                ~ AddTraitInfluence("Mercy", -40)
                ~ GiveItem("old_horse",2)
                ->END
                
        //Necromancer option
            *{PartyCanRaiseDead}[Kill the man, raise his corpse as a skeleton, {HorsesAround: take the horses,} and loot his cart (Mercy---) {RaiseDeadSkillCheckText}]
                A brilliant idea comes to your mind. Since the man is clearly worthless as a cart driver, perhaps he can find value by becoming one of your undead minions.
                In one swift motion you kill the man and go about raising him as a skeleton. Your party makes an attempt and {RaiseDeadSkillCheckTest: succeeds| fails}.
                {RaiseDeadSkillCheckTest: -> raiseSucceed | -> raiseFail}
    
    =raiseSucceed
    Having successfully raised the dead, you decide to celebrate by taking all the man's possessions.
        {ProfessionRoll:
            -0: 
                ~GiveGold(500)
            -1: 
                ~GiveItem("grain", 5)
            -2: 
                ~GiveItem("ironIngot4", 2)
        }
        {HorsesAround: {GiveItem("old_horse",2)}}
        ~ ChangePartyTroopCount("tor_vc_skeleton",1)
        -> END
    
    =raiseFail
    Having failed you decide to take all the dead man's possessions as compensation for wasting your time.
        {ProfessionRoll:
            -0: 
                ~GiveGold(500)
            -1: 
                ~GiveItem("grain", 5)
            -2: 
                ~GiveItem("ironIngot4", 2)
        }
        {HorsesAround: {GiveItem("old_horse",2)}}
        -> END

===AfterLift===
    Your party lifts the cart off the man and he {InjuryText3}.

    //Is Injured?
        {InjuryRoll:
            -0:     ->Reward
            -else:  ->Injury
        }

        =Injury
            How will you treat his injury?
                *[Treat him with medicine {MedicineSkillCheckText}]
                    Your best doctor goes to work attempting to fix the man up.
                        {MedicineSkillCheckTest: ->Success | ->Fail}
                        
                *{LoreOfLifeInParty}[Treat him with magic {SpellcraftSkillCheckText}]
                    A spellcaster in your party calls upon the winds of Ghyran to mend the man's wounds.
                        {SpellcraftSkillCheckTest: ->Success | ->Fail}
                    
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
        Having been saved, the man {HasExtorted: begrudgingly} thanks you for your help{HasExtorted: and gives you the promised reward}.
        {NotableChange: As he starts gathering his things he says, "I will tell {Notable} of your deeds as soon as I am home."}
        {HasExtorted == false && BonusRoll >=50: The man pausing for a moment says, "I know I said I didn't have much but please take this ({RewardText}). It's the least I can do for your kindness."}
        
        {HasExtorted || (not HasExtorted && BonusRoll >=50):
            -true:
                {ProfessionRoll:
                    -0: 
                        ~GiveGold(500)
                    -1: 
                        ~GiveItem("grain", 5)
                    -2: 
                        ~GiveItem("ironIngot4", 2)
                }
        }
        {NotableChange: {ChangeRelations(Notable, 5)}}
        ->END
        
    =DeadReward
        What will your party do next?
            *[Bury the man (Mercy+)]
                You decide to bury the man, hoping that he can find peace.
                {AddTraitInfluence("Mercy", 40)}
                ->DeadReward
            *[Loot the cart {HorsesAround: and take the horses} ({RewardText}{HorsesAround:, +2 tier 0 horses})]
                Now that the man has passed he obviously will not need the supplies anymore.
                {ProfessionRoll:
                    -0: 
                        ~GiveGold(500)
                    -1: 
                        ~GiveItem("grain", 5)
                    -2: 
                        ~GiveItem("ironIngot4", 2)
                }
                {HorsesAround: {GiveItem("old_horse",2)}}
                ->DeadReward
            *{PartyCanRaiseDead}[Raise him as a skeleton (+1 skeleton){RaiseDeadSkillCheckText}]
                Since a dead man has no use for his body you decide to raise it as a skeleton.
                Yout party makes an attempt and {RaiseDeadSkillCheckTest: succeeds| fails}.
                
                {RaiseDeadSkillCheckTest:
                    -true: The man's body stands up and shambles off to join the rest of your army.
                        ~ ChangePartyTroopCount("tor_vc_skeleton",1)
                }
                ->DeadReward
            *[Move along (leave)]
                You decide that it is time to move on and continue your journey.
                ->END
