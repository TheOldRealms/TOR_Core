//Global story tags
# title: Cabin In The Woods
# frequency: Common
# development: false
# illustration: roadpoint2


//Important Irregular Characters
    //| (Vertical Bar)

//Scenarios notes
    //Rarity: COMMON
    //Repeatable: YES
    
    //Restrictions
    
    //Triggers:
        //While Travelling on the campaign map
        //After clearing a random bandit camp
        //Quests:
            //Bandit Bounty quest
    
    //Scenario Explanation (explain the main scenario and any major variations that you are planning to build in. If a variation is different enough consider making it its own file.)
    
        //Main: Party comes across a locked cabin. They must find a way in.

        //Alt1: Ambush by hostile party [Not Implemented]
        //Alt2: Dungeon hidden inside [Not Implemented]
        
        
    //Future Options/Additions
        //Faith check if party has a priest of Ranald
        //Someone answers when you knock on the door
        //Burn down the cabin
        //Hidden items in the cabin
            //Different ways item can be hidden (ex magically hidden, trap door)

INCLUDE include.ink

//Variables setup
            
        VAR PartyRogueryCheckText = 0
        VAR PartyRogueryCheckTest = 0
        VAR PartySpellcraftCheckText = 0
        VAR PartySpellcraftCheckTest = 0
        VAR PartyEngineeringCheckText = 0
        VAR PartyEngineeringCheckTest = 0
        VAR PartyCanCastSpell = false
        VAR PartyVigorCheckText = ""
        VAR PartyVigorCheckTest = 0
        
    VAR LockQuality = 0
        ~ LockQuality = RANDOM(1,3)
            
    VAR LockDifficulty = 0
        ~ LockDifficulty = LockQuality * 50
            
    VAR LockText = ""
        {
            - LockQuality == 1:
                ~ LockText = "weak"
            - LockQuality == 2:
                ~ LockText = "average"
            - LockQuality == 3:
                ~ LockText = "strong"
        }
        
    ~ SetTextVariable("LockText",LockQuality)
            
    VAR DoorQuality = 0
        ~ DoorQuality = RANDOM(1,3)
            
    VAR DoorDifficulty = 0
        ~ DoorDifficulty = DoorQuality * 50

    VAR DoorText = ""
        {
            - DoorQuality == 1:
                ~ DoorText = "weak"
            - DoorQuality == 2:
                ~ DoorText = "average"
            - DoorQuality == 3:
                ~ DoorText = "strong"
        }

    ~ SetTextVariable("DoorText",DoorQuality)
    //Reward
        VAR RewardRoll = 0
           ~ RewardRoll = RANDOM(0,2)
           
        VAR RewardText = ""
            {
                - RewardRoll == 0:
                    ~ RewardText = "5 grain"
                - RewardRoll == 1:
                    ~ RewardText = "2 steel ingots"
                - RewardRoll == 2:
                    ~ RewardText = "500 gold"
            }
            
    ~ SetTextVariable("RewardText",RewardRoll)
            
 //Variable Update: Update any variables before story start
    ~ PartyRogueryCheckText = print_party_skill_chance("Roguery", LockDifficulty)
    ~ PartyRogueryCheckTest = perform_party_skill_check("Roguery", LockDifficulty)
    
    ~ PartySpellcraftCheckText = print_party_skill_chance("Spellcraft", DoorDifficulty)
    ~ PartySpellcraftCheckTest = perform_party_skill_check("Spellcraft", DoorDifficulty)           

    ~ PartyEngineeringCheckText = print_party_skill_chance("Engineering", LockDifficulty)
    ~ PartyEngineeringCheckTest = perform_party_skill_check("Engineering", LockDifficulty)

    ~ PartyVigorCheckText = print_party_attribute_chance("Vigor", DoorDifficulty / 30)
    ~ PartyVigorCheckTest = perform_party_attribute_check("Vigor", DoorDifficulty / 30)


-> Start

===Start===
    As your party is travelling along you come across a cabin in the woods. #STR_Start1
    
    *[Approach the cabin]->Approach
    *[Go on your way (Leave)]You decide it is better to move on for now.->END
    
===Approach===

As you approach the cabin you can see that it is heavily boarded up. The only door on the cabin seems to be locked tight. As you examine the door you see that the door is {DoorText} and that the lock on it is {LockText}. #STR_Approach1
->choice1  

    =choice1
    What will your party do?
    *[Knock on the door]You knock but no one answers.->Approach.choice1
    
    //Pick the lock (Roguery)
        *[Pick the lock on the door {PartyRogueryCheckText}]
            Your party's best "rogue" attempts to pick the lock.
            {PartyRogueryCheckTest: Your party succeeds in getting through the lock. ->Inside | Your party fails to pick the lock. ->Approach.choice1}
        
    //Disassemble the Lock (Engineering)
        *[Disassemble the lock {PartyEngineeringCheckText}]
            Your party's best engineer attempts to disassemble the lock.
            {PartyEngineeringCheckTest: Using a selection of their finest tools including screwdrivers, chisels, and a sledgehammer; your engineer masterfully disassembles the lock, so "thorough" is the disassembly that the lock will never be put back together. ->Inside | Your party fails to disassemble the lock. ->Approach.choice1}
    
    //Blow up the door (Spellcraft)
        *{PartyCanCastSpell == true}[Blow up the door {PartySpellcraftCheckText}]
            Your party's best mage attempts to blow up the door with magic.
            {PartySpellcraftCheckTest: Your party blows the door clean off its hinges. ->Inside |Your party fails to blow up the door. ->Approach.choice1}
            
    //Break down the door (Vigor)
        *[Break down the door {PartyVigorCheckText}]
            Your party's strongest member attempts to break down the door.
            {PartyVigorCheckTest: Your party bashes the door clean off its hinges. ->Inside |Your party fails to break down the door. ->Approach.choice1}

    *[Go on your way (Leave)]You decide it is better to move on for now.->END

===Inside===

Your party gets inside the cabin and find that someone or something has stored some supplies here. #STR_Inside1
->choice2 

    =choice2
        *[Take the supplies ({RewardText})]
            You take the {RewardText} and add it to your supplies before continuing on your way.
            {RewardRoll:
                -0: 
                    ~ GiveItem("grain",5)
                -1: 
                    ~ GiveItem("ironIngot4", 2)
                -2: 
                    ~ GiveGold(500)
            }
            ->END
        
        *[Leave]You decide to leave the supplies and head on your way.->END
