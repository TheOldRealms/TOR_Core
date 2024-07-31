using TaleWorlds.CampaignSystem;
using TaleWorlds.Localization;
using TOR_Core.CharacterDevelopment;
using TOR_Core.CharacterDevelopment.CareerSystem.Button;
using TOR_Core.CharacterDevelopment.CareerSystem.CareerButton;
using TOR_Core.Extensions;

namespace TOR_Core.CampaignMechanics.Careers;

public static class CareerButtonDialogs
{
    public static void OnSessionLaunched(CampaignGameStarter starter)
    {
        MercenaryButtonDialog(starter);
        GrailKnightButtonDialog(starter);
    }

    private static void GrailKnightButtonDialog(CampaignGameStarter starter)
    {
            starter.AddDialogLine("grailKnightCompanionHonoring_start1", "start", "grailKnightCompanionHonoring_vow1", new TextObject("{=str_tor_grail_companion_vow_start}Hello Lord, it is rare that I have the pleasure of speaking with you. How can I serve you?").ToString(), () => buttonDialogCondition() && Hero.MainHero.HasCareer(TORCareers.GrailKnight), DeactivateDialog, 200, null);
            starter.AddPlayerLine("grailKnightCompanionHonoring_vow1", "grailKnightCompanionHonoring_vow1", "grailKnightCompanionHonoring_vow2", new TextObject("{=str_tor_grail_companion_vow_1}You have served me well for some time now and like I have completed your quest for the grail and have passed the lady’s secret trials.").ToString(), null, null, 200,
                null);

            starter.AddDialogLine("grailKnightCompanionHonoring_vow2", "grailKnightCompanionHonoring_vow2", "grailKnightCompanionHonoring_vow3", new TextObject("{=str_tor_grail_companion_vow_2}I would serve no other! What an honour to fight by the side of a fellow Grail Knight, especially one as accomplished and renowned as you.").ToString(), null, null, 200,
                null);
            starter.AddPlayerLine("grailKnightCompanionHonoring_vow3", "grailKnightCompanionHonoring_vow3", "grailKnightCompanionHonoring_vow4",
                new TextObject("{=str_tor_grail_companion_vow_3}I wish to join you to myself, as a member of my family and inner circle. In such a role you will lead my men into battle, handle what tasks I set aside for you and I may even grant you your own band to campaign with.").ToString(), null, null, 200, null);
            starter.AddPlayerLine("grailKnightCompanionHonoring_vow3_end", "grailKnightCompanionHonoring_vow3", "close_window", new TextObject("{=str_tor_grail_companion_vow_3}Thank you, it was good speaking with you.").ToString(), null, null, 200, null);

            starter.AddDialogLine("grailKnightCompanionHonoring_vow4", "grailKnightCompanionHonoring_vow4", "grailKnightCompanionHonoring_vow5", new TextObject("{=str_tor_grail_companion_vow_4}I am flattered, truly my lord, I accept wholeheartedly!").ToString(), null, MakeGrailKnightCompanion, 200, null);
            starter.AddDialogLine("grailKnightCompanionHonoring_vow5", "grailKnightCompanionHonoring_vow5", "close_window", new TextObject("{=str_tor_grail_companion_vow_5}Thank you, it was good speaking with you.").ToString(), null, null, 200, null);
            
            void MakeGrailKnightCompanion()
            {
                var button = (GrailKnightCareerButtonBehavior) CareerButtons.Instance.GetCareerButton(Hero.MainHero.GetCareer());
                if (button != null)
                { 
                    button.MakeGrailKnightCompanion();
                }
                
            }
            
            
    }


    private static void MercenaryButtonDialog(CampaignGameStarter starter)
    {
            starter.AddDialogLine("mercenaryCompanion_bodyguard_start", "start", "mercenaryCompanion_bodyguard_1", new TextObject("Aye what can I do for you?").ToString(), () => buttonDialogCondition() && Hero.MainHero.HasCareer(TORCareers.Mercenary), DeactivateDialog, 200,null);
            starter.AddPlayerLine("mercenaryCompanion_bodyguard_1", "mercenaryCompanion_bodyguard_1", "mercenaryCompanion_bodyguard_2",new TextObject("Your employment has gone quite well and I want to bring you on as a partner").ToString(), null, null,200, null);
            starter.AddDialogLine("mercenaryCompanion_bodyguard_2", "mercenaryCompanion_bodyguard_2", "mercenaryCompanion_bodyguard_3", new TextObject("As a partner?").ToString(), null, null, 200,null);
            starter.AddPlayerLine("mercenaryCompanion_bodyguard_3", "mercenaryCompanion_bodyguard_3", "mercenaryCompanion_bodyguard_4",new TextObject("You get part of the share and need to accomplish a few advanced organisational matters. I will however not pay your wage anymore.").ToString(), null, null,200, null);
            starter.AddDialogLine("mercenaryCompanion_bodyguard_4", "mercenaryCompanion_bodyguard_4", "mercenaryCompanion_bodyguard_5", "First I want to see some hard coin. I am not playing Babysitter or 'Partner' without seeing some money first. You pay me {MERCCOMPANIONPRICE}{GOLD_ICON}", null,null);
            starter.AddPlayerLine("mercenaryCompanion_bodyguard_5", "mercenaryCompanion_bodyguard_5", "mercenaryCompanion_bodyguard_paymentSuccess",new TextObject("Of course, consider this a forward on your upcoming shares.").ToString(), MercenaryButtonSucessCondition, null,200, null);
            starter.AddPlayerLine("mercenaryCompanion_bodyguard_5", "mercenaryCompanion_bodyguard_5", "mercenaryCompanion_bodyguard_paymentFail",new TextObject("I don’t have that in hand right now.").ToString(), null,null);
            starter.AddDialogLine("mercenaryCompanion_bodyguard_paymentSuccess", "mercenaryCompanion_bodyguard_paymentSuccess", "mercenaryCompanion_bodyguard_end_success", new TextObject("Thats a good deal, I am looking foward into this partnership").ToString(), null,MakeMercenaryCompanion);
            starter.AddDialogLine("mercenaryCompanion_bodyguard_paymentSuccess", "mercenaryCompanion_bodyguard_paymentFail", "mercenaryCompanion_bodyguard_end_fail", new TextObject("Well then I stay with my current wage then.").ToString(), null,null);
            starter.AddPlayerLine("mercenaryCompanion_bodyguard_end_success", "mercenaryCompanion_bodyguard_end_success", "close_window", new TextObject("What a wise decision!").ToString(), null,null);
            starter.AddPlayerLine("mercenaryCompanion_bodyguard_end_fail", "mercenaryCompanion_bodyguard_end_fail", "close_window", new TextObject("Fine.").ToString(), null,null);
            
            
            bool MercenaryButtonSucessCondition()
            {
                var button = (MercenaryCareerButtonBehavior) CareerButtons.Instance.GetCareerButton(Hero.MainHero.GetCareer());
                if (button != null)
                { 
                    return button.PlayerHasMoney();
                }

                return false;
            }

            void MakeMercenaryCompanion()
            {
                var button = (MercenaryCareerButtonBehavior) CareerButtons.Instance.GetCareerButton(Hero.MainHero.GetCareer());
                if (button != null)
                { 
                    button.MakeMercenaryCompanion();
                }
                
            }
    }

    
    private static bool buttonDialogCondition()
    {
        var button = CareerButtons.Instance.GetCareerButton(Hero.MainHero.GetCareer());
        if (button != null)
        {
            return  button.isDialogStart;
        }

        return false;
    }

    private static void DeactivateDialog()
    {
        var button = CareerButtons.Instance.GetCareerButton(Hero.MainHero.GetCareer());
        button.DeactivateDialog();
    }

}