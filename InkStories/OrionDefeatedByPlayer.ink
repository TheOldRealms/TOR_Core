# title: Orion Defeated
# frequency: Unique
# development: false
# illustration: meadow

INCLUDE include.ink

-> Start

===Start===
You defeated Orion. #STR_Start1

The Wild Hunt has been broken. #STR_Start2

Among the spoils left in his wake lies a charm steeped in the hatred of beast-kind. #STR_Start3

Pick your reward. #STR_Start4

+ [Bane of the Beastkin]
    ~ LearnEnchantmentBlueprint("asrai_enchant_bane_beastkin")
    -> END