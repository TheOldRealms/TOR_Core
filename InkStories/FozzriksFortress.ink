//Global story tags
# title: Fozzrik's Fortress
# frequency: Uncommon
# development: false
# illustration: castle

INCLUDE include.ink

-> Start

=== Start ===
Your journey through the untamed wilderness brings you to the edge of a dense forest. Emerging from the trees, you come upon a sight both magnificent and mysterious – a towering citadel that seems to defy the laws of nature itself. #STR_Start1

As you approach the base of the citadel, your eyes trace the intricate carvings adorning its walls, each telling a story of ages past. Towering spires crowned by swirling winds of magic reach towards the heavens, a display of both power and elegance. It is a castle unlike any other, held aloft by a magical artifice long forgotten by most, coveted by emperors and kings across the lands. The Flying Fortress is a wonder to behold – a monument to a wizard's dreams given solid form. #STR_Start2

What path shall you tread? #STR_Start3

* [Investigate the citadel closer.] -> InvestigateCitadel
* [Continue on.] -> ContinueOn

=== InvestigateCitadel ===
Curiosity compels you to draw nearer, your steps echoing in the presence of the towering citadel. Before you can approach, an extraordinary spectacle unfolds before your very eyes. The citadel's architecture stirs to life, responding to an enigmatic force that seems to emanate from within. #STR_InvestigateCitadel

Stone walls fold upon stone walls. The grandeur of the citadel diminishes with each graceful fold, its imposing structure transforming into a fraction of its previous size. In a matter of moments, what was once a monumental fortress is now reduced to a mere semblance of itself – a sight that leaves you spellbound. #STR_InvestigateCitade2

As you reach the spot where the citadel once stood, there remains only an empty space, as if the very earth had swallowed it whole. A mixture of awe and bewilderment fills your heart, urging you to fathom the mysteries of the magical phenomenon at play. #STR_InvestigateCitade3

* [Use your knowledge of magic to detect what's unique about the fortress. {print_party_skill_chance("Spellcraft", 200)}]-> SpellcraftCheck
* [Dismiss this phenomenon.]-> DismissPhenomenon

=== SpellcraftCheck ===
{perform_party_skill_check("Spellcraft",200): -> success | -> fail}

    =success
    (SUCCESS)
    Drawing upon your knowledge of magic, you attempt to decipher what is truly going on. Realization then dawns upon you. The legends of Fozzrik, the enigmatic wizard architect, resonate with what you've witnessed. #STR_SpellcraftCheckSuccess1
    
    The citadel you've encountered, now vanished, aligns perfectly with the tales of Fozzrik's awe-inspiring Floating Fortresses. These grand constructs could fold themselves into compact forms, defying logic as they transformed into objects as small as a chest, or expand into towering citadels at will. Your insight pierces through the mystique, revealing the workings of Fozzrik's artistry. #STR_SpellcraftCheckSuccess2
    
    With newfound understanding, you step forward, your knowledge of the citadel's nature illuminating your path in the wilderness. #STR_SpellcraftCheckSuccess3
    -> END
 
    =fail
    (FAIL)
    As you strain your mind to unravel the secrets of the vanished citadel, you find yourself at an impasse. The intricacies of this craftsmanship remain shrouded in enigma, eluding your attempts at understanding. The citadel's disappearance stands as a testament to the unfathomable nature of magical arts, leaving you with a lingering sense of curiosity tinged with frustration. Despite your best efforts, the riddle of this architecture remains unsolved, a mystery that joins the ranks of countless other enigmas in the world. #STR_SpellcraftCheckFail1
    -> END

=== DismissPhenomenon ===
Though the vanishing of the citadel bewilders, you choose to set aside the enigma and focus on your continued journey. The mysteries are known to elude even the most astute minds, and pondering them might lead you astray from your goals. #STR_DismissPhenomenon1


-> END

=== ContinueOn ===
With the memory of the vanished citadel etched into your mind, you resume your travels. #STR_ContinueOn1

As you continue, the legacy of the fortress lingers, a testament to the fusion of magic and architecture that defies ordinary perception in this world. #STR_ContinueOn2

* -> END
