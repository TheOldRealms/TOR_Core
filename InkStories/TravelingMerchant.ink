//Global story tags
# title: Traveling Merchant
# frequency: Abundant
# development: false
# illustration: trader

INCLUDE include.ink

->START

===START===
While journeying along the dusty roads, a unique sight unfolds before your eyes — a colorful caravan, a mobile emporium amidst the quiet landscape. 
As you draw near, a traveling merchant approaches. With a welcoming smile, he introduces you to tales of distant realms and beckons you to take a look at his wares.
->choices
    
    =choices
    +[Browse his wares]
        ~ OpenInventoryAsTrade()
    ->AfterShopping
    *[Continue your journey (Leave)] You decide it is better to move on for now.->END

===AfterShopping===
You conclude your exploration of the merchant's array of treasures, and with a respectful bow of his head, he extends his appreciation for your interest and choices.
    -> END
