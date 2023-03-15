-> START

EXTERNAL GiveWinds(number)

===function GiveWinds(number)===
    ~ return number

EXTERNAL GiveGold(number)

===function GiveGold(number)===
    ~ return number


VAR spellcraft = 100.0


===START===
The strange menhir # title # illustration: menhir
Your scouts report finding a most curious object just a little distance off the road ahead. It is a strange menhir, a towering stone monolith shimmering with a magical aura.
As your party approaches, it becomes visible that the menhir is surrounded by a circle of runic stones etched deep into the ground.
The runes glow with a faint energy and your men feel uneasy as their hair stand on end while gazing upon this strange phenomenon. -> choices

=choices
* [Ignore the menhir and continue your journey.] -> LEAVE
* [Order your men to try to dig it out of the ground.] -> UPROOT
* [Inspect the magical aura. (Spellcraft skillcheck ({(spellcraft / 250) * 100}%))] -> DECIPHER

===LEAVE===
It is better not to become involved in supernatural events. Your party hastily departs not spending a moment longer in the vicinity of the stone monolith. You hear some of your men letting out sighs of relief and yet some others murmuring prayers to Sigmar as you leave. -> END

===UPROOT===
Thinking that an artifact collector must pay good money for something like this, you order your men to load the hunk of stone onto a cart. 
Your men bring out the shovels and ropes but any attempt to cross the runic stone circle is futile. The closer anyone gets, the feeling of uneasiness increases to such a point that it becomes unbearable. Your men refuse to try further.
-> START.choices

===DECIPHER===
{spellcraft >= RANDOM(1,250): -> succeed | -> fail}
=succeed
Success! On closer inspection you find that the menhir is no more than a magical scarecrow. Its entire function seems to be to scare people away from the surrounding area by making them feel uneasy and afraid. -> choices

=choices
* [Leave as you laugh about your foolishness.]
    You order you party to continue your journey still occasionally chuckling about how you got scared for a moment. -> END
* [Inspect the area to find out what exactly the menhir was supposed to scare people away from.] -> LOOK
* [Drain the magical charge from the menhir to increase your winds of magic.]
    You drain the menhir of its magic increasing your own winds of magic by {GiveWinds(20)}. -> choices

=fail
Failure! No matter how hard you try, you can't make heads or tails of the menhir. Its purpose and the nature of the magical aura remain a secret for you.
 -> START.choices
 
 ===LOOK===
Not far from the menhir you find a small camp in a cave. The campfire is at least several days cold, but there are belongings littered all around. Looks like the owner is out. # illustration: cave
    -> choices
=choices
* [Look around for anything valuable.]
    The camp is a mess. Several half-eaten bones are lying around with empty sacks and broken tools. Not very well hidden, there is a small coin purse under a rock.
    ** [Take the gold.]
        You gain {GiveGold(13)} gold. -> LOOK.choices
    ** [Leave it.] -> LOOK.choices
* [Leave the camp alone.]
    You decide to leave the camp alone and continue on your journey.
 -> END
 