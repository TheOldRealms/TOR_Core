using TaleWorlds.CampaignSystem;
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
        starter.AddDialogLine("grailKnightCompanionHonoring_start1", "start", "grailKnightCompanionHonoring_vow1", TORTextHelper.GetTextForNative("tor_grail_companion_vow_start", "Greetings my lord, how may I be of service?"), () => buttonDialogCondition() && Hero.MainHero.HasCareer(TORCareers.GrailKnight), DeactivateDialog, 200, null);
        starter.AddPlayerLine("grailKnightCompanionHonoring_vow1", "grailKnightCompanionHonoring_vow1", "grailKnightCompanionHonoring_vow2", TORTextHelper.GetTextForNative("tor_grail_companion_vow_1", "It has been some time since you have come into my service, you have repeatedly proven to be a most capable and chivalrous Knight."), null, null, 200, null);

        starter.AddDialogLine("grailKnightCompanionHonoring_vow2", "grailKnightCompanionHonoring_vow2", "grailKnightCompanionHonoring_vow3", TORTextHelper.GetTextForNative("tor_grail_companion_vow_2", "You honour me with your words my lord, it is my pride and pleasure to serve and fight alongside you."), null, null, 200, null);
        starter.AddPlayerLine("grailKnightCompanionHonoring_vow3", "grailKnightCompanionHonoring_vow3", "grailKnightCompanionHonoring_vow4",
            TORTextHelper.GetText("tor_grail_companion_vow_3", "I wish to bring you into my inner circle, to make you a leader and a trusted companion amongst my men; more than just a Knight in my service. What say you?"), null, null, 200, null);
        starter.AddPlayerLine("grailKnightCompanionHonoring_vow3_end", "grailKnightCompanionHonoring_vow3", "close_window", TORTextHelper.GetTextForNative("tor_grail_companion_vow_3_end", "I look forward to our future battles together."), null, null, 200, null);

        starter.AddDialogLine("grailKnightCompanionHonoring_vow4", "grailKnightCompanionHonoring_vow4", "grailKnightCompanionHonoring_vow5", TORTextHelper.GetTextForNative("tor_grail_companion_vow_4", "It would be my honour and in the name of the Lady I swear this; to serve you until my dying breath."), null, MakeGrailKnightCompanion, 200, null);
        starter.AddDialogLine("grailKnightCompanionHonoring_vow5", "grailKnightCompanionHonoring_vow5", "close_window", TORTextHelper.GetTextForNative("tor_grail_companion_vow_5", "I look forward to our future battles together."), null, null, 200, null);

        void MakeGrailKnightCompanion()
        {
            var button = (GrailKnightCareerButtonBehavior)CareerButtons.Instance.GetCareerButton(Hero.MainHero.GetCareer());
            if (button != null)
            {
                button.MakeGrailKnightCompanion();
            }
        }
    }


    private static void MercenaryButtonDialog(CampaignGameStarter starter)
    {
        starter.AddDialogLine("mercenaryCompanion_bodyguard_start", "start", "mercenaryCompanion_bodyguard_1", TORTextHelper.GetTextForNative("tor_mercenary_companion_start_text", "Aye what can I do for you?"), () => buttonDialogCondition() && Hero.MainHero.HasCareer(TORCareers.Mercenary), DeactivateDialog, 200, null);
        starter.AddPlayerLine("mercenaryCompanion_bodyguard_1", "mercenaryCompanion_bodyguard_1", "mercenaryCompanion_bodyguard_2", TORTextHelper.GetTextForNative("tor_mercenary_companion_partner_offer_text", "Your employment has gone quite well and I want to bring you on as a partner."), null, null, 200, null);
        starter.AddDialogLine("mercenaryCompanion_bodyguard_2", "mercenaryCompanion_bodyguard_2", "mercenaryCompanion_bodyguard_3", TORTextHelper.GetTextForNative("tor_mercenary_companion_partner_question_text", "As a partner?"), null, null, 200, null);
        starter.AddPlayerLine("mercenaryCompanion_bodyguard_3", "mercenaryCompanion_bodyguard_3", "mercenaryCompanion_bodyguard_4", TORTextHelper.GetTextForNative("tor_mercenary_companion_partner_terms_text", "You get part of the share and need to accomplish a few advanced organisational matters. I will however not pay your wage anymore."), null, null, 200, null);
        starter.AddDialogLine("mercenaryCompanion_bodyguard_4", "mercenaryCompanion_bodyguard_4", "mercenaryCompanion_bodyguard_5", TORTextHelper.GetTextForNative("tor_mercenary_companion_demand_payment_text", "First I want to see some hard coin. I am not playing Babysitter or 'Partner' without seeing some money first. You pay me {MERCCOMPANIONPRICE}{GOLD_ICON}"), null, null);
        starter.AddPlayerLine("mercenaryCompanion_bodyguard_5", "mercenaryCompanion_bodyguard_5", "mercenaryCompanion_bodyguard_paymentSuccess", TORTextHelper.GetTextForNative("tor_mercenary_companion_accept_payment_text", "Of course, consider this a forward on your upcoming shares."), MercenaryButtonSucessCondition, null, 200, null);
        starter.AddPlayerLine("mercenaryCompanion_bodyguard_5", "mercenaryCompanion_bodyguard_5", "mercenaryCompanion_bodyguard_paymentFail", TORTextHelper.GetTextForNative("tor_mercenary_companion_no_money_text", "I don't have that in hand right now."), null, null);
        starter.AddDialogLine("mercenaryCompanion_bodyguard_paymentSuccess", "mercenaryCompanion_bodyguard_paymentSuccess", "mercenaryCompanion_bodyguard_end_success", TORTextHelper.GetTextForNative("tor_mercenary_companion_partnership_accepted_text", "Thats a good deal, I am looking foward to this partnership."), null, MakeMercenaryCompanion);
        starter.AddDialogLine("mercenaryCompanion_bodyguard_paymentSuccess", "mercenaryCompanion_bodyguard_paymentFail", "mercenaryCompanion_bodyguard_end_fail", TORTextHelper.GetTextForNative("tor_mercenary_companion_keep_wage_text", "Well then I stay with my current wage."), null, null);
        starter.AddPlayerLine("mercenaryCompanion_bodyguard_end_success", "mercenaryCompanion_bodyguard_end_success", "close_window", TORTextHelper.GetTextForNative("tor_mercenary_companion_wise_decision_text", "What a wise decision!"), null, null);
        starter.AddPlayerLine("mercenaryCompanion_bodyguard_end_fail", "mercenaryCompanion_bodyguard_end_fail", "close_window", TORTextHelper.GetTextForNative("tor_mercenary_companion_fine_text", "Fine."), null, null);


        bool MercenaryButtonSucessCondition()
        {
            var button = (MercenaryCareerButtonBehavior)CareerButtons.Instance.GetCareerButton(Hero.MainHero.GetCareer());
            if (button != null)
            {
                return button.PlayerHasMoney();
            }

            return false;
        }

        void MakeMercenaryCompanion()
        {
            var button = (MercenaryCareerButtonBehavior)CareerButtons.Instance.GetCareerButton(Hero.MainHero.GetCareer());
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
            return button.isDialogStart;
        }

        return false;
    }

    private static void DeactivateDialog()
    {
        var button = CareerButtons.Instance.GetCareerButton(Hero.MainHero.GetCareer());
        button.DeactivateDialog();
    }

}