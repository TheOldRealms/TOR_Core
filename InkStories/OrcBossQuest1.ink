//Global story tags
# title: Da Boss Awakens
# frequency: Special
# development: false
# illustration: orc_boss_career_2

INCLUDE include.ink

//Variables setup
VAR QuestToStart = ""

->START

===START===
It comes suddenly, not quite a thought, not quite a feeling. Something closer to instinct. Your heart rate quickens. There is something stirring inside you: excitement. Urgency. You need to move. You need to KILL.

A voice whispers, then roars. It urges you on.

“Bigga… betta… stronga… kill… KILL… WAAAAAAAAAAAAAGH!”

A sign from the gods? The voice leads you, drives you. To ignore it would be to invite the wrath of Gork and Mork themselves.

You have been chosen. YOU. Given a chance to prove yourself before the gods.
You must rise up. Face challenges. Smash everything. But above all, you must KILL.

+ [LET’S DO DIS!]
    ~ StartQuest("Quests.Careers.OrcBossQuest1")
    ~ CloseStory()
    -> END

    
    