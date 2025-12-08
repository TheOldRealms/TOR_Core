//Global story tags
# title: Da Gods Demand More!
# frequency: Special
# development: false
# illustration: campfirenight

INCLUDE include.ink

->START

===START===
You've proven yourself. Da boyz respect you now—some even fear your weird magic. You've learned da ways of da Waaagh, gathered teef, and shown you ain't just some weak goblin playing with sparkly lights.

But da gods ain't satisfied yet.

You return to da shrine, and before you even start da ritual, da visions come flooding back. Gork and Mork loom over you, bigger and angrier than before.

"NOT BAD, GIT," grunts Gork. "BUT YOU AIN'T DONE YET!"

"YEAH! WE NEED A GREAT SHAMAN LORD!" cackles Mork. "NOT JUST SOME APPRENTICE WHAT KNOWS A FEW TRICKS!"

"DA BIGGEST WAAAGH IS COMIN'! AN' YOU'Z GONNA LEAD DA BOYZ! BUT FIRST—"

"MORE MAGIC! MORE TEEF! MORE CITIES KRUMPED!" they roar together.

"SHOW US YOU'Z DA GREATEST SHAMAN DA GREENSKINS EVER 'AD!"

"AN' MAYBE," Gork adds with a wicked grin, "IF YOU'Z GOOD ENOUGH, WE'LL LET YA CALL DOWN DA FOOT UV GORK ON YOUR ENEMIES!"

"OR DA FIST UV MORK!" Mork adds, not wanting to be left out.

Da vision ends, but da weight of their expectations crushes down on you. Time to get back to work.

+ [I'll show ya! I'll be da greatest!]
    ~ StartQuest("Quests.Careers.OrcShamanQuest2")
    ~ CloseStory()
    -> END