/*
Possible skill names (case sensitive!)
    OneHanded
	TwoHanded
	Polearm
	Bow
	Crossbow
	Throwing
	Riding
	Athletics
	Scouting
	Tactics
	Crafting
	Roguery
	Charm
	Leadership
	Trade
	Steward
	Medicine
	Engineering
	Faith
	Gunpowder
	Spellcraft
*/

/*
Possible personality trait names (case sensitive!)
    Mercy
	Valor
	Honor
	Generosity
	Calculating
*/

/*
Possible school (lore) of magic names (case sensitive!)
    MinorMagic
    LoreOfFire
    LoreOfLight
    LoreOfHeavens
    LoreOfLife
    LoreOfBeasts
    DarkMagic
    Necromancy
*/

===function limit100(number)===
    ~ return "{number>100:100|{number}}"
    
    
===function print_player_skill_chance(skillName, skillLevelForCertainty)===
    ~ temp chance = limit100(INT(((GetPlayerSkillValue(skillName) / skillLevelForCertainty) * 100)))
    ~ SetPlayerSkillChance(skillName, chance)
    ~ return "({skillName} check - success chance " + chance + "%)"

===function perform_player_skill_check(skillName, skillLevelToCheckAgainst)===
    ~ return GetPlayerSkillValue(skillName) >= RANDOM(1,skillLevelToCheckAgainst)
    
===function print_party_skill_chance(skillName, skillLevelForCertainty)===
    ~ temp chance = limit100(INT(((GetPartySkillValue(skillName) / skillLevelForCertainty) * 100)))
    ~ SetPartySkillChance(skillName, chance)
    ~ return "({skillName} check - success chance " + chance + "%)"

===function perform_party_skill_check(skillName, skillLevelToCheckAgainst)===
    ~ return GetPartySkillValue(skillName) >= RANDOM(1,skillLevelToCheckAgainst)

    
===function print_player_attribute_chance(attributeName, attributeLevelForCertainty)===
    ~ temp chance = limit100(INT(((GetPlayerAttributeValue(attributeName) / attributeLevelForCertainty) * 100)))
    ~ SetPlayerAttributeChance(attributeName, chance)
    ~ return "({attributeName} check - success chance " + chance + "%)"

===function perform_player_attribute_check(attributeName, attributeLevelForCertainty)===
    ~ return GetPlayerAttributeValue(attributeName) >= RANDOM(1,attributeLevelForCertainty)

===function print_party_attribute_chance(attributeName, attributeLevelForCertainty)===
    ~ temp chance = limit100(INT(((GetPartyAttributeValue(attributeName) / attributeLevelForCertainty) * 100)))
    ~ SetPartyAttributeChance(attributeName, chance)
    ~ return "({attributeName} check - success chance " + chance + "%)"

===function perform_party_attribute_check(attributeName, attributeLevelForCertainty)===
    ~ return GetPartyAttributeValue(attributeName) >= RANDOM(1,attributeLevelForCertainty)

===function came_from(-> x)===
    ~ return TURNS_SINCE(x) == 0
    
    
EXTERNAL SetTextVariable(variableName, value)
===function SetTextVariable(variableName, value)===
    ~ return ""    
    
EXTERNAL SetPlayerSkillChance(skillname, skillLevelForCertainty)
===function SetPlayerSkillChance(skillName, skillLevelForCertainty)===
    ~ return ""
    
EXTERNAL SetPartySkillChance(skillname, skillLevelForCertainty)
===function SetPartySkillChance(skillName, skillLevelForCertainty)===
    ~ return ""
    
EXTERNAL SetPlayerAttributeChance(attributeName, attributeLevelForCertainty)
===function SetPlayerAttributeChance(skillName, skillLevelForCertainty)===
    ~ return ""

EXTERNAL SetPartyAttributeChance(attributeName, attributeLevelForCertainty)
===function SetPartyAttributeChance(skillName, skillLevelForCertainty)===
    ~ return ""
    


//Gets the current skill value of the player
//Takes the skill's name as argument
//The skillname is case sensitive string
//returns the skill value as a float
EXTERNAL GetPlayerSkillValue(skillname)
===function GetPlayerSkillValue(skillname)===
    ~return 100.0

//Gets the current highest skill value of any hero member of the player's party (player or companions)
//Takes the attribute's name as argument
//The skillname is case sensitive string
//returns the skill value as a float
EXTERNAL GetPartySkillValue(skillname)
===function GetPartySkillValue(skillname)===
    ~return 100.0

//Gets the current attribute value of the player
//Takes the attribute's name as argument
//The attributeName is case sensitive string
//returns the attribute value as a float, return values are between 1-10
EXTERNAL GetPlayerAttributeValue(attributeName)
===function GetPlayerAttributeValue(attributeName)===
    ~return 5.0

//Gets the current highest attribute value of any hero member of the player's party (player or companions)
//Takes the attribute's name as argument
//The attributeName is case sensitive string
//returns the attribute value as a float, return values are between 1-10
EXTERNAL GetPartyAttributeValue(attributeName)
===function GetPartyAttributeValue(attributeName)===
    ~return 5.0


//Gives skill experience to the player
//Takes the skill's name and the amount of xp to give as arguments
//The skillname is a case sensitive string, amount must be an integer
//returns nothing
EXTERNAL GiveSkillExperience(skillname, amount)
===function GiveSkillExperience(skillname, amount)===
    ~return ""

//Gets the value of a personality trait of the player
//Takes the trait's name as argument
//The traitname is a case sensitive string
//returns the trait's value as an integer - usually ranges between [-2 , 2]
EXTERNAL GetPlayerPersonalityTraitValue(traitname)
===function GetPlayerPersonalityTraitValue(traitname)===
    ~return 1

//Checks if the player party has any vampires
//takes a boolean as argument, if true, only the player is checked, if false, all hero members of the party are checked
//playeronly is a (true|false) boolean
//returns a (true|false) boolean
EXTERNAL PartyHasVampire(playeronly)
===function PartyHasVampire(playeronly)===
    ~ return true

//Checks if the player party has any necromancers
//takes a boolean as argument, if true, only the player is checked, if false, all hero members of the party are checked
//playeronly is a (true|false) boolean
//returns a (true|false) boolean
EXTERNAL PartyHasNecromancer(playeronly)
===function PartyHasNecromancer(playeronly)===
    ~ return true

//Checks if the player party has any spellcasters
//takes a boolean as argument, if true, only the player is checked, if false, all hero members of the party are checked
//playeronly is a (true|false) boolean
//returns a (true|false) boolean
EXTERNAL PartyHasSpellcaster(playeronly)
===function PartyHasSpellcaster(playeronly)===
    ~ return true

//Checks if the player party has any member with the knowledge of the passed in school of magic (lore of magic)
//takes a boolean as argument, if true, only the player is checked, if false, all hero members of the party are checked
//second argument is a case sensitive string with the lore's name
//playeronly is a (true|false) boolean
//lorename is a case sensitive string
//returns a (true|false) boolean
EXTERNAL DoesPartyKnowSchoolOfMagic(playeronly, lorename)
===function DoesPartyKnowSchoolOfMagic(playeronly, lorename)===
    ~ return true

//Gets the name of the closest settlement of the given type to the player's party
//takes a string as an argument with only the following possible values (town|village|castle)
//settlementtype is a non-case sensitive string from a finite list of possible values
//returns a string with the name of the settlement
EXTERNAL GetNearestSettlement(settlementtype)
===function GetNearestSettlement(settlementtype)===
    ~ return "ExampleSettlement"

//Gets the name of a random notable from the closest settlement to the player's party
//takes a string as an argument with only the following possible values (town|village) NOTE! No Castle!
//settlementtype is a non-case sensitive string from a finite list of possible values
//returns a string with the name of the notable
EXTERNAL GetRandomNotableFromNearestSettlement(settlementtype)
===function GetRandomNotableFromNearestSettlement(settlementtype)===
    ~ return "Random John"
    
//Gets the name of a random notable from a specific settlement
//takes a string as an argument with the name of the Settlement
//settlementname is a case-sensitive string
//returns a string with the name of the notable
EXTERNAL GetRandomNotableFromSpecificSettlement(settlementname)
===function GetRandomNotableFromSpecificSettlement(settlementname)===
    ~ return "Specific John"

//Method to change the count of some troop types in the player's party. Can add or remove as well based on whether the count is a positive or a negative number.
//takes a string as an argument that MUST match a valid troop ID from the game. If you are not sure how to look that up, bug hunharibo or Z3rca about it
//returns nothing
EXTERNAL ChangePartyTroopCount(troopId, count)
===function ChangePartyTroopCount(troopId, count)===
    ~ return ""

//Method to give gold to the player
//amount is an integer
EXTERNAL GiveGold(amount)
===function GiveGold(amount)===
    ~ return ""

//Method to give items to the player
//itemId is a case sensitive string, MUST match a valid item ID from the game. If you are not sure how to look that up, bug hunharibo or Z3rca about it
EXTERNAL GiveItem(itemId, amount)
===function GiveItem(itemId, amount)===
    ~ return ""

//Method to give relations with an NPC (can be negative)
//targetNPC is a case sensitive string, MUST match a valid npc name from the game. Can set directly or get the value through the randomnotable function
//changeAmount is an integer
EXTERNAL ChangeRelations(targetNPC, changeAmount)
===function ChangeRelations(targetNPC, changeAmount)===
    ~ return ""

//Method to add a value to influence the personality traits of the player
//changeByAmount is an integer
EXTERNAL AddTraitInfluence(traitname, changeByAmount)
===function AddTraitInfluence(traitname, changeByAmount)===
    ~ return ""

//Method to heal all hero members and all troops to full
EXTERNAL HealPartyToFull()
===function HealPartyToFull()===
    ~ return ""
    
//Get the total member count of the main party including heros
EXTERNAL GetTotalPartyMemberCount()
===function GetTotalPartyMemberCount()===
    ~ return 50

//Get the total wounded count of the main party including heros
EXTERNAL GetTotalPartyWoundedCount()
===function GetTotalPartyWoundedCount()===
    ~ return 20

//Set the main party disorganized
EXTERNAL MakePartyDisorganized()
===function MakePartyDisorganized()===
    ~ return ""
    
//Opens the duel mission
EXTERNAL OpenDuelMission()
===function OpenDuelMission()===
    ~ return ""

//Opens the cultist lair mission
EXTERNAL OpenCultistLairMission(missionName)
===function OpenCultistLairMission(missionName)===
    ~ return ""

//Gets player tag
EXTERNAL GetPlayerHasCustomTag(tag)
===function GetPlayerHasCustomTag(tag)===
    ~ return false
    
//Sets player tag
EXTERNAL SetPlayerCustomTag(tag)
===function SetPlayerCustomTag(tag)===
    ~ return ""
    
//Opens a random invetory as trade
EXTERNAL OpenInventoryAsTrade()
===function OpenInventoryAsTrade()===
    ~ return ""

//Checks whether the current time of day is night
EXTERNAL IsNight()
===function IsNight()===
    ~ return false

//Checks whether player has the indicated amount of gold
EXTERNAL HasEnoughGold(number)
===function HasEnoughGold(number)===
    ~ return true
    
//Plays a music from the sounds.xml
EXTERNAL PlayMusic(musicname)
===function PlayMusic(musicname)===
    ~ return ""

//Give an artifact item based on player's religion
EXTERNAL GiveMiracleItem()
===function GiveMiracleItem()===
    ~ return ""
    
//Resets all raiding sites (e.g. herdstones and chaos portal)
EXTERNAL ResetRaiderSites()
===function ResetRaiderSites()===
    ~ return ""