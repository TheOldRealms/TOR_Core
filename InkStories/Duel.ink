//Global story tags
# title: The Art of the blade
# frequency: Special
# development: false
# illustration: roadpoint2

INCLUDE include.ink

VAR PlayerWin = false
VAR MetBefore = true
    ~ MetBefore = GetPlayerHasCustomTag("MetVittorio")
VAR DeniedBefore = true
    ~ DeniedBefore = GetPlayerHasCustomTag("DeniedVittorio")


->START

===START===
As you continue your travels along a meandering road, the soft whispers of the breeze accompany your every step.
Suddenly, the rhythmic beat of approaching footsteps captures your attention. Glancing up, {not MetBefore: you spot a mysterious figure approaching with confident strides. As they draw nearer, the glint of a finely crafted rapier catches your eye. The stranger stops before you, a warm smile on their face as they appraise you.}{MetBefore:  you spot the familiar figure of Vittorio de Luca, the renowned Tilean duelist, making his way towards your group with confident strides. {not DeniedBefore: Memories of your previous encounter flood back, the thrill of the first duel still lingering in your mind.}} #illustration: stranger
{not MetBefore: "Ah, what a stroke of luck to meet a band of worthy warriors on this lonely path," they say. "I am Vittorio de Luca, a master of the blade from the distant lands of Tilea. I have journeyed far and wide, seeking a worthy adversary who can match my skills in combat. And now, fate has led me to you. Care to prove your skills in a friendly duel, with a little wager to make it exciting?"}
{MetBefore: As Vittorio draws nearer, the glint of his finely crafted rapier catches your eye, and a warm smile spreads across his face as he appraises you. "Ah, what a stroke of luck to meet again on this lonely path," he says, his voice carrying a playful undertone. "I see the fire of a warrior still burns within you. {not DeniedBefore: Care to prove your skills once more in a rematch?"} {DeniedBefore: Care to prove your skills this time around?}}
~ SetPlayerCustomTag("MetVittorio")
-> choices

=choices
*[Accept the challenge.] ->accept
*[Perhaps another time. We have no time to waste.] -> deny

=accept
{not MetBefore: Intrigued by the proposition, you return their smile, curious about the stakes they propose.}
{not MetBefore: "A duel with a wager? I'm listening," you reply, open to the idea.}
{not MetBefore: The duelist's eyes sparkle with anticipation as they explain the terms. "If you win, I shall offer a sum of 5000 gold coins as a testament to your skill. Should I prove triumphant, I ask for nothing more than the honor of having tested my skills against yours."}
With a gleam of excitement in your eyes, you accept the duelist's challenge, and a determined smile crosses your face. "Very well," you say, "I accept your offer, Vittorio de Luca. Let us make this duel one to remember {MetBefore: once more}."
As your fellow warriors cheer in support, you order them to make camp by the roadside, turning the clearing into an impromptu arena. #illustration: meadow
With the arena ready, you step into the center, your heart pounding with anticipation. Your fellow warriors gather around, forming a circle to watch the contest, their expressions a mix of excitement and pride.
->enterArena

=deny
~ SetPlayerCustomTag("DeniedVittorio")
Vittorio's expression remains composed, but a subtle smirk plays at the corners of his lips. 
"A pity," he responds, his voice laced with a touch of condescension. "I had hoped to find someone worthy of my time, but it seems the rumours surrounding your prowess have been exaggerated." 
With an air of haughty elegance, Vittorio de Luca concludes the encounter by offering a disdainful bow, his movements exuding unquestionable superiority.
->END

=enterArena
~ OpenDuelMission()
...
{PlayerWin: As the clash of swords subsides, the cheering of your fellow warriors fills the air, echoing in the aftermath of your hard-fought victory. {SetPlayerCustomTag("DefeatedVittorio")}}
{PlayerWin: You stand at the center of the makeshift fighting pit, your chest heaving with exertion and triumph. Vittorio de Luca, the renowned Tilean duelist, extends a hand in a gesture of respect, a genuine smile lighting up his face. "Well fought," he says, his voice filled with admiration.}
{PlayerWin: The camaraderie between your party and Vittorio solidifies as he graciously presents you 5000 gold coins, honoring his wager and acknowledging your skill. {GiveGold(5000)}} 
{not PlayerWin: As the duel concludes, the air is thick with a mix of emotions. Your fellow warriors watch in silent respect as Vittorio de Luca, the renowned Tilean duelist, emerges victorious from the fierce contest.}
{not PlayerWin:You step back, acknowledging his skill with a nod of admiration. Vittorio stands at the center of the makeshift fighting pit, his rapier gleaming in the fading light, a victorious smile gracing his face. "A formidable opponent indeed," he says, his voice carrying a sense of pride in his achievement. "You fought valiantly, but this time, the victory is mine."}
Vittorio de Luca bids your band a respectful farewell. His graceful demeanor and the elegance of his words remain unchanged despite the outcome of the duel.
->END