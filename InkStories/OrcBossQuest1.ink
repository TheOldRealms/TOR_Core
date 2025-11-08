//Global story tags
# title: Da Boss Awakens
# frequency: Special
# development: false
# illustration: campfirenight

INCLUDE include.ink

//Variables setup
VAR QuestToStart = ""

->START

===START===
You wake from your mushroom slumber, da spores still clinging to your thick green skin. Your head pounds like a Troll's footsteps, but it ain't pain—it's somefing else. Somefing primal.

WAAAGH!

Da voice in your head screams it, over and over. Your muscles twitch, your fists clench. You need to SMASH. You need to CRUMP. You need to FIGHT!

Every fiber of your being demands violence. Not da sneaky kind—no, you ain't no goblin. You need proper scrapping: breaking bones, crushing skulls, showing everyone who's DA BIGGEST AND DA STRONGEST!

Your vision goes red at da edges. If you don't find somefing to hit soon, you might just explode from all dis pent-up WAAAGH energy!

Time to get to work. Time to prove you're a proper Boss.

+ [WAAAGH! Let's get krumpin'!]
    ~ StartQuest("Quests.Careers.OrcBossQuest1")
    ~ CloseStory()
    -> END

    
    