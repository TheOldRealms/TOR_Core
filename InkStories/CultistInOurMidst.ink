//Global story tags
# title: Cultist in our midst
# frequency: Special
# development: false
# illustration: village

INCLUDE include.ink
        
    VAR DealtWithCultists = false
    VAR CultName = "Cult of Khorne"
    VAR HardSkillCheckValue = 250
    VAR NormalSkillCheckValue = 150
    VAR EasySkillCheckValue = 80
    VAR ElderState = 0
        ~ ElderState = RANDOM(1,3) // 1 - normal, 2 - guilty, 3 - grumpy
    VAR SymbolLeftBehind = 1
        //~ SymbolLeftBehind = RANDOM(0,1)
    VAR CultIsKnownToPlayer = false
    VAR MassacreHappened = false
    VAR StruggleHappened = false

-> Start

===Start===
    The journey to the village has been treacherous, winding through dense forests and foggy valleys. As you approach, you notice the hustle and bustle of villagers going about their daily lives. However, there's an undercurrent of unease in the air, hidden behind forced smiles and hushed whispers.
    You leave your party camped outside the village and decide to begin your investigation discreetly on your own. Observing from the shadows, you notice a small group congregating near the village square, their demeanor suspiciously secretive. They exchange coded glances and speak in hushed tones.
    ->choices

    =choices
    *[Approach the group and listen in on their conversation. {print_player_skill_chance("Roguery", NormalSkillCheckValue)}]
        {perform_player_skill_check("Roguery", NormalSkillCheckValue): ->ListenToGroup.succeed | -> ListenToGroup.fail}
    *[Gather information from the villagers without raising suspicion.] ->InvestigateVillagers


=== ListenToGroup ===

    =succeed
    (SUCCESS)
    You stealthily approach the group, careful not to draw attention to yourself. Standing at a distance, you strain your ears to catch snippets of their conversation.
    
    "...the summoning ritual must be performed soon," whispers one figure anxiously. "Our power grows stronger every day."
    
    Another voice responds, "We must keep our true identities hidden. The Templar Order might be onto us. We don't want witch hunters all over the village, then all will be lost."
    
    "Let's meet at the Cradle tonight..."
    
    The group disperses, each member disappearing into the crowd. The villagers continue their daily routines, seemingly oblivious to the hidden darkness lurking within their midst. 
    ->Start.choices
    
    =fail
    (FAIL)
    You try to stealthily approach the group, careful not to draw attention to yourself, however stealth is not your strong suit and as you inch closer, a sudden creaking noise alerts them to your presence. 
    They glance in your direction, their eyes narrowing with suspicion. They exchange a few quick words before disappearing into the crowd.
    Your attempt to eavesdrop has failed, and you can't help but wonder if your element of suprise has just been compromised. 
    ->Start.choices


=== InvestigateVillagers ===
    You realize that the cultists are adept at hiding their true identities. Finding out who they are is going to be no easy feat. You decide to interact with the villagers and gather more information.
    Speaking to various individuals, you subtly inquire about recent strange occurrences, missing persons, or rumors of dark practices. Some villagers express unease, speaking of mysterious symbols etched in hidden corners, unexplained disappearances and strange lights appearing in the surrounding forest during the night.
    ->choices

    =choices
    *[Seek out the village elder for questioning.] ->InterviewElder
    *[Venture into the woods during the night to uncover the source of the strange lights.] ->Woods
    *[Look into the disappearances by talking to the relatives of the disappeared.] ->InvestigateDisappearances
    * -> OutOfOptions


=== InterviewElder ===
{ElderState == 3: ->grumpy | ->normal}

    =normal
    The elder, a man of some means compared to the modest state of the village, resides in a comfortable cottage near the center of the village.
    Knocking on the wooden door, the elder welcomes you inside with a warm smile. The cottage exudes a sense of coziness, with a crackling fireplace casting a comforting glow across the room. You take a seat by a small wooden table, ready to discuss your concerns about the strange occurrences in the village.
    {ElderState == 1:As you confront the elder about the potential presence of a cult in the village, he listens attentively but with a skeptical expression on his face. He dismisses the notion of a cult, finding it absurd and far-fetched. He believes that the recent troubles can be attributed to mere coincidences or isolated incidents.}
    {ElderState == 1:"I understand your concerns," he says, his voice tinged with a touch of condescension. "But I assure you, there is no cult in our village. These strange occurrences can be explained by natural causes or the overactive imagination of some villagers."}
    {ElderState == 1:Frustrated by the elder's denial, you realize that convincing him to take action against the cult will be an uphill battle. It's clear that alternative approaches need to be explored to address the growing threat.}
    {ElderState == 2: During the conversation, you observe the surroundings, paying attention to the subtle indications of the elder's relatively elevated wealth. The silverware glimmers in the soft candlelight, the paintings on the walls reveal scenes of serene landscapes and the elder's clothing exhibits a higher level of craftsmanship compared to the average villager.}
    {ElderState == 2:As the discussion progresses, the elder admits to the village's troubles, but finds the idea of a cult operating within the village simply absurd. However, you sense a flicker of unease in his eyes, a hint of guilt that betrays more than his words convey.}
    ->choices
    
    =grumpy
    The elder, a man known for his lackluster performance in maintaining order and resolving village issues, resides in a modest cottage at the heart of the village. As you approach, you notice signs of neglect in the surroundings—overgrown garden, peeling paint on the front door, and an overall air of disarray.
    You knock on the wooden door, and the elder opens it with a slightly exasperated expression. "What do you want?" he grumbles, his tone reflecting a touch of annoyance. You explain the reason for your visit, expressing concerns about the strange occurrences in the village and the possible presence of a cult.
    "You think there's a cult in our village?" he scoffs, his voice tinged with disbelief. "That's preposterous! We have enough problems with everyday life without such wild tales. Troubles? Yes, we have plenty. But a cult? No way."
    As you press further, attempting to convince the elder of the seriousness of the situation, his temper flares up. "I have more pressing matters to attend to than listening to such nonsense!" he snaps, his frustration palpable. "If you want to investigate, go ahead. But don't come bothering me with your imaginary cults!"
    With that, he slams the door in your face, the sound echoing through the quiet village streets.
    Left with no choice, you must find alternative means to investigate without the elder's cooperation.
    ->InvestigateVillagers.choices
    
    =choices
    *{ElderState == 2}[Confront the elder about his apparent wealth accusing him of illicit activities. {print_player_skill_chance("Charm", HardSkillCheckValue)}]
        {perform_player_skill_check("Charm", HardSkillCheckValue): -> InterviewElder.succeed | -> InterviewElder.fail}
    * -> InvestigateVillagers.choices

    =succeed
    (SUCCESS)
    The elder's face twitches, caught off guard by the bluntness of your allegations.
    In a moment of vulnerability, the elder confesses that he has been receiving small sums of money left anonymously at his doorstep. The source of the money remains a mystery to him, but he admits that he has turned a blind eye to the strange events in exchange for these bribes. Shame fills his voice as he explains that his financial struggles and the allure of a better life for his family had clouded his judgment.
    He seems sincere. You are convinced he truly doesn't know more about the origin of the bribe money.
    Despite his lack of knowledge, you implore the elder to take responsibility for his actions and sever ties with the anonymous benefactor. You emphasize the importance of restoring the village's safety and well-being, urging him to become an ally in the fight against the cult.
    *[Lie in wait for the next drop of bribe money in order to follow the person who delivers it.] -> wait
    *[Find other ways to continue your investigation.] -> InvestigateVillagers.choices
    
    =fail
    (FAIL)
     The elder vehemently denies any such accusations. With an air of indignation, he defends himself, claiming that his relatively improved circumstances are a result of shrewd financial management and investments made outside the village. 
     The elder skillfully deflects your allegations, attributing them to rumors and jealousy among the villagers who are envious of his modest success. 
     Despite your suspicions, he manages to maintain an outward appearance of innocence, leaving you with lingering doubts about his true intentions. 
     ->InvestigateVillagers.choices
    
    =wait
    Determined to uncover the mystery behind the bribe money, you devise a plan to stake out the elder's home and wait for the next drop. Days turn into nights as you patiently remain hidden, keeping a vigilant watch for any signs of the mysterious deliverer. But as time goes by, no one arrives, and the nights remain undisturbed.
    Growing frustrated and exhausted, you start to doubt the effectiveness of this approach. Perhaps the briber has become aware of your presence or has changed their method of delivery. The lack of any significant leads or developments weighs heavily on your determination.
    You decide to abandon the stakeout, acknowledging that this particular lead has reached a dead end.
    -> InvestigateVillagers.choices

===Woods===
    Intrigued by the mention of strange lights in the woods, you decide to delve into the depths of the forest during the cloak of night. With your senses sharpened and your weapon at the ready, you navigate through the dense foliage.
    
    As you make your way deeper into the woods, the glow of the lights becomes more intense and magical. It dances and flickers in patterns that seem orchestrated, almost intentional.
    
    To your surprise, you stumble upon several unusually large swarms of fireflies, their luminescent bodies creating a breathtaking spectacle. They flutter and twirl in mesmerizing unison, illuminating the surrounding trees with their enchanting glow.
    
    Realizing that these fireflies are the source of the mysterious lights, you watch in awe as they continue their nocturnal display. Though not the cultists you were seeking, their presence reminds you of the beauty and wonder that exists in the world.
    
    Feeling a sense of peace and tranquility, you take a moment to appreciate the natural marvel before continuing your investigation.
    ->InvestigateVillagers.choices

===InvestigateDisappearances===
    Your first course of action is to approach the relatives of the disappeared individuals. You lend a sympathetic ear, offering comfort and support while discreetly gathering information. Each tale is filled with anguish and confusion, with common threads of unexplained circumstances. Dark rumors circulate, whispering of an unseen force lurking within the shadows of the village.
    Driven by a sense of urgency, you delve deeper into the matter, searching for clues and connections. You map out the locations where the disappearances occurred, marking them on a makeshift investigation board. Patterns emerge, indicating a concentration of incidents near the outskirts of the village and the surrounding woods.
    ->choices
    
    =search
    With a determined focus on finding answers, you set out to investigate the homes of the disappeared individuals, hoping to uncover any clues that might shed light on their unsettling vanishing. As you enter each home, a sense of sadness and unease fills the air, reminding you of the lives that were abruptly interrupted.
    Inside one of the homes, you come across signs of struggle—a knocked-over chair, a shattered vase, and belongings strewn about haphazardly. It's evident that something untoward occurred here, suggesting a forced departure rather than a voluntary one.
    In another home, you discover personal belongings left behind — a cherished trinket, a half-finished letter, and a favorite book. These remnants of their lives hint at the suddenness and unexpected nature of their departure.
    It becomes clear that the vanished individuals were victims, taken against their will.
    {SymbolLeftBehind == 1: As you meticulously investigate the home with the signs of struggle, your sharp eye catches something amidst the chaos — an item left behind by the perpetrators. Carefully hidden beneath a toppled table, you discover a broken amulet with a torn chain, unmistakably belonging to the cult you have been seeking. -> identify_option}
    {SymbolLeftBehind == 0: Despite your thorough investigation of the disappeared victims' homes, you find no further significant leads or breakthroughs. The signs of struggle and abandoned belongings only deepen the mystery, leaving you with more questions than answers. Frustration and a sense of helplessness start to settle in as you realize that the trail has gone cold. ->InvestigateVillagers.choices}
    
    =identify_option
    *[Identify the symbol. {print_player_attribute_chance("Intelligence",5)}] -> identify_check
    
    =identify_check
    {perform_player_attribute_check("Intelligence", 5): -> succeed | -> fail}
    
    =succeed
    (SUCCESS)
    ~ CultIsKnownToPlayer = true
    You instantly recognize the distinct symbol of the {CultName}.
    A chill runs down your spine as you recognize the significance of the item. It's a distinct piece of paraphernalia associated with the cult, confirming their direct involvement in the disappearances. 
    ->InvestigateVillagers.choices
    
    =fail
    (FAIL)
    Despite your thorough examination of the symbol, you are unable to identify its meaning or significance. 
    You find no further significant leads or breakthroughs. The enigmatic symbol, signs of struggle and abandoned belongings only deepen the mystery, leaving you with more questions than answers. 
    ->InvestigateVillagers.choices
    
    =choices
    *[Carefully search the homes of the disappeared.] -> search


===OutOfOptions===

After tirelessly pursuing various leads and options, your efforts have yielded little progress in solving the intricate web of mysteries that shroud the village. Frustration and weariness start to take their toll, leaving you at a crossroads, unsure of the best path forward.

With your mind weighed down by the weight of the unresolved enigmas, you find yourself standing before the village tavern. The warm glow of its windows and the inviting aroma of food beckon you inside. Perhaps a moment of respite, a chance to gather your thoughts and reassess your strategies, is what you need.

As you sit in the tavern, contemplating your next move, a snippet of conversation from a nearby group catches your attention. The villagers are abuzz with talk of a gathering planned for the next day, a rare occasion when the entire village will come together to discuss recent events and concerns. 

Listening closely, you learn that the gathering will take place at the village square. The news piques your interest, as it presents a chance to observe the villagers' reactions, gauge their suspicions, and perhaps catch a glimpse of any cultists who might try to blend in with the crowd.

You can't help but wonder about the possibilities that such an event presents. The thought of the entire village congregating in one place, including potential cultists, triggers a series of calculations in your mind.

Could the cultists be planning to make a move during this gathering? Would they take advantage of the crowd to advance their agenda, or would they simply observe from the shadows, disguising their true intentions?

*[Attend the gathering.] -> AttendGathering
*[Give up the search. This investigation has already taken too much of your time.] -> VoluntaryEnd

=== AttendGathering ===
As the day of the village gathering arrives, a mix of anticipation and caution fills the air. You make your way to the village square, your senses sharpened and your awareness heightened. The bustling crowd, a sea of familiar faces, conceals the unknown. As you navigate through the villagers, you remain vigilant, your gaze scanning for any signs of the cult's presence.

Suddenly, a subtle shift in the atmosphere catches your attention. An undercurrent of tension ripples through the crowd, and you notice several individuals who seem out of place—furtive glances, concealed expressions. Instinctively, your eyes narrow on their movements. Your suspicions are confirmed when you glimpse the glint of daggers, hidden within their clothing.

Your heart quickens as the gravity of the situation becomes clear. The cultists are among the villagers, concealed and armed. {CultIsKnownToPlayer: Knowing what you know about the {CultName} it becomes evident that their | Their} intent is not merely to observe; they plan to strike, unleashing chaos and violence.

How do you proceed?

*[Careful not to cause chaos, try to disarm the cultists one by one. {print_player_skill_chance("Roguery", HardSkillCheckValue)}] -> DisarmCultists
*[Expose their true nature to the villagers, turning them against the cultists. {print_player_skill_chance("Charm", HardSkillCheckValue)}] -> ExposeCultists

=== ExposeCultists ===
{perform_player_skill_check("Charm", HardSkillCheckValue): -> succeed | -> fail}

    =succeed
    ~ StruggleHappened = true
    (SUCCESS)
    Summoning every ounce of determination, you raise your voice above the chaos, your words cutting through the fear and confusion with authority. Urgency infuses your voice as you expose the hidden threat of the cultists, their daggers concealed and their intent to unleash violence upon the unsuspecting villagers. 
    
    Shock and disbelief ripple through the crowd as they gaze upon their friends and neighbors, unmasked as agents of chaos.
    
    The villagers, enraged and betrayed, turn against the cultists, their trust shattered. The ensuing struggle is brief but fierce, the villagers driven by a shared goal — to protect their community and rid it of this malevolent presence.
    
    Among the chaos, a handful of cultists manage to break free from the villagers' grasp, disappearing into the winding streets that surround the square.
    
    The rest are swiftly outnumbered and subdued. As the dust settles, a sense of triumph and relief washes over the square. 
    ->choices

    =fail
    (FAIL)
    Amidst the village gathering, you step forward, heart pounding with the urgency of your message. Your voice carries above the chatter, capturing the attention of those around you. You unveil the hidden threat — the presence of cultists among them, armed and intent on a violent act to appease their dark deity. 
    
    But as your words hang in the air, disbelief and skepticism ripple through the crowd like a stone cast into a calm pond. Eyes narrow and brows furrow as the villagers exchange glances, some even chuckling at what they perceive as an outlandish tale. 
    ->CultistsAct
    
    =choices
    *[Chase after the nearest escaping cultist.] ->ChaseCultist

===DisarmCultists===
Moving with a deliberate caution, you weave through the villagers, your movements calculated to avoid drawing attention. The cultists are strategically positioned near the edge of the crowd, and as you approach the first one, your heart pounds with a mix of fear and purpose.
{perform_player_skill_check("Roguery", HardSkillCheckValue): -> succeed | -> fail}

    =succeed
    (SUCCESS)
    Your fingers deftly work as you reach the cultist's side, your hand moving to disarm the hidden dagger. The blade is cool against your touch as you extract it from its concealed sheath, the cultist remaining blissfully unaware of your actions. 
    Your movements are calculated, your senses attuned to every nuance. The weight of each blade taken away is both a victory and a somber reminder of the violence that could have been. 
    Eventually some of your earlier victims realize that something is amiss. Their gaze narrows, a glint of suspicion sparking within their eyes. Panic flares within you as you realize that your actions have not gone entirely unnoticed.
    The cultists exchange alarmed glances, their unspoken communication reveals a decision — they recognize the element of surprise they had hoped to wield has slipped away. They begin to disengage from their positions within the crowd and blend into the labyrinthine streets that surround the square.
    ->choices
    
    =fail
    (FAIL)
    In an instant, your heart skips a beat as the cultist's gaze locks onto your actions. Panic flares in his eyes, followed by a swift reaction. With a sharp intake of breath, they attempt to wrench the dagger from your grasp. The element of surprise is lost, replaced by a struggle that draws the attention of nearby cultists.
    ->CultistsAct
    
    =choices
    *[Chase after the nearest escaping cultist.] ->ChaseCultist

===CultistsAct===
~ MassacreHappened = true
The hidden cultists seize this moment to enact their plan. Strategically positioned near the edge of the gathering, they draw concealed daggers and converge with a sinister purpose. 
Before anyone can react, the cultists spring into action, their blades gleaming in the daylight. Chaos ensues as they mercilessly cut down anyone in their path. Chaos reigns as horror-stricken cries fill the air as the village square transforms into a scene of nightmarish violence. The cultists' chilling efficiency and the villagers' shock paralyze any chance of immediate escape.
Amidst the chaos and horror that engulfs the village square, your eyes catch something strange — patterns emerging within the flow of spilled blood on the cobblestones. The cultists' daggers wielded with a calculated brutality create rivulets of crimson that seem to converge in deliberate paths.
A cold shiver courses down your spine as you recognize the significance of these patterns — the cultists' intent is far more insidious than a mere massacre. The blood they spill is not wasted; it's directed towards a purpose. Their dark ritual aims to channel the spilled blood into the hidden depths beneath the village square, a macabre ceremony to appease their bloodthirsty deity.

*[Rally some of the villagers to mount a defense with your leadership.] -> RallyVillagers
*[Fearing for your own life, flee the scene of horror and abandon this futile quest.]
    As the horrifying chaos of the massacre unfurls before you, your instincts take over, propelling you into action.
    Adrenaline courses through your veins as you turn away from the scene of violence. The screams of the villagers echo in your ears, spurring you to move swiftly, desperately seeking an escape.
    Leaving the village and its enigmas behind, you turn away from the chaos, the violence, and the darkness that have consumed your days.
    ->END

===RallyVillagers===
Amidst the chaos of the village square, your determination ignites a spark of action within you. With a voice raised above the cacophony, you call out to those within earshot, your words carrying a sense of urgency and authority.

"Villagers, stand together" - with a firm voice, you command the villagers to build barricades using nearby stands and tables and mount a defense against the cultists' onslaught.

The horrific scene at the village square transforms into one of organization and defiance as the villagers rally to your command. Their makeshift weapons, combined with the barriers they've created, form a defensive line that stands as a formidable challenge to the cultists' daggers. 

As the cultists' advance is met with this unexpected resistance, their determination begins to falter. They pause, held at bay by the villagers' united front and the strategic advantage they've taken. The realization dawns upon them that the element of surprise has been thwarted, replaced by a defiant strength that they hadn't anticipated.

In a swift decision, the cultists begin to withdraw, their footsteps retreating as they fade into the background.
->choices

    =choices
    *[Chase after the nearest escaping cultist.] ->ChaseCultist
    
===ChaseCultist===
Driven by a relentless determination, you choose to give chase as the cultists retreat from the village square. The winding streets and narrow alleys become a blur as you navigate the labyrinthine paths, driven by a thirst for answers and justice.
Your pursuit eventually leads you to a building that stands apart from the others — an abandoned and partly ruined structure marred by time and neglect. The entrance, concealed by a tattered curtain of vines, hints at the darkness that lies within. The cultists' footsteps fade as they disappear through this ominous entrance.
Sword in hand, you decide to enter after them.
->EnterHideout

===VoluntaryEnd===
    With a heavy sigh, you acknowledge that this puzzle has proven too much for you, robbing you of time and peace. You decide to leave the village to its fate. With a final glance you turn away, the weight of unanswered questions and unfulfilled justice a burden you reluctantly leave behind.
->END

===EnterHideout===
~ OpenCultistLairMission("TOR_cultist_lair_001")
...
{DealtWithCultists: As the last cultist falls beneath the weight of your blade, a deafening silence descends upon the chamber. The air is thick with the scent of victory and the echoes of battle. You stand amidst the fallen cultists, the sword in your hand a testament to your unwavering determination and skill.}
{DealtWithCultists && MassacreHappened: As you step out of the underground chamber, your heart sinks at the sight that awaits you in the village square. The once vibrant heart of the village now lies transformed into a scene of unspeakable horror. Bodies of villagers and cultists alike litter the ground, their lives snuffed out in the violent clash that has taken place.}
{DealtWithCultists && MassacreHappened: Blood stains the cobblestones, turning the ground into a macabre canvas of tragedy. The air is thick with the scent of iron and the aftermath of battle, a stark contrast to the festivities that had filled the square only moments before. The debris of the confrontation, overturned stands and shattered tables, bears witness to the chaos that has unfolded.}
{DealtWithCultists && MassacreHappened: The once-lively atmosphere is replaced by an eerie stillness, broken only by the distant sounds of sobbing and the soft cries of those who have survived. The villagers, who had rallied to your side in the face of the cultists' threat, now grapple with the brutal reality that their efforts have come at a heavy cost.}
{DealtWithCultists && not MassacreHappened && not StruggleHappened: You emerge from the hideout and step into the square, your heart is still racing from the confrontation with the cultists. }
{DealtWithCultists && not MassacreHappened && not StruggleHappened: As you move through the crowd, you realize that your actions have gone unnoticed. The villagers laugh and chat, engrossed in their festivities, unaware of the sinister plot that had loomed over them.}
{DealtWithCultists && not MassacreHappened && not StruggleHappened:You take a moment to appreciate the warmth and joy that fill the air. Children play, adults converse, and the camaraderie of the villagers is a testament to their shared bonds and resilience. The darkness that had sought to infiltrate their lives has been kept at bay, and your actions have played a crucial role in preserving their way of life.}
 {DealtWithCultists && not MassacreHappened: The cult has been eradicated, and the hidden threat has been extinguished, leaving behind a village that can continue to thrive in the light of a new day.}
 {not DealtWithCultists: As you lie defeated within the hidden chamber, your breath ragged and your body battered, the weight of your failure presses heavily upon you. The cold stone walls, which had borne witness to the battle's violence, now seem to close in around you, a grim reminder of the darkness that has prevailed.}
 {not DealtWithCultists: Amidst the silence of your defeat, a distant sound reaches your ears — a chorus of hurried footsteps and raised voices. The troops of your party, who had been camped outside the village are now rushing to your aid.}
 {not DealtWithCultists: The cultists who had managed to overwhelm you, having exposed themselves, are no longer within your reach. They have slipped away, disappearing like smoke carried by the wind, leaving the village in their wake.}
 {not DealtWithCultists: The knowledge that the cultists will surely continue their reign of darkness in another unsuspecting village is a painful reality to accept.}
->END