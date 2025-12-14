//Global story tags
# title: Da Calling of Da Gods
# frequency: Special
# development: false
# illustration: campfirenight

INCLUDE include.ink

//Variables setup
VAR QuestToStart = ""

->START

===START===
You wake with a pounding headache, but dis ain't from too much fungus beer. Your skull feels like it's gonna split open, and strange visions flash through your mind—green lightning, roaring gods, and da endless WAAAGH!

Somefing's different about you. Da other boyz look at you funny. Some are scared. Others whisper words you've heard before: "Shaman." "Weird boy." "Touched by da gods."

You don't understand it all yet, but you know one fing for certain—Gork and Mork are calling to you. Da twin gods of da Greenskins want somefing from you, and you ain't gonna ignore 'em unless you fancy getting your head stomped flat.

You need to find a shrine. A proper one, where da Greenskin gods can speak to you without all dis noise in your head.

Time to answer da call.

+ [Alright, alright! I'll find a shrine!]
    ~ StartQuest("Quests.Careers.OrcShamanQuest1")
    ~ CloseStory()
    -> END
