//Make sure you include this in all ink files to get access to integration functions
INCLUDE include.ink


//Variables setup
//IMPORTANT! Initial values are mandatory, but they can only be primitives (number, string, boolean). If we want to assign the return value of a function to the variable, we must do it on a separate line, see one line below
    
    // Determines the severity of the injury
    VAR InjuryRoll = 0 //not important initial value
        ~ InjuryRoll = RANDOM(0,2) 
    
    // Gets the name of the nearest settlement and stores it in the Settlement variable
    VAR Settlement = "DOESNTMATTER" //not important initial value
        ~ Settlement = GetNearestSettlement("town") 

    //Gets a random notable from Settlement !!!NOTE: the argument being passed in to the function is a variable itself
    VAR Notable = "DOESNTMATTER" //not important initial value
        ~ Notable = GetRandomNotableFromSpecificSettlement(Settlement) 

    //sets up a random chance whether horses are around or not
    VAR HorsesAround = "DOESNTMATTER" // not important initial value
        ~ HorsesAround = "{~true|false}" 

    //sets up a random chance for the profession of the injured man from a finite list
    VAR Profession = "DOESNTMATTER" // not important initial value
        ~ Profession = "{~merchant|farmer|blacksmith}"

    //Variable that changes to true if you choose to extort the man under the cart
    VAR IsExtorted = false //!!!NOTE: this is actually an important initial value that we are going to use (because its primitive, the assignment can be done on one line)
    
    //Variable that changes if you asked the stuck man for more information
    VAR IsAsked = false //important initial value
        
    //Variable for the rewards based on the profession of the injured man.
    VAR Reward = "DOESNTMATTER" 
        {
            - Profession == "merchant" : 
                ~ Reward = "500 gold"
            - Profession == "farmer" :
                ~ Reward = "5 grain"
            - Profession == "blacksmith" :
                ~ Reward = "2 steel ingots"
        }

-> Start

===Start===
Roadside accident #title #illustration: cart_accident

In the distance you see a cart.

As you approach the cart you can see that it is overturned{HorsesAround == true: and that the horses that were pulling it are grazing on some nearby grass}.

*[Approach the cart.] ->Approach
*[Go in another direction. (Leave)] ->Result.noreward

=== Approach ===

You approach the cart and find a man trying to get unstuck. When he sees you approaching he begs you for help.

You notice that the man trapped under the cart is {InjuryRoll == 0: uninjured}{InjuryRoll == 1: mildly injured}{InjuryRoll == 2: severly injured}.

When you approach he {InjuryRoll == 0: says}{InjuryRoll == 1: says}{InjuryRoll == 2: gasps}, "Please help me." ->choices

    =choices
    *[Help him (+Mercy, +Relations with {Notable})] ->AfterLift
    *[Ask what he can do for you if you help him.] ->MoreInfo
    *{HorsesAround == true}[Take the horses and leave(+2 low tier horses; --Mercy)] ->Result.takehorses
    *{IsAsked == true}[Extort for a reward ({Reward}; -Mercy)] ->Extortion
    *{IsAsked == true && HorsesAround == true}[Demand one of the horses in exchange for your help (+1 low tier horse; -Mercy)] ->Extortion
    *{PartyHasNecromancer(false) == true}[Kill the man, raise his corpse as a zombie, and loot his cart (+1 zombie, {Reward}, ---Mercy)]->Result.raisedead
    *[Leave him to his fate.] ->Result.noreward
    
    =MoreInfo
    ~IsAsked = true
    He replies, "I am just a simple {Profession} from {Settlement}, I cannot give you a reward other than my thanks."
    After a moment he says, "I am a friend of {Notable} and can put in a good word for you."
    ->Approach.choices
    
    =Extortion
    ~IsExtorted = true
    ->AfterLift

===AfterLift===

After lifting the cart the man {InjuryRoll == 0: gets up}{InjuryRoll == 1: barely stands up}{InjuryRoll == 2: lays there trying not to die}.
*{InjuryRoll == 0}[Talk to the uninjured man] ->succeed
*{InjuryRoll == 1}[Treat the man with medicine. (Medicine {(GetPartySkillValue("Medicine") / 100) * 100}%)]-> MildRecover
*{InjuryRoll == 1}{DoesPartyKnowSchoolOfMagic(false, "LoreOfLife") == true}[Treat the man with magic. (Spellcraft {(GetPartySkillValue("Spellcraft") / 100) * 100}%)]-> MildRecover
*{InjuryRoll == 2}[Treat the man with medicine. (Medicine {(GetPartySkillValue("Medicine") / 250) * 100}%)]-> SeverRecover
*{InjuryRoll == 2}{DoesPartyKnowSchoolOfMagic(false, "LoreOfLife") == true}[Treat the man with magic. (Spellcraft {(GetPartySkillValue("Spellcraft") / 250) * 100}%)]-> SeverRecover

    =MildRecover
    {GetPartySkillValue("Medicine") >= RANDOM(1,100): -> succeed | -> fail1}
    {GetPartySkillValue("Spellcraft") >= RANDOM(1,100): -> succeed | -> fail1}
    
    =SeverRecover
    {GetPartySkillValue("Medicine") >= RANDOM(1,250): -> succeed | -> fail2}
    {GetPartySkillValue("Spellcraft") >= RANDOM(1,250): -> succeed | -> fail2}

    =succeed
    You succeed in treating the man. He {IsExtorted == true: begrudingly} thanks you for your help {IsExtorted == true: and gives you the promised reward -> Result.givereward | -> Bonus}.
    =fail1
    You fail in completely treating the man. He is dissapointed as he will probably limp for the rest of his life, but {IsExtorted == true: begrudingly} thanks you nevertheless {IsExtorted == true: and gives you the promised money -> Result.givereward | {IsAsked == true: -> Result.relations | -> Result.noreward}}.
    =fail2
    Despite your best effort, the man dies. There is nothing more to do. ->Result.noreward
    
    =Bonus
    ~ temp BonusChance = RANDOM(0,100)
    {BonusChance >= 50: He also offers you a reward for your help! ({Reward}) -> Result.givereward | {IsAsked == true: -> Result.relations | -> Result.noreward}} 

===Result===

    =noreward
    {came_from(-> AfterLift.Bonus): {ChangeTraitValue("Mercy", 1)}}
    You continue your journey. ->END
    
    =takehorses
    {GiveItem("old_horse", 2)}
    {ChangeTraitValue("Mercy", -2)}
    You take two old work horses and leave. -> END
    
    =raisedead
    {ChangePartyTroopCount("tor_vc_skeleton", 1)} -> givereward
    
    =givereward
    {
        - Reward == "500 gold" : {GiveGold(500)}
        - Reward == "5 grain" : {GiveItem("grain", 5)}
        - Reward == "2 steel ingots" : {GiveItem("ironIngot4", 2)}
    }
    {IsExtorted == true: {ChangeTraitValue("Mercy", -1)}}
    {IsExtorted == false && came_from(-> AfterLift.Bonus): {ChangeTraitValue("Mercy", 1)}}
    {came_from(-> raisedead): {ChangeTraitValue("Mercy", -3)} Looting the cart you find | You recieve } {Reward}. -> END
    
    =relations
    {ChangeRelations(Notable, 2)}
    Words about your good deed will reach {Notable}. -> END
    
-> END