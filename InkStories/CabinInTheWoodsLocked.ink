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
        
//Data Import/Export Section
    //Make sure you include this in all ink files to get access to integration functions
        INCLUDE include.ink
        
    //List of Data Being Imported (use this to help keep track of what data you are importing; will help with troubleshooting and testing.)
    
        //Roguery (Highest in the Party)
            //Used for lock picking the door
            VAR PartyRogueryCheckText = 0 //Not important initial value
                //~ PartyRogueryCheckText = print_party_skill_chance("Roguery", LockDifficulty) [Variable Update]
            
            VAR PartyRogueryCheckTest = 0
                //~ PartyRogueryCheckTest = perform_party_skill_check("Roguery", LockDifficulty) [Variable Update]
                
        //Spellcraft (Highest in the Party)
            //Used for breaking down the door
            VAR PartySpellcraftCheckText = 0 //Not important initial value
                //~ PartySpellcraftCheckText = print_party_skill_chance("Spellcraft", DoorDifficulty) [Variable Update]
                
            VAR PartySpellcraftCheckTest = 0 //Not important initial value
                //~ PartySpellcraftCheckTest = perform_party_skill_check("Spellcraft", DoorDifficulty) [Variable Update]
                
        //Engineering (Highest in the Party)
            //Used for disassembling the lock
            VAR PartyEngineeringCheckText = 0 //Not important initial value
                //~ PartyEngineeringCheckText = print_party_skill_chance("Engineering", LockDifficulty) [Variable Update]
                
            VAR PartyEngineeringCheckTest = 0 //Not important initial value
                //~ PartyEngineeringCheckTest = perform_party_skill_check("Engineering", LockDifficulty) [Variable Update]
                
        //Party has a spell caster
            VAR PartyCanCastSpell = false
                ~ PartyCanCastSpell = PartyHasSpellcaster(false)
                
        //Vigor (Highest in the Party):
            VAR PartyVigorCheckText = 0
               // ~ PartyVigorCheckText = print_party_attribute_chance(30*"Vigor", DoorDifficulty) [Variable Update]

            VAR PartyVigorCheckTest = 0
               // ~ PartyVigorCheckTest = perform_party_attribute_check(30*"Vigor", DoorDifficulty) [Variable Update]
        
    //Data Exported (use this to help keep track of what data you are exporting; will help with troubleshooting and testing.)

        //GiveItem
            //Used to give a reward for successfully completing the scenario
            
        //GiveGold
            //Used to give gold if the player successfully completes the scenario
        
//Variables setup
    //IMPORTANT! Initial values are mandatory, but they can only be primitives (number, string, boolean). If we want to assign the return value of a function to the variable, we must do it on a separate line, see one line below

    //Seed
        //~ SEED_RANDOM(100) //Uncomment to lock an RNG testing seed for the randomness. Change number inside () for different seed
        
    //Lock:
        VAR LockQuality = 0 //Not important initial value
            ~ LockQuality = RANDOM(1,3)
            
        VAR LockDifficulty = 0 //Not important initial value
            ~ LockDifficulty = LockQuality*50
            
        VAR LockText = "" //Not important initial value
            {
                - LockQuality == 1:
                    ~ LockText = "weak"
                - LockQuality == 2:
                    ~ LockText = "average"
                - LockQuality == 3:
                    ~ LockText = "strong"
            }
            
    //DoorQuality:
        VAR DoorQuality = 0 //Not important initial value
            ~ DoorQuality = RANDOM(1,3)
            
        VAR DoorDifficulty = 0 //Not important initial value
            ~ DoorDifficulty = DoorQuality*50

        VAR DoorText = "" //Not important initial value
            {
                - DoorQuality == 1:
                    ~ DoorText = "weak"
                - DoorQuality == 2:
                    ~ DoorText = "average"
                - DoorQuality == 3:
                    ~ DoorText = "strong"
            }

    //Reward
        VAR RewardRoll = 0 //Not important initial value
           ~ RewardRoll = RANDOM(0,2)
           
        VAR RewardText = "" //Not important initial value
            {
                - RewardRoll == 0:
                    ~ RewardText = "5 grain"
                - RewardRoll == 1:
                    ~ RewardText = "2 steel ingots"
                - RewardRoll == 2:
                    ~ RewardText = "500 gold"
            }
            
        //Reward payout prepared ahead
            VAR RewardPayout = 0
            VAR RewardItemID = ""
            VAR RewardItemCount= 0
                {
                    - RewardRoll == 0:
                        ~ RewardItemID = "grain"
                        ~ RewardItemCount = 5
                        ~ RewardPayout = GiveItem(RewardItemID,RewardItemCount)
                    - RewardRoll == 1:
                        ~ RewardItemID = "ironIngot4"
                        ~ RewardItemCount = 2
                        ~ RewardPayout = GiveItem(RewardItemID,RewardItemCount)
                    - RewardRoll == 2:
                        ~ RewardPayout = GiveGold(500)
                }
            
 //Variable Update: Update any variables before story start
    ~ PartyRogueryCheckText = print_party_skill_chance("Roguery", LockDifficulty)
    ~ PartyRogueryCheckTest = perform_party_skill_check("Roguery", LockDifficulty)
    
    ~ PartySpellcraftCheckText = print_party_skill_chance("Spellcraft", DoorDifficulty)
    ~ PartySpellcraftCheckTest = perform_party_skill_check("Spellcraft", DoorDifficulty)           

    ~ PartyEngineeringCheckText = print_party_skill_chance("Engineering", LockDifficulty)
    ~ PartyEngineeringCheckTest = perform_party_skill_check("Engineering", LockDifficulty)

    ~ PartyVigorCheckText = print_party_attribute_chance("Vigor", DoorDifficulty / 30)
    ~ PartyVigorCheckTest = perform_party_attribute_check("Vigor", DoorDifficulty / 30)

//Variable Check (Use for sanity check. Uncomment variables to see what they are)


-> Start

===Start===
    As your party is travelling along you come across a cabin in the woods.
    
    *[Approach the cabin]->Approach
    *[Go on your way (Leave)]You decide it is better to move on for now.->END
    
===Approach===

As you approach the cabin you can see that it is heavily boarded up. The only door on the cabin seems to be locked tight. As you examine the door you see that the door is {DoorText} and that the lock on it is {LockText}.->choice1

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

    *[Go on your way(Leave)]You decide it is better to move on for now.->END

===Inside===

Your party gets inside the cabin and find that someone or something has stored some supplies here.->choice2

    =choice2
        *[Take the supplies ({RewardText})]
            You take the {RewardText} and add it to your supplies before continuing on your way.
            {RewardPayout:
                -else:{RewardPayout}
            }
            ->END
        
        *[Leave]You decide to leave the supplies and head on your way.->END

-> END