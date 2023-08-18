INCLUDE include.ink

VAR EXTREMESKILLCHECK = 1500
VAR NORMALSKILLCHECK = 150
VAR EASYSKILLCHECK = 50
VAR found_vial = false
VAR suspicious = false
VAR found_passage = false
VAR met_healer = false
VAR found_age = false
VAR playerVictory = false

->START


===START===
The plague ridden village #title #illustration: village
You leave your party camped at the edge of the village and decide to enter alone, wearing simple commoner's clothes to avoid attention. As you step into the village, a foul stench assaults your senses. The air hangs heavy with the scent of decay, mingled with the lingering odor of despair. Narrow, deserted streets wind through dilapidated buildings, their facades worn and battered by time and neglect. The villagers, once vibrant and lively, now shuffle along like haunted specters, their vitality snuffed out by the merciless grip of an unkown plague.

A sickly haze blankets the village, casting a pallor upon everything it touches. Doors creak on rusty hinges, their once vibrant colors faded and peeling. Shuttered windows betray the fear that resides within, as if the villagers have locked themselves away. Here and there, flickering lanterns cast feeble light upon desperate symbols of protection scrawled on walls - crude sigils etched in blood and ash, futile attempts to ward off the contagion.

Amidst the desolation, a few figures defy the relentless onslaught of the disease. They move with purpose, their eyes harboring a flicker of resilience. These survivors cast wary glances, their bodies untouched by the affliction that has ravaged their neighbors. Their existence, like fragile embers amidst the encroaching darkness, hints at a lingering hope for the village. -> choices

    =choices
    *[Seek information from the villagers.] -> QuestionVillagers
    *[Walk around the village carefully observing the sights.] -> ObserveVillage
    *{suspicious}{not found_age} [Ask around about the healer.] -> AskAboutHealer
    *{found_vial || (found_age && met_healer)}[Take a moment to gather your thoughts and go over your findings.] -> GatherThoughts


===QuestionVillagers===
As you approach a lone figure among the weary villagers, their eyes glimmer with a spark of resilience. Intrigued, you pose the question that weighs heavily on your mind, asking about the plague. 
With a wearied expression, the villager responds in a hushed voice, "The plague... it has been haunting us for months now. So many lives lost, so much suffering endured." Their voice carries the weight of their personal experience, reflecting the collective anguish that permeates the air. 
They gesture towards the dwelling of the village healer, acknowledging their unwavering dedication in the face of despair, and say, "Seek the healer. They have become our beacon of hope, tirelessly fighting against this unyielding affliction." ->choices 

    =choices
    *{not met_healer}[Heed the advice and find the village healer.] -> AtTheHealers
    *{met_healer}[Go back to the healer's dwelling hoping you will find him there this time.] -> AtTheHealers

===AskAboutHealer===
You decide to discreetly inquire about the healer among the villagers. As you strike up conversations, you notice a common thread in their responses—the healer has been a pillar of the village for as long as anyone can remember. Generations have relied on his expertise, his knowledge passed down from one era to the next.
"Ah, the healer? He's been with us for as long as I can recall," one elderly villager says, a sense of reverence in his voice. "His wisdom and remedies have saved countless lives, even my grandfather sought his aid."
Another villager adds, "Yes, I was just a child when I first saw him, and that was many decades ago. He has remained a steady presence, offering comfort to the sick and hope to the desperate."
The villagers' words strike you as odd, for if their accounts are true, the healer would be over ninety years old, yet the last time you saw him, he appeared no older than fifty. Doubt gnaws at the corners of your mind, as if reality itself has twisted within this forsaken village.
~found_age = true
->START.choices

===AtTheHealers===
~met_healer=true
As you step into the healer's dwelling, a scene unfolds before your eyes that both captivates and unsettles. The room is filled with patients, their pallid faces marked by the ravages of the plague. Some lay on cots, writhing in pain, while others sit in chairs, waiting anxiously for their turn to receive treatment. 

The healer moves through the room, their movements graceful yet purposeful. They offer soothing words of comfort, their touch gentle and assured. Shelves lining the walls bear the weight of countless vials, jars, and medical instruments. The flickering candlelight casts eerie shadows, revealing glimpses of the healer's dedication and the tools of their trade.

Amidst the array of medicinal herbs and potions, your gaze lands on something seemingly out of place — a curious vial of dark, viscous liquid that stands apart from the rest. -> choices 

    =choices
    *[Examine the curious vial. {print_player_skill_chance("Medicine", EASYSKILLCHECK)}]
        {perform_player_skill_check("Medicine", EASYSKILLCHECK): -> succeed | -> fail}
    *[Ask the healer about the vial.] -> inquire
    *{found_vial}[Confront the healer.] -> ConfrontHealer
    *{found_vial}[Decide to keep the discovery for yourself and continue your investigation.] -> START.choices
    *[Leave.] -> START.choices

    =succeed
    As you examine the vial with a growing sense of alarm, your trained eye recognizes the contents for what they truly are — an insidious agent of the contagion, a potent and vile substance that fuels the very plague consuming the village.
    ~found_vial = true
    ->choices
    
    =fail
    As you examine the vial with a perplexed expression, you can't quite place its purpose or contents, lacking the necessary medical knowledge to discern its true nature. -> choices

    =inquire
    You direct your gaze towards the healer and ask directly about the peculiar vial. 
    "What is the purpose of this vial? Its contents appear unlike any remedy I have encountered," you inquire, your tone laced with a mix of curiosity and caution. 
    The healer meets your gaze, their eyes briefly flickering with unease before they respond in a reassuring voice, "Ah, that vial contains a potent extract of a rare herb—a key ingredient in a powerful disinfectant. It aids in curbing the spread of the disease, ensuring the safety of both the afflicted and the healthy." Their words are accompanied by a calm smile, masking any underlying apprehension. 
    ~suspicious = true
    -> choices

===ObserveVillage===

You walk the desolate streets, your eyes keenly scanning the surroundings for any signs that might reveal the truth behind the plague and the rumors of cultist activity. Among the dilapidated buildings and the suffering inhabitants, you notice subtle details that pique your interest.

A faded sigil etched on a crumbling wall catches your eye. It bears a resemblance to symbols associated with the chaos god Nurgle, hinting at a possible connection to the cultist rumors. Seeing the plague, you already suspected as much, but its presence alone is not enough to confirm the truth.

Amidst the desolation, you notice a peculiar pattern. Your eyes are drawn to certain individuals who defy the affliction that plagues the rest. Mid-aged males between the ages of thirty and forty, they exude an extraordinary level of health and vitality. Their robust, muscular builds stand as a stark contrast to the frail, emaciated figures that surround them. Their cheeks bear a healthy flush, glowing with vitality, while their eyes sparkle with resilience and strength. Their very presence seems to radiate life amidst the gloom.

You cannot help but be awestruck by their sheer well-being. Their exceptional health raises questions within you — what grants them this extraordinary resilience? Is there a natural explanation, or could there be more to their apparent invulnerability? -> choices

    =choices
    *[Observe the daily routine of the healthy villagers.] -> ObserveRoutine

===ObserveRoutine===
Intrigued by the extraordinary health of the mid-aged males in the village, you decide to observe their daily routine over the course of a few days. Your investigation leads you to a fascinating discovery - each morning, without fail, the healthy villagers gather at the humble dwelling of the village healer.
It piques your curiosity as they don't require any treatment, yet they spend a considerable amount of time inside. -> choices
    
    =choices
    *[Decide to enter the healer's dwelling while most of the healthy villagers are inside.] -> AtTheHealersAgain

===AtTheHealersAgain===
With curiosity getting the better of you, you decide to seize the opportunity and enter the healer's dwelling {met_healer: again} when the healthy villagers are gathered inside. The moment you step through the door, an eerie sight greets you. The healer's abode is dimly lit, filled with the scent of herbs and incense. On one side of the room, a few villagers lie in bad condition, moaning in pain, their bodies ravaged by the plague. 
But there's no sign of the healer or the healthy villagers who you just saw enter moments ago.
~suspicious = true
->choices

    =choices
    *[Look around for a clue as to where they might have disappeared.{print_player_skill_chance("Scouting", EASYSKILLCHECK)}]
        {perform_player_skill_check("Scouting", EASYSKILLCHECK): -> succeed | -> fail}
    *{found_passage}[Without hesitation, you descend underground.] -> Descend
    *{not found_passage}[Give up the search and leave.] -> START.choices

    =succeed
    Your eyes sweep across the room, searching for any clue that might explain their sudden disappearance. Shelves line the walls, adorned with vials, potion bottles, and ancient tomes on various medicinal practices. The room feels strangely tense, as if it holds a secret waiting to be unraveled.
    As you cautiously explore further, a hidden passageway catches your attention, concealed behind a heavy tapestry. Instinctively, you move closer, your heart pounding in anticipation. The passageway seems to lead underground, into an unknown darkness that beckons you to uncover its secrets.
    ~found_passage=true
    ->choices
    
    =fail
    Your eyes sweep across the room, searching for any clue that might explain their sudden disappearance. You meticulously inspect every nook and cranny, running your hands over the shelves and walls, trying to find a hidden passageway. However, no matter how hard you look, there seems to be no obvious way to uncover the mystery.
    As frustration and bewilderment start to take hold, you can't help but feel dumbfounded by the situation. The healer and the healthy villagers seem to have vanished without a trace, leaving you with more questions than answers. Could they have sensed your presence and slipped away unnoticed?->choices

===GatherThoughts===
You find a moment of respite to gather your thoughts. You retreat to a quiet corner of the village, away from prying eyes, and meticulously review the evidence you have amassed so far.
The healthy villagers, seemingly untouched by the plague, continue to stand out as an enigma. Their robust health, vibrant energy, and inexplicable immunity confound reason. They are the very embodiment of life amidst the despair that consumes the village.
Yet, their association with the healer raises more questions than answers. If the healer has indeed been tending to the village for generations, he should be a frail, elderly man, but that is far from the truth. The last time you encountered him, he appeared to be a man in his prime, defying the passage of time.
The discovery of the symbol of Nurgle etched on a crumbling wall hints at a possible connection to the cultist rumors. The presence of such a symbol in the village casts a sinister shadow over its already grim atmosphere. 
Additionally, the suspicious vial you found in the healer's dwelling lingers in your thoughts. {not found_vial: Its contents remain unidentified, and the healer's explanation raises doubts about its true purpose. Could it be an agent of the contagion, or is it genuinely an innocent remedy as claimed?}{found_vial: You are absolutely certain that the contents of the vial are used to fuel the epidemic. As for what purpose? That remains to be seen.}
A chilling suspicion takes root in your mind. Could it be that the healer himself is somehow linked to the plague and the cultist rumors? Is there something clandestine hidden beneath the surface of his benevolent facade? With renewed determination you decide to confront finally confront the healer.
->ConfrontHealer

===ConfrontHealer===
{came_from(-> GatherThoughts): Fueled by determination, you enter the healer's dwelling once more, your eyes sharp with newfound knowledge.} As you approach, the healer's eyes meet yours. There is a moment of silent acknowledgment — a realization that the truth has been uncovered.
Sensing the weight of your suspicions, the healer's face contorts with a mix of fear and desperation. Without a word, he turns and dashes towards the far end of the room, making a desperate attempt to escape your scrutiny.
"Wait!" you call out, giving chase as he reaches the back of the dwelling. In a swift motion, he reveals a hidden trap door, flinging it open and disappearing into the depths below. You don't hesitate, following closely behind, descending the narrow steps into the unknown darkness. -> choices
    
    =choices
    *[Without hesitation, you follow him, descending the narrow steps into the unknown darkness.] -> Descend
    

===Descend===
As you venture deeper, the eerie ambiance intensifies, guiding you towards the heart of the cult's malevolence. The passage leads you into a massive chamber, adorned with grotesque carvings venerating the loathsome glory of Nurgle. An unsettling greenish glow emanates from the eerie torches, casting ghastly shadows on the walls.
To your astonishment, you discover not only the healer but a congregation of cult members, donned in tattered robes. Their eyes gleam with an unholy fervor, reflecting their fanatical devotion to the chaos god.
At the center of the chamber rests a large cauldron, its contents seething with a putrid liquid akin to the vial you found earlier. The noxious stench fills the air, betraying the malevolence of the concoction. It becomes evident that this cauldron holds the malefic source of the plaguing torment that has befallen the village.
As the cult leader, once masquerading as the healer, steps forward, his true intentions unveil before you. He proclaims his allegiance to Nurgle, promising the congregation an eldritch boon — the gift of eternal youth and resolute health in exchange for disseminating the accursed plague. 
The cult members join in an unholy chant, the malefic melody pulsating with ominous power. They believe in the perverse grandeur of Nurgle's supposed benevolence, as though they have willingly surrendered their souls to the deity of rot and decay. -> choices

    =choices
    *[Engage in conversation with the cultists.] -> END
    *[Engage the cultists in battle determined to rid the village of the chaos corruption.] -> END

->END