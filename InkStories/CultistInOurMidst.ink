//Scenarios notes
    //Rarity: COMMON
    //Repeatable: YES
    
    //Restrictions
        //Character Level:
        //Race:
        //Faction:
        //
    
    //Triggers:
        // Entering target settlement while having the corresponding quest
    
    //Scenario Explanation (explain the main scenario and any major variations that you are planning to build in. If a variation is different enough consider making it its own file.)
    
        //Main:
        //The player arrives at village to investigate reports of cultists hiding there.
        
//Data Import/Export Section
    //Make sure you include this in all ink files to get access to integration functions
        INCLUDE include.ink
        
    //List of Data Being Imported (use this to help keep track of what data you are importing; will help with troubleshooting and testing.)
    
        //
        
    //Data Exported (use this to help keep track of what data you are exporting; will help with troubleshooting and testing.)
        //
        
//Variables setup
    //IMPORTANT! Initial values are mandatory, but they can only be primitives (number, string, boolean). If we want to assign the return value of a function to the variable, we must do it on a separate line, see one line below

    //Seed
        //~ SEED_RANDOM(100) //Uncomment to lock an RNG testing seed for the randomness. Change number inside () for different seed
    VAR DealtWithCultists = false
    VAR CultName = "Cult of Tzeench"
    VAR HardSkillCheckValue = 250
    VAR NormalSkillCheckValue = 150
    VAR EasySkillCheckValue = 80
    VAR ElderState = 0
        ~ ElderState = RANDOM(1,3) // 1 - normal, 2 - guilty, 3 - grumpy
    VAR SymbolLeftBehind = 0
        ~ SymbolLeftBehind = RANDOM(0,1)

-> Start

===Start===
Cultist in our midst #title #illustration: village

    The journey to the village has been treacherous, winding through dense forests and foggy valleys. As you approach, you notice the hustle and bustle of villagers going about their daily lives. However, there's an undercurrent of unease in the air, hidden behind forced smiles and hushed whispers.
    You leave your party camped outside the village and decide to begin your investigation discreetly on your own. Observing from the shadows, you notice a small group congregating near the village square, their demeanor suspiciously secretive. They exchange coded glances and speak in hushed tones. -> choices

    =choices
    *[Approach the group and listen in on their conversation. {print_player_skill_chance("Roguery", NormalSkillCheckValue)}]
        {perform_player_skill_check("Roguery", NormalSkillCheckValue): -> ListenToGroup.succeed | -> ListenToGroup.fail}
    *[Gather information from the villagers without raising suspicion.] -> InvestigateVillagers



=== ListenToGroup ===

    =succeed
    (SUCCESS)
    You stealthily approach the group, careful not to draw attention to yourself. Standing at a distance, you strain your ears to catch snippets of their conversation.
    
    "...the summoning ritual must be performed soon," whispers one figure anxiously. "Our power grows stronger every day."
    
    Another voice responds, "We must keep our true identities hidden. The Templar Order might be onto us. We don't want witch hunters all over the village, then all will be lost."
    
    The group disperses, each member disappearing into the crowd. The villagers continue their daily routines, seemingly oblivious to the hidden darkness lurking within their midst. -> Start.choices
    
    =fail
    (FAIL)
    You try to stealthily approach the group, careful not to draw attention to yourself, however stealth is not your strong suit and as you inch closer, a sudden creaking noise alerts them to your presence. 
    They glance in your direction, their eyes narrowing with suspicion. They exchange a few quick words before disappearing into the crowd.
    Your attempt to eavesdrop has failed, and you can't help but wonder if your element of suprise has just been compromised. -> Start.choices






=== InvestigateVillagers ===
    You realize that the cultists are adept at hiding their true identities. Finding out who they are is going to be no easy feat. You decide to interact with the villagers and gather more information.
    Speaking to various individuals, you subtly inquire about recent strange occurrences, missing persons, or rumors of dark practices. Some villagers express unease, speaking of mysterious symbols etched in hidden corners, unexplained disappearances, missing livestock and strange lights appearing in the surrounding forest during the night.
    ->choices

    =choices
    *[Seek out the village elder for questioning.] -> InterviewElder
    *[Venture into the woods during the night to uncover the source of the strange lights.] -> Woods
    *[Investigate the missing livestock.] -> Symbols
    *[Look into the disappearances by talking to the relatives of the disappeared.] -> InvestigateDisappearances
    *[Embark on a meticulous search for discreet symbols or markings throughout the village.] -> Symbols
    * -> OutOfOptions
    //*[Attend a village gathering and observe the behavior of the suspected cultists.] -> AttendGathering








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
     Despite your suspicions, he manages to maintain an outward appearance of innocence, leaving you with lingering doubts about his true intentions. -> InvestigateVillagers.choices
    
    =wait
    Determined to uncover the mystery behind the bribe money, you devise a plan to stake out the elder's home and wait for the next drop. Days turn into nights as you patiently remain hidden, keeping a vigilant watch for any signs of the mysterious deliverer. But as time goes by, no one arrives, and the nights remain undisturbed.
    Growing frustrated and exhausted, you start to doubt the effectiveness of this approach. Perhaps the briber has become aware of your presence or has changed their method of delivery. The lack of any significant leads or developments weighs heavily on your determination.
    You decide to abandon the stakeout, acknowledging that this particular lead has reached a dead end.
    -> InvestigateVillagers.choices







=== Symbols ===
    Under the cover of darkness, you discreetly search the villagers' homes for hidden symbols and artifacts. It's a risky endeavor, but you're determined to find any evidence of the cult's presence.
    
    In one house, you discover a hidden compartment containing a weathered tome, filled with incantations and sinister rituals. The cultists have indeed infiltrated the village, using innocent homes as their hiding places for their dark artifacts.
    
    Armed with this knowledge, you continue to search for the true identities of the cultists with renewed determination.
    ->InvestigateVillagers.choices







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
    *[Identify the symbol. {print_player_attribute_chance("Intelligence",7)}] -> identify_check
    
    =identify_check
    {perform_player_attribute_check("Intelligence", 7): -> succeed | -> fail}
    
    =succeed
    (SUCCESS)
    You instantly recognize the distinct symbol of the {CultName}.
    A chill runs down your spine as you recognize the significance of the item. It's a distinct piece of paraphernalia associated with the cult, confirming their direct involvement in the disappearances. Emboldened by this newfound evidence, you resolve to redouble your efforts in exposing the cult. ->InvestigateVillagers.choices
    
    =fail
    (FAIL)
    Despite your thorough examination of the symbol, you are unable to identify its meaning or significance. 
    You find no further significant leads or breakthroughs. The enigmatic symbol, signs of struggle and abandoned belongings only deepen the mystery, leaving you with more questions than answers. Frustration and a sense of helplessness start to settle in as you realize that the trail has gone cold. ->InvestigateVillagers.choices
    
    =choices
    *[Carefully search the homes of the disappeared.] -> search






=== EavesdropTavern ===
You blend into the lively atmosphere of the local tavern, strategically positioning yourself to overhear conversations. The air is filled with laughter and merriment, providing the perfect cover for your investigation.

As you listen, fragments of conversations reach your ears. Whispers of strange occurrences, odd rituals performed under the cover of night, and the involvement of certain villagers.

Piecing together the information, you realize that the cultists are using the tavern as a meeting place, hiding in plain sight.

Deciding to follow one of the suspected cultists, you discreetly leave the tavern, ready to unveil the truth.
->TrackCultist

=== ExploreRuins ===
You venture into the forgotten ruins on the outskirts of the village, the air thick with an eerie stillness. Within the crumbling walls, you discover hidden chambers adorned with forbidden symbols and traces of dark magic.

Carefully examining the ruins, you unearth a concealed passage leading to an underground chamber. Inside, you find an altar surrounded by the paraphernalia of the {CultName}. This must be the heart of their operations.

Knowing you've uncovered their secret lair, you prepare yourself for a confrontation. With your sword in hand, you descend further into the depths, ready to face the cultists head-on.

-> ConfrontCultists

=== AttendGathering ===
The village gathering provides an opportunity to observe the suspected cultists without raising suspicion. As you mingle with the crowd, you keep a keen eye on individuals displaying suspicious behavior.

You notice a group of villagers forming a tight circle, exchanging meaningful glances. They break away intermittently, disappearing into secluded corners, seemingly engrossed in whispered conversations.

Determined to expose their true intentions, you discreetly follow one of the suspected cultists, careful not to arouse their suspicion.

-> TrackCultist

=== TrackCultist ===
Silently trailing the suspected cultist, you navigate through the village's winding paths. They lead you to a hidden grove on the outskirts, shielded by ancient trees. There, you witness a chilling scene.

The cultists stand in a circle, their hoods lowered, revealing familiar faces of trusted villagers. The once-bustling village square has become the backdrop for a dark ritual. Their voices rise in unison, a twisted chant that sends shivers down your spine.

You realize that the entire village is unknowingly under the cultists' sway, manipulated by their subtle influence. To break their grip, you must confront them head-on.

-> ConfrontCultists

=== ConfrontCultists ===
You step forward, revealing yourself to the cultists. Their eyes widen in surprise and fear, caught off guard by your presence.

"You've deceived the entire village!" you declare, your voice carrying the weight of authority. "Your twisted rituals end now."

A tense silence ensues, the cultists weighing their options. One among them steps forward, their voice laced with arrogance. "You cannot stop us, witch hunter. Our power is greater than anything you've ever faced."

Then, after a flickering moment of hesitation, he continues. "You could join us instead. These powers could be of benefit to you as well."

*[Engage in a direct battle with the cultists.] -> BattleCultists
*[Expose their true nature to the villagers, turning them against the cultists.] -> ExposeCultists
*[Join the cultists and embrace the power they offer.] -> JoinCultists

=== BattleCultists ===
Unleashing your training and wielding your weapons with precision, you engage in a fierce battle against the cultists. Spells clash, dark energy and steel meeting in a deadly dance.

Though outnumbered, your determination and experience give you an edge. With each strike, you weaken their defenses, disrupting their unholy rituals. The battle rages on, but you never waver.

As the last cultist falls, defeated and disarmed, the village awakens from its oblivious slumber. They witness the aftermath of the cult's dark influence and extend their gratitude to you, the hero who freed them from the grasp of deception.
~ DealtWithCultists = true
-> END

=== ExposeCultists ===
With the knowledge you've gathered, you step forward and reveal the cultists' true nature to the unsuspecting villagers. Shock and disbelief ripple through the crowd as they gaze upon their friends and neighbors, unmasked as agents of chaos.

The villagers, enraged and betrayed, turn against the cultists, their trust shattered. The cultists are overwhelmed, unable to withstand the collective fury of those they sought to control.

Witnessing the unity and strength of the villagers, you know that the darkness has been exposed and defeated. The village can now rebuild, stronger and more resilient than ever before.
~ DealtWithCultists = true
-> END

=== JoinCultists ===
A twisted thought takes hold of your mind, tempting you with the promise of unimaginable power. The cultists' words resonate within you, and you find yourself seduced by their dark influence.

Turning your back on your previous life as a witch hunter, you step forward, revealing your allegiance to the cult. The other cultists welcome you with open arms, celebrating your decision.

Together, you join their circle, continuing the ritual that will plunge the village and the world into eternal darkness. Your transformation is complete, and the path you once walked as a force of good is forever lost.

-> BadEnding

=== BadEnding ===
In the days that follow, the village becomes a place of despair and suffering. The cult's influence spreads like a festering disease, corrupting the innocent and extinguishing all hope.

As you leave the village, you gaze upon the devastation you helped unleash and a hollow emptiness fills your heart. Was the power you were seeking truly worth all this? The world falls deeper into chaos and the light of justice dims.

-> END 

===OutOfOptions===
the end?
-> END
