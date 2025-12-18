using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.ViewModelCollection;
using TaleWorlds.Core;
using TaleWorlds.Core.ImageIdentifiers;
using TaleWorlds.LinQuick;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;
using TOR_Core.CampaignMechanics.Crafting;
using TOR_Core.CharacterDevelopment;
using TOR_Core.Extensions;
using TOR_Core.Extensions.ExtendedInfoSystem;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignMechanics;

public class SkillTrainerBehavior : CampaignBehaviorBase
{
    private readonly string _skillTrainerAttribute = "SkillTrainer";

    private Dictionary<string, string> _icons;



    private readonly Dictionary<string, (string TrainerDialogId, string SkillId, string HubId, string HubReintroKey, List<string> Restrictions)> _skilltrainers = new()
    {
        {"tor_priest_trainer_empire_ulric_0",("EngineerEmpire","Faith", "priest_hubcult_of_ulric","priest_hub_reintrocult_of_ulric",[])},
        {"tor_priest_trainer_empire_sigmar_0",("SigmarPriest","Faith", "priest_hubcult_of_sigmar","priest_hub_reintrocult_of_sigmar",[])},
        {"tor_priest_trainer_empire_shallya_0",("ShallyaTrainRE","Medicine", "priest_hubcult_of_shallya","priest_hub_reintrocult_of_shallya",[])},
        {"tor_priest_trainer_empire_shallya_1",("ShallyaTrainCO","Medicine", "priest_hubcult_of_shallya","priest_hub_reintrocult_of_shallya",[])},
        {"tor_nulnengineernpc_empire",("EngineerEmpire","Engineering", "hub","hubaftermission",[])},
        {"tor_spelltrainer_empire_0",("EmpireMagister","Spellcraft", "choices","start",["SpellCaster"])},
        {"tor_dawi_runelord_trainer_0",("DwarfRunelord","Spellcraft", "tor_dw_guildmaster_runesmith_hub","tor_dw_guildmaster_runesmith_start_reintro",["Runesmith"])},
        {"tor_spelltrainer_vc_0",("Necromancer","Spellcraft", "priest_hubcult_of_sigmar","priest_hub_reintrocult_of_sigmar",[])},
    };

    private Dictionary<string, HeroTrainingData> _heroesInTraining = new();

    private Hero _currentTrainer = null;
    private SkillObject _currentSkill;

    public override void RegisterEvents()
    {

        CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
        CampaignEvents.OnNewGameCreatedPartialFollowUpEvent.AddNonSerializedListener(this, Setup);
        CampaignEvents.BeforeMissionOpenedEvent.AddNonSerializedListener(this, OnBeforeMissionStart);


        CampaignEvents.GameMenuOpened.AddNonSerializedListener(this, OnGameMenuOpened);

        CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, TeachCompanions);

        CampaignEvents.OnHeroTeleportationRequestedEvent.AddNonSerializedListener(this, TeleportRequest);

        CampaignEvents.OnSettlementOwnerChangedEvent.AddNonSerializedListener(this, SettlementOwnerChanged);
    }

    private void SettlementOwnerChanged(Settlement settlement, bool openToClaim, Hero newOwner, Hero oldOwner, Hero capturerHero, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail detail)
    {
        if (_heroesInTraining.Any())
        {
            if (detail != ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail.ByRebellion &&
                detail != ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail.BySiege) return;

            foreach (var heroEntry in _heroesInTraining)
            {
                if (heroEntry.Value.SettlementId == settlement.StringId)
                {
                    var hero = Hero.MainHero.Clan.Heroes.FirstOrDefault(x => x.StringId == heroEntry.Key);
                    LeaveTraining(hero);
                }
            }

        }
    }

    private void LeaveTraining(Hero hero)
    {
        if (hero.CurrentSettlement == null || !hero.CurrentSettlement.IsUnderSiege)
        {
            if (hero.PartyBelongedTo == null)
            {
                TeleportHeroAction.ApplyDelayedTeleportToParty(hero, MobileParty.MainParty);
            }

        }

        _heroesInTraining.Remove(hero.StringId);

    }

    private void Setup(CampaignGameStarter campaignGameStarter, int i)
    {
        if (i == CampaignEvents.OnNewGameCreatedPartialFollowUpEventMaxIndex - 1)
        {
            foreach (var hero in Campaign.Current.AliveHeroes)
            {
                OnNewHeroCreated(hero, false);
            }
        }



    }
    private void TeleportRequest(Hero arg1, Settlement arg2, MobileParty arg3, TeleportHeroAction.TeleportationDetail teleportDetails)
    {

    }


    private void TeachCompanions()
    {
        var removableHeroes = new List<Hero>();
        if (_heroesInTraining.Any())
        {
            foreach (var entry in _heroesInTraining)
            {
                var hero = Campaign.Current.AliveHeroes.FirstOrDefault(x => x.StringId == entry.Key);

                if (hero == null)
                {
                    continue;
                }

                if (hero.CurrentSettlement != null && hero.CurrentSettlement.IsUnderSiege)
                {
                    removableHeroes.Add(hero);
                }

                if (hero.PartyBelongedTo != null)
                {
                    removableHeroes.Add(hero);
                    continue;
                }
                var skill = Skills.All.FirstOrDefault(x => x.StringId == entry.Value.skillId);

                if (skill == null)
                {
                    removableHeroes.Add(hero);
                    continue;
                }

                entry.Value.duration++;


                var model = Campaign.Current.Models.GetCompanionTrainingModel();
                var value = model.DailySkillGainForTraining(hero, skill);
                if (!model.ReachedSkillCap(hero, skill, value))
                {
                    hero.AddSkillXp(skill, value);
                }
                else
                {
                    hero.AddSkillXp(skill, value);
                    removableHeroes.Add(hero);
                }

            }
        }

        foreach (var hero in removableHeroes)
        {
            LeaveTraining(hero);
        }
    }

    private void OnBeforeMissionStart()
    {
    }

    private void OnGameMenuOpened(MenuCallbackArgs obj)
    {

    }

    private void OnNewHeroCreated(Hero hero, bool b)
    {
        if (hero.Template == null) return;

        if (_skilltrainers.ContainsKey(hero.Template.StringId))
        {
            var values = _skilltrainers[hero.Template.StringId];
            var skillTrainerAtribute = _skillTrainerAttribute;
            var skill = values.SkillId;

            var info = hero.GetExtendedInfo();
            if (info != null)
            {
                hero.AddAttribute(skillTrainerAtribute);
                hero.AddAttribute(skill);
            }
        }
    }

    private void OnSessionLaunched(CampaignGameStarter campaignStarter)
    {
        var skills = new List<SkillObject>();

        skills = Game.Current.DefaultSkills.GetDefaultSkills();
        skills.AddRange(TORSkills.Instance.GetTorSkills());

        foreach (var skill in skills)
        {
            var icon = "gui_skills_icon_" + skill.StringId.ToLower() + "_small";
            MBTextManager.SetTextVariable("SKILL_ICON_" + skill.StringId.ToLower(), string.Format("<img src=\"{0}\" extend=\"8\">", icon));
        }
        foreach (var entry in _skilltrainers)
        {
            var hub = entry.Value.HubId;
            var reintro_hub = entry.Value.HubReintroKey;
            var trainerDialogId = entry.Value.TrainerDialogId;
            var trainerId = entry.Key;
            var skillId = entry.Value.SkillId;
            var restrictions = entry.Value.Restrictions;

            campaignStarter.AddPlayerLine("tor_teach_skills_dialog_p" + trainerDialogId + skillId, hub, "tor_skill_teacher_train_1" + trainerDialogId + skillId,
                TORTextHelper.GetText("tor_teach_skills_dialog_p", trainerDialogId, "I would like to arrange training for one of my companions.", true), () => TrainerCondition(trainerId, skillId), null, 210);


            //train companion
            campaignStarter.AddDialogLine("tor_skill_teacher_train_1" + trainerDialogId + skillId, "tor_skill_teacher_train_1" + trainerDialogId + skillId, "tor_skill_teacher_train_2" + trainerDialogId + skillId,
                TORTextHelper.GetText("tor_skill_teacher_train_1", trainerDialogId, "Of course. I can train your companions in the necessary skills.", true), null, null, 200);

            campaignStarter.AddDialogLine("tor_skill_teacher_train_2" + trainerDialogId + skillId, "tor_skill_teacher_train_2" + trainerDialogId + skillId, "skilltrainer_train_hub" + trainerDialogId + skillId,
                TORTextHelper.GetText("tor_skill_teacher_train_2", trainerDialogId, "The training will take some time, but your companion will learn much. What do you wish to do?", true), null, null, 200);

            campaignStarter.AddPlayerLine("tor_skill_train_hub_select_companion_p" + trainerDialogId + skillId, "skilltrainer_train_hub" + trainerDialogId + skillId, "priest_train_hub_select_companion" + trainerDialogId + skillId,
                TORTextHelper.GetText("tor_skill_train_hub_select_companion_p", trainerDialogId, "I would like to send a companion for training.", true), null, null, 200);


            campaignStarter.AddDialogLine("tor_skill_train_hub_select_companion" + trainerDialogId + skillId, "priest_train_hub_select_companion" + trainerDialogId + skillId, "tor_skill_teacher_train_2" + trainerDialogId,
                TORTextHelper.GetText("tor_skill_train_hub_select_companion", trainerDialogId, "Very well. Choose which companion you wish to send for training.", true), () => IsAnyCompanionEligableForTraining(skillId, restrictions), () => SelectCompanionForTraining(skillId), 200);

            campaignStarter.AddDialogLine("tor_skill_train_hub_select_companion_decline" + trainerDialogId + skillId, "priest_train_hub_select_companion" + trainerDialogId + skillId, "tor_skill_teacher_train_2" + trainerDialogId + skillId,
                TORTextHelper.GetText("tor_skill_train_hub_select_companion_decline", trainerDialogId, "I'm afraid none of your companions are eligible for training at this time.", true), () => !IsAnyCompanionEligableForTraining(skillId, restrictions), null, 200);



            campaignStarter.AddPlayerLine("skilltrain_train_hub_quit_p" + trainerDialogId + skillId, "skilltrainer_train_hub" + trainerDialogId + skillId, reintro_hub,
                TORTextHelper.GetText("tor_priest_train_hub_quit_p", trainerDialogId, "That is all for now.", true), null, null, 200);
        }

        bool TrainerCondition(string trainerId, string skillId)
        {
            var partner = CharacterObject.OneToOneConversationCharacter;

            if (!partner.HeroObject.HasAttribute("SkillTrainer"))
            {
                return false;
            }


            if (partner.HeroObject.Template.StringId != trainerId)
            {
                return false;
            }

            if (!partner.HeroObject.HasAttribute(skillId))

                return false;

            return true;
        }

        bool IsAnyCompanionEligableForTraining(string skillId, List<string> restrictions)
        {
            var skill = Skills.All.FirstOrDefault(x => x.StringId == skillId);

            if (skill == null)
            {
                return false;
            }
            var model = Campaign.Current.Models.GetCompanionTrainingModel();
            var heroes = Hero.MainHero.PartyBelongedTo.GetMemberHeroes();

            foreach (var hero in heroes.Where(hero => hero != Hero.MainHero))
            {
                var allowed = true;
                if (restrictions.Any())
                {
                    allowed = false;
                    foreach (var restriction in restrictions)
                    {
                        allowed = hero.HasKnownLore(restriction) || hero.HasAttribute(restriction);
                    }
                }

                if (!allowed)
                {
                    return false;
                }
                if (model.HeroIsEligibleForTraining(hero, skill))
                {
                    return true;
                }
            }

            return false;
        }
    }

    private void SelectCompanionForTraining(string skillId)
    {

        _currentTrainer = Hero.OneToOneConversationHero;

        var skill = Skills.All.FirstOrDefault(x => x.StringId == skillId);

        if (skill == null)
        {
            return;
        }

        _currentSkill = skill;
        GameTexts.SetVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"8\">");
        GameTexts.SetVariable("CUSTOMRESOURCE", Hero.MainHero.GetCultureSpecificCustomResource().GetCustomResourceIconAsText(true));
        var title = TORTextHelper.GetTextObject("tor_skill_training_prompt", "title", "Companion Training: {SKILL_NAME}", true);
        title.SetTextVariable("SKILL_NAME", skill.Name);
        var description = TORTextHelper.GetTextObject("tor_skill_training_prompt", "description", "Select a companion to train {SKILLNAME} with {INSTRUCTOR_NAME}.", true);
        description.SetTextVariable("INSTRUCTOR_NAME", _currentTrainer.Name);
        description.SetTextVariable("SKILLNAME", skill.Name);


        var elements = new List<InquiryElement>();


        var heroes = Hero.MainHero.PartyBelongedTo.GetMemberHeroes();




        var model = Campaign.Current.Models.GetCompanionTrainingModel();

        foreach (var hero in heroes)
        {
            if (hero == Hero.MainHero)
            {
                continue;
            }
            var isEnabled = false;
            if (!model.HeroIsEligibleForTraining(hero, skill))
            {
                continue;
            }

            isEnabled = true;

            var reason = new StringBuilder();

            var icon = Hero.MainHero.GetCultureSpecificCustomResource().GetCustomResourceIconAsText(true);

            var costs = model.GetCostForTraining(hero, skill);

            if (Hero.MainHero.Gold < costs.goldcost)
            {
                isEnabled = false;

                var noGoldText = TORTextHelper.GetTextObject("tor_skill_training_hover", "NoGold", "Not enough gold.", true);

                reason.Append(noGoldText);
            }

            if (Hero.MainHero.GetCultureSpecificCustomResourceValue() < costs.customResourceCost)
            {
                isEnabled = false;
                var noCustomResource = TORTextHelper.GetTextObject("tor_skill_training_hover", "NoCustomResource", "Not enough custom resource.", true);
                reason.Append(noCustomResource);
                //reason = new TextObject("not enough"+Hero.MainHero.GetCultureSpecificCustomResource().GetCustomResourceIconAsText(true) +" requires "+costs.customResourceCost);
            }

            GameTexts.SetVariable("SKILLTRAINING_REASON", reason.ToString());
            var costText = TORTextHelper.GetTextObject("tor_skill_training_hover", "Costs", "Cost: {GOLD_COST} {GOLD_ICON}, {CR_COST} {CUSTOMRESOURCE}", true);
            costText.SetTextVariable("GOLD_COST", costs.goldcost);
            costText.SetTextVariable("CR_COST", costs.customResourceCost);
            GameTexts.SetVariable("SKILL_TRAINING_COSTS", costText.ToString());

            var currentSkillValueText = TORTextHelper.GetTextObject("tor_skill_training_prompt", "skill_value", "Current Skill: {SKILL_VALUE}", true);
            currentSkillValueText.SetTextVariable("SKILL_VALUE", hero.GetSkillValue(skill));
            var final = TORTextHelper.GetTextObject("tor_skill_training_hover", "Full", "{SKILL_TRAINING_COSTS}\n{SKILLTRAINING_REASON}", true);

            var heroItem = new InquiryElement(new Tuple<Hero, (int goldCost, int crCost)>(hero, costs), hero.Name.ToString() + "\n" + currentSkillValueText,
                new CharacterImageIdentifier(CampaignUIHelper.GetCharacterCode(hero.CharacterObject)), isEnabled, final.ToString());

            elements.Add(heroItem);

        }

        var inquirydata = new MultiSelectionInquiryData(title.ToString(), description.ToString(), elements, true, 1, 1, TORTextHelper.GetText("tor_inquiry_accept_text", "Accept"), TORTextHelper.GetText("tor_inquiry_cancel_text", "Cancel"),
            MoveHeroToTrainer, null);
        MBInformationManager.ShowMultiSelectionInquiry(inquirydata, true);

        void MoveHeroToTrainer(List<InquiryElement> inquiryElements)
        {
            var obj = (Tuple<Hero, (int goldCost, int crCost)>)inquiryElements.FirstOrDefault()?.Identifier;

            Hero hero = obj.Item1;
            var costs = obj.Item2;

            if (hero == null)
                return;

            var partner = Hero.OneToOneConversationHero;

            TeleportHeroAction.ApplyImmediateTeleportToSettlement(hero, partner.CurrentSettlement);

            Hero.MainHero.ChangeHeroGold(-costs.goldCost);

            Hero.MainHero.AddCultureSpecificCustomResource(-costs.crCost);



            var data = new HeroTrainingData()
            {
                skillId = _currentSkill.StringId,
                timeStampTrainingBegin = Campaign.CurrentTime,
                duration = 0f,
                SettlementId = partner.CurrentSettlement.StringId

            };
            _heroesInTraining.Add(hero.StringId, data);
            _currentSkill = null;
        }


    }

    public override void SyncData(IDataStore dataStore)
    {
        dataStore.SyncData("_heroesInTraining", ref _heroesInTraining);
    }


}

public class HeroTrainingData
{
    [SaveableField(0)] public string skillId;
    [SaveableField(1)] public float timeStampTrainingBegin;
    [SaveableField(2)] public float duration;
    [SaveableField(3)] public string SettlementId;
}