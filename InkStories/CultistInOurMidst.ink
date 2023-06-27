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
    VAR ReligionName = "Cult of Tzeench"
    VAR DealtWithCultists = false
    //
        
//Variable Check (Use for sanity check. Uncomment variables to see what they are)


-> Start

===Start===
Cultist in our midst #title #illustration: village

The journey to the village has been treacherous, winding through dense forests and foggy valleys. As you approach, you notice the hustle and bustle of villagers going about their daily lives. However, there's an undercurrent of unease in the air, hidden behind forced smiles and hushed whispers.
You decide to begin your investigation discreetly. Observing from the shadows, you notice a small group congregating near the village square, their demeanor suspiciously secretive. They exchange coded glances and speak in hushed tones.

*[Approach the group and listen in on their conversation.] -> ListenToGroup
*[Gather information from the villagers without raising suspicion.] -> InvestigateVillagers

=== ListenToGroup ===
You stealthily approach the group, careful not to draw attention to yourself. Standing at a distance, you strain your ears to catch snippets of their conversation.

"...the summoning ritual must be performed soon," whispers one figure anxiously. "Our power grows stronger every day."

Another voice responds, "We must keep our true identities hidden. The Templar Order might be onto us. We don't want witch hunters all over the village, then all will be lost."

The group disperses, each member disappearing into the crowd. The villagers continue their daily routines, seemingly oblivious to the hidden darkness lurking within their midst.

-> InvestigateVillagers

=== InvestigateVillagers ===
You realize that the cultists are adept at hiding their true identities. To uncover their secrets, you decide to interact with the villagers and gather more information.

Speaking to various individuals, you subtly inquire about recent strange occurrences, missing persons, or rumors of dark practices. Some villagers express unease, speaking of mysterious symbols etched in hidden corners, unexplained disappearances, and strange chanting during the night.

Slowly, a picture emerges: the cultists have cleverly embedded themselves within the village, concealing their true nature behind ordinary facades. To expose them, you must delve deeper into their activities.

->choices

=choices
*[Interview the village elders for ancient legends and folklore.] -> InterviewElders
*[Search for hidden symbols and artifacts in the villagers' homes.] -> SearchHomes
*[Eavesdrop on conversations in the local tavern.] -> EavesdropTavern
*[Attend a village gathering and observe the behavior of the suspected cultists.] -> AttendGathering

=== InterviewElders ===
You seek out the village elders, respected individuals with a wealth of knowledge about the village's history. They share ancient legends and folklore, hinting at possible connections to the cult's activities.

Listening intently, you learn about ancient ruins in the nearby forest, rumored to hold great power. It becomes clear that the cultists are drawing upon the village's ancient secrets for their nefarious purposes.

Armed with this information, you decide to venture into the forest and investigate further.
-> ExploreRuins

=== SearchHomes ===
Under the cover of darkness, you discreetly search the villagers' homes for hidden symbols and artifacts. It's a risky endeavor, but you're determined to find any evidence of the cult's presence.

In one house, you discover a hidden compartment containing a weathered tome, filled with incantations and sinister rituals. The cultists have indeed infiltrated the village, using innocent homes as their hiding places for their dark artifacts.

Armed with this knowledge, you continue to search for the true identities of the cultists with renewed determination.
->InvestigateVillagers.choices

=== EavesdropTavern ===
You blend into the lively atmosphere of the local tavern, strategically positioning yourself to overhear conversations. The air is filled with laughter and merriment, providing the perfect cover for your investigation.

As you listen, fragments of conversations reach your ears. Whispers of strange occurrences, odd rituals performed under the cover of night, and the involvement of certain villagers.

Piecing together the information, you realize that the cultists are using the tavern as a meeting place, hiding in plain sight.

Deciding to follow one of the suspected cultists, you discreetly leave the tavern, ready to unveil the truth.
->TrackCultist

=== ExploreRuins ===
You venture into the forgotten ruins on the outskirts of the village, the air thick with an eerie stillness. Within the crumbling walls, you discover hidden chambers adorned with forbidden symbols and traces of dark magic.

Carefully examining the ruins, you unearth a concealed passage leading to an underground chamber. Inside, you find an altar surrounded by the paraphernalia of the {ReligionName}. This must be the heart of their operations.

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
