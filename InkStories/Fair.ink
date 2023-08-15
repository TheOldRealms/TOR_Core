-> Start

=== Start ===
A Fair in the Woods #title #illustration: fair_warhammer

As your army travels, a sudden clearing reveals a surprising sight – a bustling fair known as the Morrslieb Revelry. Tents stand proud, colors dancing in the dappled sunlight. Laughter mingles with the snorts of horses, the heart of this joyous gathering.

Merchants beckon, their eyes alight with mischief, hawking horses at a discount from the standard rates you'd find in the scrolls. The air carries the tempting scents of roasted meat, frothy ale, and tangy wine. Amidst the merry crowd, a farmer grins, offering a turnip that oddly resembles the famed twin-tailed comet. Yours for a mere penny, a chance to possess this curious marvel.

* [Join the crowd at the horse market.]->HorseStalls
* [Savor the flavors of the fair.]->FoodStalls
* [Test your luck at the horse races.]->HorseRaces
* [Examine the peculiar turnip.]->Turnip
* [Continue your journey through the woods.]->Leave

=== HorseStalls ===
The fair's heart beats strongest at the horse market. Proud stallions prance, their eyes fierce and wild.

* [Take his deal for a horse.]->BuyHorse
* [Persuade the merchant to lower the price.]->PersuadeMerchant
* [Return to the fair's heart.]->Start

=== BuyHorse ===
You secure yourself a brand new steed, a fine Imperial warhorse.

* [Return to the revelry]->Start

=== PersuadeMerchant ===
You decide to use your charm and haggling skills to lower the price of the horse.

(CharmSkillCheckText)
* (CharmSkillCheckSuccess)[Successfully persuade the merchant to lower the price]->SuccessfulPersuasion
* (CharmSkillCheckFail)[Fail to persuade the merchant, pay full price]->ReturnToStalls

=== SuccessfulPersuasion ===
Your words work their magic, and the merchant agrees to lower the price by 25%. The merchant grumbles but respects your negotiating skills.

* [Purchase a horse.]->Start
* [Do not purchase a horse.]->Start

=== ReturnToStalls ===
Despite your best attempts to haggle, the merchant remains firm on the price. Do you still wish to purchase a horse from him?

* [Purchase a horse.]->Start
* [Do not purchase a horse.]->Start

=== FoodStalls ===
Scents swirl and tempt, guiding you to a feast of flavors. Meats sizzle and ale froths – a carnival for the senses. There's plenty of food available, and it's your choice to partake.

* [Indulge in the fair's feast.]->BuyFood
* [Haggle with the food vendor for a better deal.]->HaggleFood
* [Carry on, resisting the temptation.]->Start

=== BuyFood ===
Indulgence wins. You feast, the fair's flavors a delightful symphony on your tongue. Merchants nod their approval as you partake.

* [Return to the merriment.]->Start

=== HaggleFood ===
You decide to use your trade skills to haggle with the food vendor for a better deal.

(TradeSkillCheckText)
* (TradeSkillCheckSuccess)[Successfully haggle for a discount]->SuccessfulHaggling
* (TradeSkillCheckFail)[Fail to haggle, pay full price]->ReturnToFeast

=== SuccessfulHaggling ===
Your trading skills shine as you negotiate a discount on the feast. The vendor begrudgingly accepts your offer.

* [Return to the merriment.]->Start

=== ReturnToFeast ===
The vendor remains firm on the price, and you decide to move on.

* [Return to the merriment.]->Start

=== HorseRaces ===
Cheers erupt from an amphitheater. Horses thunder, riders urging them to glory.

* [Place a wager on a racing horse.]->PlaceBetWin
* [Place a wager on a racing horse.]->PlaceBetLose
* [Observe from the outskirts.]->Start

=== PlaceBetWin ===
Your heart races as you place your wager. The horse you chose surges forward, and luck dances in your favor. Laughter and clinking coins surround you.

* [Return to the merry crowd.]->Start

=== PlaceBetLose ===
Your heart races as you place your wager. The horse you chose quickly surges forward at first, but the other riders soon catch up. Eventually, your horse slows down to the point of only earning a late place. Laughter and clinking coins surround you.

* [Return to the merry crowd.]->Start

=== Turnip ===
Intrigue tugs at your senses as you gaze upon the comet-shaped turnip – a whimsical marvel. A farmer grins, inviting you to join a raffle.

* [Try your luck with a raffle ticket.]->BuyTicket
* [Use your perception to find hidden clues about the turnip]->PerceiveTurnip
* [Move on, leaving the curious turnip behind.]->Start

=== BuyTicket ===
With a coin and a smile, you secure your chance at the raffle. Who knows? The comet-kissed turnip might be yours after all.

* [Return to the mirthful revelry.]->Start

=== PerceiveTurnip ===
You decide to use your keen perception to examine the turnip more closely.

(PerceptionSkillCheckText)
* (PerceptionSkillCheckSuccess)[Notice hidden details about the turnip]->SuccessfulPerception
* (PerceptionSkillCheckFail)[Fail to perceive anything unusual]->ContinueExamination

=== SuccessfulPerception ===
Your sharp eyes pick up on subtle details that others might miss. The turnip seems to have no strange markings that hint at its significance. It's completely ordinary.

* [Return to the mirthful revelry.]->Start

=== ContinueExamination ===
Your examination doesn't reveal anything unusual about the turnip.

* [Return to the mirthful revelry.]->Start

=== Leave ===
As the fair's merriment fades, you step back into the embrace of the wilderness, leaving the laughter of the fair's revelry behind.

* [Continue your path.]->END
