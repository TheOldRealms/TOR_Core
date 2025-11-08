using Ink.Parsed;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.ViewModelCollection;
using TaleWorlds.Core;
using TaleWorlds.Core.ImageIdentifiers;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.SaveSystem;
using TOR_Core.CharacterDevelopment;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.Items.InventoryUseScripts;

public class EnchantmentBlueprintScript : BaseInventoryUseScript
{
    [SaveableField(1)]
    private string blueprintId;
    
    [SaveableField(2)]
    private string _requiredSkill;

    [SaveableField(3)]
    private int _requiredSkillValue;
    
    [SaveableField(4)]
    private List<string> _requiredAttributesOrLores;
    
    
    public EnchantmentBlueprintScript(string[] arguments) : base(arguments)
    {
        if(arguments.Count() >= 3)
        {
            
            blueprintId = arguments[0];
            
            if (ItemTrait.All.All(itemTrait => itemTrait.ItemTraitStringId != blueprintId))
            {
                throw new TORUseScriptArgumentException($"EnchantmentBluePrintScript failed to find itemtraitID with ID: {blueprintId}.");
            }
            
            _requiredSkill = arguments[1];
            if (Campaign.Current.ObjectManager.GetObject<SkillObject>(_requiredSkill) == null)
            {
                throw new TORUseScriptArgumentException($"EnchantmentBluePrintScript failed to find skill with ID: {_requiredSkill}.");
            }
            
            if (!int.TryParse(arguments[2], out _requiredSkillValue))
            {
                throw new TORUseScriptArgumentException($"EnchantmentBluePrintScript failed to parse XP amount from argument: {arguments[1]}.");
            }

            if (_requiredSkillValue > 300 || _requiredSkillValue < 0)
            {
                throw new TORUseScriptArgumentException($"EnchantmentBluePrintScript failed to parse required Skill Value. Number must be between 0 and 300.");
            }
            
            _requiredAttributesOrLores = new List<string>();
            
            if (_arguments.Length > 3)
            {
      
                for (int i = 3; i < _arguments.Length; i++)
                {
                    _requiredAttributesOrLores.Add(_arguments[i]);
                }
            }
        }
        else
        {
            throw new TORUseScriptArgumentException("EnchantmentBluePrintScript requires at least 2 arguments: SkillId, XP amount, and learning time in hours.");
        }
        
    }
    public override void OnUse(MobileParty userParty, ItemObject item)
    {
        var heroes = Hero.MainHero.PartyBelongedTo.GetMemberHeroes();

        List<SkillObject> skills = Game.Current.DefaultSkills.GetDefaultSkills();

        skills.AddRange (TORSkills.Instance.GetTorSkills());

        var selectableHeroes = new List<InquiryElement>();

        foreach (var hero in heroes)
        {
            var isValid = false;
            if (!_requiredAttributesOrLores.IsEmpty())
            {
                
                if (_requiredAttributesOrLores.Any(attribute => hero.HasAttribute(attribute)))
                {
                    isValid = true;
                }

                if (_requiredAttributesOrLores.Any(attribute => hero.HasKnownLore(attribute))) //use attributes or lores to check.
                {
                    isValid = true;
                }
            }
            else
            {
                isValid = true;
            }

            if (!isValid)
            {
                continue;
            }

            foreach (var skill in skills.Where(skill => skill.StringId == _requiredSkill).Where(skill => hero.GetSkillValue(skill) < _requiredSkillValue))
            {
                isValid = false;
            }

            if (isValid)
            {
                var blueprints = hero.GetExtendedInfo().KnownEnchantmentBlueprints;

                foreach (var blueprint in blueprints)
                {
                    if (blueprint == blueprintId)
                    {
                        isValid = false;
                    }
                }
            }
            
            
            if (isValid)
            {
                selectableHeroes.Add(new InquiryElement(hero, hero.Name.ToString(), new CharacterImageIdentifier(CampaignUIHelper.GetCharacterCode(hero.CharacterObject))));
            }
        }
        
        if (selectableHeroes.IsEmpty())
        {
            TORCommon.Say("The manuscript is of no use for you.");
            return;
        }
        
        var inquirydata = new MultiSelectionInquiryData("Choose hero to learn new enchantment",
            "The scribing entails a powerful new enchantment effect for one of your party members to learn. Choose who will specialize in", selectableHeroes,true, 1, 1, "Accept", "Cancel", OnSelectedOption, null, "", false);
        MBInformationManager.ShowMultiSelectionInquiry(inquirydata);
        
        
        void OnSelectedOption(List<InquiryElement> inquiryElements)
        {
            var hero =(Hero) inquiryElements[0].Identifier;
            hero.AddEnchantmentBlueprint(blueprintId,true);
            
        }
        
    }
}