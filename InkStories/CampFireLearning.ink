//Global story tags
# title: The Campfire
# frequency: Special
# development: false
# illustration: campfirenight

//Important Irregular Characters
    //| (Vertical Bar)

//Scenarios notes
    //Rarity: COMMON
    //Repeatable: YES
    
    //Triggers:
        //While traveling on the campaign map
    
    //Scenario Explanation (explain the main scenario and any major variations that you are planning to build in. If a variation is different enough consider making it its own file.)
    
        //Main: You are around a campfire and can listen to a story to gain xp or tell your men to rest.

        //Alt: You can get ambushed
        
    //Future Options/Additions
        //More possible variants in groupings
        //Magic and Faith XP options when proper restictions are in place
        
//Data Import/Export Section
    //Make sure you include this in all ink files to get access to integration functions
        INCLUDE include.ink
        
    //List of Data Being Imported (use this to help keep track of what data you are importing; will help with troubleshooting and testing.)
    
        //
        
    //Data Exported (use this to help keep track of what data you are exporting; will help with troubleshooting and testing.)
        
        //Skill XP
        
//Variables setup
    //IMPORTANT! Initial values are mandatory, but they can only be primitives (number, string, boolean). If we want to assign the return value of a function to the variable, we must do it on a separate line, see one line below

    //Seed
        //~ SEED_RANDOM(100) //Uncomment to lock an RNG testing seed for the randomness. Change number inside () for different seed
        
    //Learning sets (The groupings of exp by campfire story category)
        //Each option gives 3000 Xp total. So if a story has 2 skills attached they each get 1500 Xp. For 3 it is 1000 for each.
        //Grouping 1: War stories
            //1. The Hunt (Scouting, Random ranged weapon skill, Tactics)
            //2. The Ambush (Leadership, Tactics, Roguery)
            //3. The Charge (Riding, Polearm, Leadership)
            //4. Holding the Line (Random Melee skill, Leadership, Tactics)
            //5. The Brawl (All melee skills)
            //6. The Shootout (All ranged weapon skills)
        //Grouping 2: Talk about
            //1. Great Rulers (Steward, Leadership, Charm)
            //2. Craftsman (Smithing and Engineering)
            //3. Negotiation (Charm, Trade, Roguery)
            //4. Traveling (Riding and Athletics)
            //5. Survival (Medicine, Scouting, Athletics)
            
    //Random Selections
        //Melee Weapon
            VAR MeleeWeaponRandom = 0
                ~ MeleeWeaponRandom = RANDOM(1,3)
            VAR MeleeWeaponText = ""
                
                {MeleeWeaponRandom:
                    -1:
                        ~ MeleeWeaponText = "One Handed"
                    -2:
                        ~ MeleeWeaponText = "Two Handed"
                    -3:
                        ~ MeleeWeaponText = "Polearm"
                }
                

        //Ranged
            VAR RangedWeaponRandom = 0
                ~ RangedWeaponRandom = RANDOM(1,4)
            VAR RangedWeaponText = ""
                
                {RangedWeaponRandom:
                    -1:
                        ~ RangedWeaponText = "Bow"
                    -2:
                        ~ RangedWeaponText = "Crossbow"
                    -3:
                        ~ RangedWeaponText = "Throwing"
                    -4:
                        ~ RangedWeaponText = "Gunpowder"
                }
    
    //Group 1
        VAR StoryName = ""
        VAR StoryBranch = ""
        VAR StoryXpText = ""
        
        VAR StorySelect = 0
            ~ StorySelect = RANDOM(1,6)
            
            {StorySelect:
                -0: ERROR
                -1:
                    ~ StoryName = "The Hunt"
                    ~ StoryBranch = ->TheHunt
                    ~ StoryXpText = "(+1000 XP for Scouting, {RangedWeaponText}, and Tactics)"
                -2:
                    ~ StoryName = "The Ambush"
                    ~ StoryBranch = ->TheAmbush
                    ~ StoryXpText = "(+1000 XP for Leadership, Tactics, and Roguery)"
                -3:
                    ~ StoryName = "The Charge"
                    ~ StoryBranch = ->TheCharge
                    ~ StoryXpText = "(+1000 XP for Riding, Polearm, and Leadership)"
                -4:
                    ~ StoryName = "Holding the Line"
                    ~ StoryBranch = ->HoldingTheLine
                    ~ StoryXpText = "(+1000 XP for {MeleeWeaponText}, Leadership, and Tactics)"
                -5:
                    ~ StoryName = "The Brawl"
                    ~ StoryBranch = ->TheBrawl
                    ~ StoryXpText = "(+1000 XP for all melee weapon skills)"
                -6:
                    ~ StoryName = "The Shootout"
                    ~ StoryBranch = ->TheShootout
                    ~ StoryXpText = "(+750 XP for all ranged weapon skills)"
            }
    
    //Group 2
        VAR DiscussionName = ""
        VAR DiscussionBranch = ""
        VAR DiscussionXpText = ""
        
        VAR DiscussionSelect = 0
            ~ DiscussionSelect = RANDOM(1,5)
            
            {DiscussionSelect:
                -0: ERROR
                -1:
                    ~ DiscussionName = "Great Rulers"
                    ~ DiscussionBranch = ->GreatRulers
                    ~ DiscussionXpText = "(+1000 XP for Steward, Leadership, and Charm)"
                -2:
                    ~ DiscussionName = "Craftsman"
                    ~ DiscussionBranch = ->Craftsman
                    ~ DiscussionXpText = "(+1500 XP for Smithing and Engineering)"
                -3:
                    ~ DiscussionName = "Negotiation"
                    ~ DiscussionBranch = ->Negotiation
                    ~ DiscussionXpText = "(+1000 XP for Charm, Trade, and Roguery)"
                -4:
                    ~ DiscussionName = "Traveling"
                    ~ DiscussionBranch = ->Traveling
                    ~ DiscussionXpText = "(+1500 XP for Riding and Athletics)"
                -5:
                    ~ DiscussionName = "Survival"
                    ~ DiscussionBranch = ->Survival
                    ~ DiscussionXpText = "(+1000 XP for Medicine, Scouting, and Athletics)"
            }
            
            
            
        
//Variable Check (Use for sanity check. Uncomment variables to see what they are)
//{GiveSkillExperience("Throwing", 1000)}

-> Start

===Start===

As it gets dark you and your men setup camp. As the night goes on you can see that your men have broken off into two groups. One seems to be telling war stories, while the other is just talking. #STR_Start1
-> choice1

    =choice1
        What will you do? //{MeleeWeaponRandom} {RangedWeaponRandom} //Uncomment for bug testing
            *[Listen in on the story of {StoryName} {StoryXpText}]
                ->StoryBranch
            *[Join in the discussion of {DiscussionName} {DiscussionXpText}]
                ->DiscussionBranch
            *[Tell your men to get some rest (All companions healed and all wounded troops restored)]
                You tell your men to head to bed early and get all the rest they can.
                ~ HealPartyToFull()
                ->END

===TheHunt===
    Amidst the crackling of the campfire, a grizzled soldier's voice carried a tale of stealth and pursuit. The flickering flames seemed to mirror the anticipation in the eyes of his companions as they leaned in to listen. #STR_TheHunt1

"Listen up, lads and lasses," the soldier began, "let me regale you with the tale of our last hunt. It was a moonless night, our steps guided by shadows and the rustling leaves. Our scouts moved through the underbrush, eyes sharp and senses alert as we sensed a minotaur..." #STR_TheHunt2

As the story unfolded, the soldiers felt themselves drawn into the narrative, experiencing the thrill of the chase and the tension that hung in the air. The storyteller's words painted a vivid picture of cunning and strategy, and by the time the tale concluded, the soldiers had a newfound appreciation for scouting and the art of the hunt. #STR_TheHunt3
    
    //Give Xp
        ~ GiveSkillExperience("Scouting",1000)
        ~ GiveSkillExperience("Tactics",1000)
        
        {RangedWeaponRandom:
                    -1:
                        ~ GiveSkillExperience("Bow" ,1000)
                    -2:
                        ~ GiveSkillExperience("Crossbow" ,1000)
                    -3:
                        ~ GiveSkillExperience("Throwing" ,1000)
                    -4:
                        ~ GiveSkillExperience("Gunpowder" ,1000)
                }
    -> END

===TheAmbush===
    Amid the crackling embers, a soldier's voice rose with a mischievous glint in his eye. The campfire's warm glow illuminated the eager faces of his comrades as they settled in for the story. #STR_TheAmbush1

"Gather 'round, men," the soldier said with a grin, "and let me tell you about the ambush we survived. Picture this—a Mannslieblit night, the enemy beastmen advancing unaware. Our plan was cunning, our movements swift. We struck with surprise and ferocity, turning the tide in our favor..." #STR_TheAmbush2

The soldiers were transported to a scene of calculated cunning and swift execution. Laughter and nods of approval followed the tale's conclusion, leaving the soldiers with a deeper understanding of tactics and the power of a well-executed ambush. #STR_TheAmbush3
    
        //Give Xp
            ~ GiveSkillExperience("Leadership",1000)
            ~ GiveSkillExperience("Tactics",1000)
            ~ GiveSkillExperience("Roguery",1000)
    -> END

===TheCharge===
    The fire's warm embrace cast dancing shadows upon the faces of the soldiers gathered around. Their attention was rapt as a battle-hardened warrior's voice filled the air. #STR_Charge1

"Listen well, my friends," the soldier began, "to the tale of our last battle. It was a day bathed in the glow of a setting sun. Our horses were eager, their hooves pawing at the earth. With a thunderous cry, we charged..." #STR_Charge2

The soldiers could almost feel the rush of wind against their faces and the pounding of hooves beneath them. The story painted a vivid picture of unity and bravery, leaving the soldiers with a deeper understanding of riding, weapon usage, and the power of a well-coordinated charge. #STR_Charge3
    
    //Give Xp
        ~ GiveSkillExperience("Riding",1000)
        ~ GiveSkillExperience("Polearm",1000)
        ~ GiveSkillExperience("Leadership",1000)
    -> END 

===HoldingTheLine===
    Amidst the campfire's gentle crackle, a soldier's voice carried the weight of determination. The glow of the flames seemed to mirror the resolve in the eyes of his companions. #STR_HoldingTheLine1

"Listen closely, my friends," the soldier spoke with unwavering conviction, "to the tale of our last battle. It was a moment of unbreakable unity as we positioned ourselves to hold strong, shields locked in steadfast defense. As the undead army advanced, we stood resolute..." #STR_HoldingTheLine2

The soldiers felt a sense of solidarity wash over them, as if they were standing side by side with the warriors of the story. The storyteller's words emphasized the importance of leadership and tactics, leaving the soldiers with a deeper appreciation for the art of defense. #STR_HoldingTheLine3
    
    //Give Xp
            ~ GiveSkillExperience("Scouting",1000)
            
            ~ GiveSkillExperience("Tactics",1000)
            
            {MeleeWeaponRandom:
                    -1:
                        ~ GiveSkillExperience("OneHanded",1000)
                    -2:
                        ~ GiveSkillExperience("TwoHanded",1000)
                    -3:
                        ~ GiveSkillExperience("Polearm",1000)
                }
    -> END

===TheBrawl===
    Around the campfire's flickering light, a soldier's voice carried a tale of camaraderie and friendly competition. Laughter mingled with the crackling of flames as his companions leaned in, eager to hear the story. #STR_TheBrawl1

"Ah, my comrades," the soldier chuckled, "let me tell you of what occured the last training session! It was a night of merriment turned into spirited contest. We playfully tested our mettle, each strike and parry a dance of skill..." #STR_TheBrawl2

The soldiers shared knowing glances, their own memories of friendly contests coming to mind. The storyteller's words emphasized the bonds of camaraderie and the lessons of melee combat techniques, leaving the soldiers with a sense of shared experience. #STR_TheBrawl3
    
    //Give Xp
            ~ GiveSkillExperience("OneHanded",1000)
            ~ GiveSkillExperience("TwoHanded",1000)
            ~ GiveSkillExperience("Polearm",1000)
    -> END

===TheShootout===
    Amidst the warm embrace of the campfire, a soldier's voice rose with a sense of anticipation. The flames danced in the eyes of his companions as they settled in to hear the tale. #STR_TheShootout1

"Listen closely, my comrades," the soldier began, "to the tale of our last battle. Imagine a sky heavy with clouds, setting the stage for a display of ranged prowess as my regiment aimed for the approaching beastmen. Bows, crossbows, throwing knives, and gunpowder weapons took center stage..." #STR_TheShootout2

The soldiers exchanged nods, their minds vividly painting scenes of arrows and projectiles soaring through the air. The storyteller's words underscored the intricacies of ranged combat, leaving the soldiers with a deeper understanding of various ranged weapon skills. #STR_TheShootout3
    
    //Give Xp
            ~ GiveSkillExperience("Bow",750)
            ~ GiveSkillExperience("Crossbow",750)
            ~ GiveSkillExperience("Throwing",750)
            ~ GiveSkillExperience("Gunpowder",750)
    -> END

===GreatRulers===
    Amidst the warm glow of the campfire, a group of soldiers engaged in a spirited conversation about the great rulers of the Old World. Their voices carried admiration and respect, their tales interwoven with lessons of leadership and statecraft. #STR_GreatRulers1

One soldier began, his voice laden with reverence, "Let us speak of the legendary rulers who shaped our lands. Last I heard, the great Karl Franz seemed to uphold his reputation well, as it takes a lot to govern..." #STR_GreatRulers2

As the stories flowed, the soldiers contemplated the qualities that made these rulers exceptional—their mastery of stewardship, the art of leadership, and the charisma that united their subjects. In their minds, the lessons of stewardship, leadership, and charm took root, leaving them with a deeper understanding of the responsibilities that came with power. #STR_GreatRulers3
    
    //Give Xp
            ~ GiveSkillExperience("Steward",1000)
            ~ GiveSkillExperience("Leadership",1000)
            ~ GiveSkillExperience("Charm",1000)
    -> END

===Craftsman===
    Amidst the camaraderie of the campfire, a group of soldiers exchanged tales of craftsmanship and engineering marvels. Their voices held a sense of awe and admiration as they recounted the feats of master artisans and ingenious engineers.  #STR_Craftsman1

"Listen closely, comrades," one soldier urged, "Not many know this, but I have had the blessing to learn about engineering from a dwarf. The Dwarfen holds are a testament to the art of smithing..." #STR_Craftsman2

As the stories unfolded, the soldiers marveled at the intricate designs and the sheer ingenuity that drove these feats. Their discussions delved into the realms of smithing, engineering, and the marvels born from the minds of skilled craftsmen and craftsdwarfs, leaving them with a newfound appreciation for these vital trades. #STR_Craftsman3
    
    //Give Xp
            ~ GiveSkillExperience("Smithing",1500)
            ~ GiveSkillExperience("Engineering",1500)
    -> END

===Negotiation===
    Amidst the flickering firelight, a group of soldiers regaled one another with stories of haggling and trade. Laughter mingled with their voices as they shared both successful negotiations and amusing tales of when things had gone awry. #STR_Negotiation1

"Ah, my comrades," one soldier chuckled, "let me share the art of haggling and the dance of trade. From bartering with the wily halflings to facing the shrewd merchants of Marienburg, the path to a fair deal is lined with wit and cunning..." #STR_Negotiation2

The soldiers leaned in, captivated by the stories of wit and banter that had unfolded in the bustling markets and bazaars of the Old World. They contemplated the delicate balance of charm, trade acumen, and the occasional misstep that came with the territory, leaving them with a deeper understanding of the art of negotiation. #STR_Negotiation3
    
    //Give Xp
            ~ GiveSkillExperience("Charm",1000)
            ~ GiveSkillExperience("Trade",1000)
            ~ GiveSkillExperience("Roguery",1000)
    -> END

===Traveling===
   Around the crackling fire, a group of soldiers shared tales of their travels and experiences on horseback. Their voices held a sense of adventure and camaraderie as they recounted journeys across treacherous landscapes and encounters with the denizens of the Old World. #STR_Traveling1

"Keeping a battlehorse healthy," one soldier declared, "that is its own challenge. The bond between rider and steed is a connection unlike any other..." #STR_Traveling2

As the stories wove their tapestry of adventure, the soldiers found themselves transported to distant lands and untamed wilderness. They contemplated the skills of riding and the unbreakable bond between a rider and their mount, leaving them with a deeper appreciation for the art of traveling on horseback. ##STR_Traveling3
    
    //Give Xp
            ~ GiveSkillExperience("Riding",1500)
            ~ GiveSkillExperience("Athletics",1500)
    -> END
    
===Survival===
    Amidst the gentle crackling of the fire, a group of soldiers shared their insights on surviving in the wilderness. Their voices carried the weight of experience as they recounted tales of resourcefulness and endurance in the face of nature's challenges. #STR_Survival1

"Listen well," one soldier began, his voice steady and assured, "for I shall impart the wisdom of how to properly survival in the thickest wild woods. From foraging for sustenance to navigating the dense forests and treacherous swamps, the key lies in understanding the land's rhythms..." #STR_Survival2

As the tales unfolded, the soldiers found themselves immersed in the art of survival, learning to read the signs of nature and adapt to its demands. The stories emphasized the skills of medicine, scouting, and athleticism, leaving the soldiers with a newfound respect for the unforgiving yet awe-inspiring world beyond the safety of civilization. #STR_Survival3
    
    //Give Xp
            ~ GiveSkillExperience("Medicine",1000)
            ~ GiveSkillExperience("Scouting",1000)
            ~ GiveSkillExperience("Athletics",1000)
    -> END


-> END


























