# title: Orion Defeated
# frequency: Unique
# development: false
# illustration: meadow

INCLUDE include.ink

-> Start

===Start===
You defeated Orion. #STR_Start1

The Wild Hunt has been broken. #STR_Start2

Among the spoils left in his wake lies an enchantment (...) #STR_Start3

Claim your reward. #STR_Start4

+ [Claim the enchantment]
    ~ LearnRandomUnknownOrionEnchantment()
    -> END