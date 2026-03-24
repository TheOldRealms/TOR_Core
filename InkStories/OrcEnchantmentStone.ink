//Global story tags
# title: 'Ow to make speshul fings
# frequency: Special
# development: false
# illustration: gs_enchant_table_1

INCLUDE include.ink

->START

===START===

+ [Turn da fing o'er]->SecondSide
+ [Stoopid fing, go away]->END

===SecondSide===
#illustration: gs_enchant_table_2

+ [Turn da fing o'er again]->START
+ [Stoopid fing, go away]->END

===ThirdSide===
You'z lookin' fer sumfin'? Stone carvin' can't 'ave 3 sides, ya git! 

+ [Turn da fing o'er AGAIN]->START
+ [Stoopid fing, go away]->END