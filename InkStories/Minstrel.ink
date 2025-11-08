//Global story tags
# title: Traveling Troubadours
# frequency: Special
# development: false
# illustration: minstrel


INCLUDE include.ink

->START

===START===
~ PlayMusic("west_bretonnia")
During your travels, your ears catch the lively chatter of a group of troubadours engaged in an animated conversation. Their vibrant attire and energetic gestures suggest a shared passion for their craft. Curiosity beckons you closer, and as you approach, their discussion shifts seamlessly into an improvised performance. #STR_Start1

Captivated by their harmonious voices, you decide to stay and listen. The troubadours' music weaves a narrative that transcends mere words. The rhythm of their song pulls you into a shared moment, where the world's worries and uncertainties seem to fade away. #STR_Start2

As their performance reaches its climax, the troubadours' gazes meet, their smiles reflecting the joy they find in their artistic exchange. And then, with a final, triumphant note, their song comes to an end. #STR_Start3

*[Clap and applaud] -> Applaud
*[Express your appreciation] -> Appreciate

===Applaud===
Caught up in the magic of the moment, you find yourself clapping along with the gathered crowd, an unspoken acknowledgement of the beauty you've all just experienced. #STR_Applaud1
->Leave

===Appreciate===
Your heart full of gratitude, you express your deep appreciation for the troubadours' performance. They exchange a knowing glance, their smiles warm and genuine.
#STR_Appreciate2
->Leave

===Leave===
With a final nod of appreciation, you leave the troubadours to continue their musical journey, carrying the memory of their impromptu performance with you as you resume your own path. #STR_Leave1
(Clicking on "End" will stop the music if it's still playing.) #STR_Leave2
->END