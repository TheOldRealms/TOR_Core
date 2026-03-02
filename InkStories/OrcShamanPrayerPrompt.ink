//Global story tags
# title: Da Calling of Da Gods
# frequency: Special
# development: false
# illustration: orc_shaman_career_2

INCLUDE include.ink

//Variables setup
VAR QuestToStart = ""

->START

===START===
After a night of prolonged dancing, you wake up with a horrible headache. You haved dealt with headaches before but this is different.

It’s a throbbing pain, as if the gods themselves are shaking you around.

Some boys gather around you, watching in awe and fear. You fall to your knees and vomit green bile onto the earth.

In the foul-smelling mush you see meat chunks and bone splinters arranged in an image of a shrine, idols to Gork and Mork.

The gods are calling, it is time to answer.

+ [Where’s dis bloody place!?]
    ~ StartQuest("Quests.Careers.OrcShamanQuest1")
    ~ CloseStory()
    -> END
