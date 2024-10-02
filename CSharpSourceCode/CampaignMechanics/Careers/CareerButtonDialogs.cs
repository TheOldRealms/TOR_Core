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
            starter.AddDialogLine("grailKnightCompanionHonoring_start1", "start", "grailKnightCompanionHonoring_vow1", new TextObject("{=str_tor_grail_companion_vow_start}Greetings my lord, how may I be of service? ").ToString(), () => buttonDialogCondition() && Hero.MainHero.HasCareer(TORCareers.GrailKnight), DeactivateDialog, 200, null);
            starter.AddPlayerLine("grailKnightCompanionHonoring_vow1", "grailKnightCompanionHonoring_vow1", "grailKnightCompanionHonoring_vow2", new TextObject("{=str_tor_grail_companion_vow_1}It has been some time since you have come into my service, you have repeatedly proven to be a most capable and chivalrous Knight.").ToString(), null, null, 200,
                null);

            starter.AddDialogLine("grailKnightCompanionHonoring_vow2", "grailKnightCompanionHonoring_vow2", "grailKnightCompanionHonoring_vow3", new TextObject("{=str_tor_grail_companion_vow_2}You honour me with your words my lord, it is my pride and pleasure to serve and fight alongside you.").ToString(), null, null, 200,
                null);
            starter.AddPlayerLine("grailKnightCompanionHonoring_vow3", "grailKnightCompanionHonoring_vow3", "grailKnightCompanionHonoring_vow4",
                new TextObject("{=str_tor_grail_companion_vow_3}I wish to bring you into my inner circle, to make you a leader and a trusted companion amongst my men; more than just a Knight in my service. What say you?").ToString(), null, null, 200, null);
            starter.AddPlayerLine("grailKnightCompanionHonoring_vow3_end", "grailKnightCompanionHonoring_vow3", "close_window", new TextObject("{=str_tor_grail_companion_vow_3}I look forward to our future battles together.").ToString(), null, null, 200, null);

            starter.AddDialogLine("grailKnightCompanionHonoring_vow4", "grailKnightCompanionHonoring_vow4", "grailKnightCompanionHonoring_vow5", new TextObject("{=str_tor_grail_companion_vow_4}It would be my honour and in the name of the Lady I swear this; to serve you until my dying breath.").ToString(), null, MakeGrailKnightCompanion, 200, null);
            starter.AddDialogLine("grailKnightCompanionHonoring_vow5", "grailKnightCompanionHonoring_vow5", "close_window", new TextObject("{=str_tor_grail_companion_vow_5}I look forward to our future battles together.").ToString(), null, null, 200, null);
            
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
            starter.AddPlayerLine("mercenaryCompanion_bodyguard_1", "mercenaryCompanion_bodyguard_1", "mercenaryCompanion_bodyguard_2",new TextObject("Your employment has gone quite well and I want to bring you on as a partner.").ToString(), null, null,200, null);
            starter.AddDialogLine("mercenaryCompanion_bodyguard_2", "mercenaryCompanion_bodyguard_2", "mercenaryCompanion_bodyguard_3", new TextObject("As a partner?").ToString(), null, null, 200,null);
            starter.AddPlayerLine("mercenaryCompanion_bodyguard_3", "mercenaryCompanion_bodyguard_3", "mercenaryCompanion_bodyguard_4",new TextObject("You get part of the share and need to accomplish a few advanced organisational matters. I will however not pay your wage anymore.").ToString(), null, null,200, null);
            starter.AddDialogLine("mercenaryCompanion_bodyguard_4", "mercenaryCompanion_bodyguard_4", "mercenaryCompanion_bodyguard_5", "First I want to see some hard coin. I am not playing Babysitter or 'Partner' without seeing some money first. You pay me {MERCCOMPANIONPRICE}{GOLD_ICON}", null,null);
            starter.AddPlayerLine("mercenaryCompanion_bodyguard_5", "mercenaryCompanion_bodyguard_5", "mercenaryCompanion_bodyguard_paymentSuccess",new TextObject("Of course, consider this a forward on your upcoming shares.").ToString(), MercenaryButtonSucessCondition, null,200, null);
            starter.AddPlayerLine("mercenaryCompanion_bodyguard_5", "mercenaryCompanion_bodyguard_5", "mercenaryCompanion_bodyguard_paymentFail",new TextObject("I don’t have that in hand right now.").ToString(), null,null);
            starter.AddDialogLine("mercenaryCompanion_bodyguard_paymentSuccess", "mercenaryCompanion_bodyguard_paymentSuccess", "mercenaryCompanion_bodyguard_end_success", new TextObject("Thats a good deal, I am looking foward to this partnership.").ToString(), null,MakeMercenaryCompanion);
            starter.AddDialogLine("mercenaryCompanion_bodyguard_paymentSuccess", "mercenaryCompanion_bodyguard_paymentFail", "mercenaryCompanion_bodyguard_end_fail", new TextObject("Well then I stay with my current wage.").ToString(), null,null);
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