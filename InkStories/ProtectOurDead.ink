//Global story tags
# title: Protect Our Dead
# frequency: Uncommon
# development: true
# illustration: campfirenight

INCLUDE include.ink

VAR PlayerWin = false
VAR PartyCanRaiseDead = false
            ~ PartyCanRaiseDead = PartyHasNecromancer(false)
VAR RaiseDeadSkillCheckTest = false
            ~ RaiseDeadSkillCheckTest = perform_party_skill_check("Spellcraft", 25)
//Scenarios notes
    //Rarity: COMMON
    //Repeatable: YES
    
    //Restrictions
        //Terrain: Empire, Bretonnia, Telia, Estalia, or Border Princes culture
    
    //Triggers:
        //While travelling on campaign map
    
    //Scenario Explanation
    
        //Main: You are traveling and a peasant asks you to rid the local graveyard of a necromancer.
		// Rewards: faith exp + small amount of gold or skeleton troops + staff.

->START

===START===
At the end of the days march, your men are setting up camp. You know sunset will come soon, and these lands are dangerous, especially at night. #STR_Start1
Suddenly, one of your men shouts a warning. Glancing up, you see a local villager approaching. He appears to be unarmed. #illustration: stranger #STR_Start2
The man explains that a recently arrived necromancer has started raising the dead from the village cemetery. Although the village is quite poor, he says they will pay a modest reward to anyone who slays the necromancer. #STR_Start3 
-> choices

    =choices
    *[We will kill this necromancer for you.] ->accept
    *[This is an outrage, those skeletons should belong to me!] ->accept
    *[Perhaps another time. We have more urgent matters to attend to.] -> deny
    
    =accept
    The village explains that the necromancer comes every night with a few skeletons. With this knowledge, you make a plan to ambush him in the graveyard.  #STR_Accept1
    
    ->enterArena
    
    =deny
    ->END

    =enterArena
    //~ OpenGraveyardMission()
    ...
    {PlayerWin: As the necromancer falls, you give thanks to insert_deity_name. #STR_PlayerWin1}

    ->BattleResult
    
===BattleResult===
        *[Return to the village and claim the reward {GiveGold(500)}{GiveSkillExperience("Faith",1000)}]
		-> END
		
        //Necromancer option
        *{PartyCanRaiseDead}[Attempt to bind the defeated skeletons to your will, {print_party_skill_chance("Spellcraft", 25)}]
                {RaiseDeadSkillCheckTest: -> raiseSucceed | -> raiseFail}
    
        =raiseSucceed
        Having successfully raised the dead, you search the necromancer for anything of value. {GiveItem("tor_vc_weapon_staff_nm_001", 1)} #STR_HelpNecromancerSuccess
            
            ~ChangePartyTroopCount("tor_vc_skeleton",8)
            -> END
        
        =raiseFail
        You may have failed to raise the dead, but at least the necromancer left a useful staff behind. {GiveItem("tor_vc_weapon_staff_nm_001", 1)} #STR_HelpNecromancerFail
            -> END