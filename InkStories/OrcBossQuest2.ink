//Global story tags
# title: Da Big Boss Rises
# frequency: Special
# development: false
# illustration: orc_boss_career_2

INCLUDE include.ink

//Variables setup
VAR QuestToStart = ""

->START

===START===
Under the watchful gaze of Gork and Mork you have proven yourself time and time again. You are a Boss, a warrior, you are death upon the battlefield and countless corpses lie in your wake.

Your mob of boys follows you, fears you, respects you. They know you will lead them to the biggest fights and the greatest loot.

You have become a boss among bosses, but the gods are not satisfied, never are, never will. There is more to be done.

+ [I'Z GUNNA BE DA BIGGEST DERE EVER WAS!!]
    -> confirm_big_boss

===confirm_big_boss===
You look to your boys, they are restless, they are ready. You look to the horizon, the road has been long, it's littered with loot, shinies and the corpses of all the runts that oppose you.

No enemy dares to face you in open battle, so you must take the fight to them, tear them from their walls and drag them screaming from their homes.

The time is now, the green tide under your command will drown the world.

+ [CRUSH 'EM ALL!! WAAAAAAAAAAAAAAAAGH!!!]
    ~ StartQuest("Quests.Careers.OrcBossQuest2")
    ~ CloseStory()
    -> END