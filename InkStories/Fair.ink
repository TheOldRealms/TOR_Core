//Global story tags
# title: A Fair in the Woods
# frequency: Uncommon
# development: false
# illustration: trader

INCLUDE include.ink

VAR HorsePrice = 2000
VAR FoodPrice = 10
VAR HorseBetPrice = 500
VAR HorseBetPayout = 2500
VAR WinHorseRace = 0
    ~ WinHorseRace = RANDOM(0,1)
VAR TurnipPrice = 50

-> Start

===Start===




As your army travels, a sudden clearing reveals a surprising sight – a bustling fair known as the Morrslieb Revelry. Tents stand proud, colors dancing in the dappled sunlight. Laughter mingles with the snorts of horses, the heart of this joyous gathering. #STR_Start1

Merchants beckon, their eyes alight with mischief, hawking horses at a discount from the standard rates you'd find in the scrolls. The air carries the tempting scents of roasted meat, frothy ale, and tangy wine. Amidst the merry crowd, a farmer grins, offering a turnip that oddly resembles the famed twin-tailed comet. Yours for a mere penny, a chance to possess this curious marvel. #STR_Start2
    ->choices

    =choices
    * [Join the crowd at the horse market.]->HorseStalls 
    * [Savor the flavors of the fair.]->FoodStalls
    * [Test your luck at the horse races.]->HorseRaces
    * [Examine the peculiar turnip.]->Turnip
    * [Continue your journey through the woods.]->Leave

===HorseStalls===
The fair's heart beats strongest at the horse market. Proud stallions prance, their eyes fierce and wild. One horse, in particular, captures your attention. It has a sleek, ebony coat that glistens in the sunlight, and its eyes seem to hold a knowing glint. #STR_HorseStalls1
    ->choices

    =choices
    + [Take the merchant's deal for the horse. ({HorsePrice} gold)]->BuyHorse
    * [Persuade the merchant to lower the price. {print_player_skill_chance("Charm",150)}]->PersuadeMerchant
    * [Return to the fair's heart.]->Start.choices

===BuyHorse===
{HasEnoughGold(HorsePrice): You strike a deal with the merchant. You exchange coins for a sturdy saddle and reins. With a surge of anticipation, you mount the horse. The connection between you is immediate, the horse seems to respond to your touch with trust and eagerness. {GiveGold(-HorsePrice)} {GiveItem("t2_empire_horse",1)} | You don't have enough gold. #STR_BuyHorse1NOTENOUGHGOLD }  #STR_BuyHorse1

* [Return to the revelry]->Start.choices

===PersuadeMerchant===
{perform_player_skill_check("Charm",150): -> success | -> fail}

    =success
    Your words work their magic, and the merchant agrees to lower the price by 50%. The merchant grumbles but respects your negotiating skills. #STR_PersuadeMerchant_Success
    ~HorsePrice = 1000
    ->HorseStalls.choices

    =fail
    Despite your best attempts to haggle, the merchant remains firm on the price. #STR_PersuadeMerchant_Fail
    ->HorseStalls.choices
    

===FoodStalls===
Scents swirl and tempt, guiding you to a feast of flavors. Meats sizzle and ale froths – a carnival for the senses. There's plenty of food available, and it's your choice to partake. #STR_FoodStalls1

* [Indulge in the fair's feast. ({FoodPrice} gold)]->BuyFood
* [Carry on, resisting the temptation.]->Start

===BuyFood===
{HasEnoughGold(FoodPrice): Indulgence wins. You feast, the fair's flavors a delightful symphony on your tongue. Merchants nod their approval as you partake. {GiveGold(-FoodPrice)} | You don't have enough gold. #STR_BuyFood1NOTENOUGHGOLD}#STR_BuyFood1

* [Return to the merriment.]->Start.choices

===HorseRaces===
Cheers erupt from an amphitheater. Horses thunder, riders urging them to glory. #STR_HorseRaces
->choices

    =choices
    * [Place a wager on a racing horse. ({HorseBetPrice} gold - payout 5x on win)]->PlaceBet
    * [You decide that you shouldn't test your luck.]->Start.choices

===PlaceBet===
{not HasEnoughGold(HorseBetPrice): You don't have enough gold. #STR_PlaceBet_NOTENOUGHGOLD -> HorseRaces.choices } 
~GiveGold(-HorseBetPrice)
{WinHorseRace: ->success | ->fail}
    =success
    Your heart races as you place your wager. The horse you chose surges forward, and luck dances in your favor. Laughter and clinking coins surround you. #STR_PlaceBet_Success
    ~ GiveGold(HorseBetPayout)
    * [Return to the merry crowd.]->Start.choices

    =fail
    Your heart races as you place your wager. The horse you chose quickly surges forward at first, but the other riders soon catch up. Eventually, your horse slows down to the point of only earning a late place. Laughter and clinking coins surround you.
        #STR_PlaceBet_Fail
    * [Return to the merry crowd.]->Start.choices

===Turnip===
Intrigue tugs at your senses as you gaze upon the comet-shaped turnip – a whimsical marvel. A farmer grins, inviting you to join a raffle. #STR_Turnip1
    ->choices

    =choices
    * [Try your luck with a raffle ticket. ({TurnipPrice} gold)]->BuyTicket
    * [Use your perception to find hidden clues about the turnip. {print_player_skill_chance("Roguery", 80)}]->PerceiveTurnip
    * [Move on, leaving the curious turnip behind.]->Start.choices

===BuyTicket===
{HasEnoughGold(TurnipPrice): With a coin and a smile, you secure your chance at the raffle. Who knows? The comet-kissed turnip might be yours after all. {GiveGold(-TurnipPrice)} | You don't have enough gold. #STR_BuyTicket1NOTENOUGHGOLD  -> Turnip.choices}  #STR_BuyTicket1

With anticipation in the air, the raffle commences, and as the announcer calls out the winning ticket number, you hold your breath. However, luck is not on your side this time. The winning number isn't yours, and a twinge of disappointment washes over you. #STR_BuyTicket2

* [Return to the mirthful revelry.]->Start.choices

=== PerceiveTurnip ===
{perform_player_skill_check("Roguery", 80): -> success | ->fail}

    =success
    Your sharp eyes pick up on subtle details that others might miss. The turnip seems to have no strange markings that hint at its significance. It's completely ordinary.   #STR_PerceiveTurnip_Success
    ->Turnip.choices
    
    =fail
    Your examination doesn't reveal anything unusual about the turnip. #STR_PerceiveTurnip_Success
    ->Turnip.choices

===Leave===
As the fair's merriment fades, you step back into the embrace of the wilderness, leaving the laughter of the fair's revelry behind. #STR_Leave1
->END
