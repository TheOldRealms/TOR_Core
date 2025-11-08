//Global story tags
# title: Da Big Boss Rises
# frequency: Special
# development: false
# illustration: campfirenight

INCLUDE include.ink

//Variables setup
VAR QuestToStart = ""

->START

===START===
You've proven yourself a proper Boss. Da boyz respect ya, fear ya even. You've krumped enough gits to make a mountain of bodies, and your pile of teef is big enough to buy half da world.

But somefing still ain't right.

You sit on your pile of shinies, surrounded by your toughest boyz, and you feel it again—dat burning in your chest. Dat WAAAGH energy dat never quite goes away.

A nearby goblin squeaks somefing about "Da Big Boss," and it hits you like a choppa to da face.

You ain't just A Boss. You're gonna be DA BIGGEST BOSS! Da one all da other Bosses look up to (or get krumped by). Da one who makes even da Black Orcs think twice before getting lippy.

But being DA BIG BOSS ain't easy. You gotta be da strongest, da meanest, da most kunnin' (well, kunnin' enough), and have da biggest pile of teef anyone's ever seen!

Time to show da whole world who's really in charge.

+ [I'Z GONNA BE DA BIGGEST!]
    -> confirm_big_boss

===confirm_big_boss===
You stand up, your shadow falling over da gathered boyz. They look up at you with a mix of fear and excitement.

"Listen up, ya gits!" you bellow. "We ain't done yet! We're gonna krump more gits, take more cities, and get so much teef dat even da humies will be jealous!"

Da boyz roar their approval. WAAAGH!

Time to become DA BIG BOSS.

+ [WAAAGH! Let's get even MORE krumpin'!]
    ~ StartQuest("Quests.Careers.OrcBossQuest2")
    ~ CloseStory()
    -> END