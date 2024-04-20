using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ink.Parsed;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Pages;
using TaleWorlds.CampaignSystem.ViewModelCollection.Party;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using TaleWorlds.ScreenSystem;
using TaleWorlds.TwoDimension;
using TOR_Core.BattleMechanics.DamageSystem;
using TOR_Core.CampaignMechanics;
using TOR_Core.CharacterDevelopment.CareerSystem;
using TOR_Core.CharacterDevelopment.CareerSystem.CareerButton;
using TOR_Core.Extensions.ExtendedInfoSystem;
using TOR_Core.Utilities;

namespace TOR_Core.Extensions.UI
{
    [ViewModelExtension(typeof(PartyCharacterVM))]
    public class PartyCharacterVMExtension : BaseViewModelExtension
    {
        private bool _shouldButtonBeVisible;
        private bool _isButtonEnabled;
        private bool _isTroop;
        private BasicTooltipViewModel _buttonHint;
        private BasicTooltipViewModel _extendedInfoHint;
        private string _spriteTORButton;
        private TextObject disableReason;
        

        public PartyCharacterVMExtension(ViewModel vm) : base(vm)
        {
            _buttonHint = new BasicTooltipViewModel(GetButtonHintText);

            if (Hero.MainHero.HasAnyCareer())
            {
                var careerButton = CareerHelper.GetCareerButton();
                if (careerButton != null)
                {
                    careerButton.Register();
                }
                else
                {
                    SpecialbuttonEventManagerHandler.Instance.Disable();
                }
            }
            
            
            RefreshValues();
            
        }

        public override void RefreshValues()
        {
            //Update datasource properties here. Gets called every time the base ViewModel would get refreshed.
            //Careful to always update the property, not the field behind it directly, because then the engine won't get notified and events won't be raised.
            
            var troopCharacter = ( (PartyCharacterVM)_vm ).Troop.Character;

            

            var isPrisoner = ( (PartyCharacterVM)_vm ).IsPrisonerOfPlayer;
            if(troopCharacter==null) return;
            
            IsTroop = !troopCharacter.IsHero;

            if (IsTroop)
            {
                var extendedInfoList = GetTroopExtendedInfoText(troopCharacter);
                if (!extendedInfoList.IsEmpty())
                {
                    ExtendedInfoHint = new BasicTooltipViewModel(()=> extendedInfoList);
                }
            }

            ShouldButtonBeVisible = SpecialbuttonEventManagerHandler.Instance.ShouldButtonBeVisible(troopCharacter, isPrisoner);
            
            IsButtonEnabled =  SpecialbuttonEventManagerHandler.Instance.ShouldButtonBeActive(troopCharacter, out var displaytext, isPrisoner);
            disableReason = displaytext;
            
            SpriteTORButton = CareerHelper.GetButtonSprite();
        }

        private string GetButtonHintText()
        {
            //This method will get called every time the hint shows up, so you can dynamically construct the hint text based on context, not just static text
            return disableReason.ToString();
        }

        public void ExecuteButtonClick()
        {
            var troop = ( (PartyCharacterVM)_vm ).Troop.Character;
            var isPrisoner = ( (PartyCharacterVM)_vm ).IsPrisonerOfPlayer;
            SpecialbuttonEventManagerHandler.Instance.OnButtonClicked(troop,isPrisoner );
            
            ((PartyCharacterVM)_vm).RefreshValues();

        }
        
        [DataSourceProperty]
        public string SpriteTORButton
        {
            get
            {
                return _spriteTORButton;
            }
            set
            { 
                _spriteTORButton=value;
                _vm.OnPropertyChangedWithValue(value, "SpriteTORButton");
            }
        }
        
        private List<TooltipProperty> GetTroopExtendedInfoText(CharacterObject characterObject)
        {
            var text = "Unit Attributes";
            var equipment = characterObject.GetCharacterEquipment(EquipmentIndex.Weapon0, EquipmentIndex.Weapon3);

            var model = Campaign.Current.Models.CharacterStatsModel;

            var hitpoints = model.MaxHitpoints(characterObject).ResultNumber;
            
            var info = ExtendedInfoManager.GetCharacterInfoFor(characterObject.StringId);
            List<TooltipProperty> list = new List<TooltipProperty>();
            
            list.Add(new TooltipProperty("Health", hitpoints.ToString,0, false,TooltipProperty.TooltipPropertyFlags.RundownResult));
            
            var baseDamage = 0;
            var listDamages = new List<(int DamageValue, string damageType) >();
            
            foreach (var item in equipment)
            {
                if (characterObject.IsRanged)
                {
                    if (item.PrimaryWeapon.IsRangedWeapon && !item.PrimaryWeapon.IsAmmo)
                    {
                        listDamages.Add((item.PrimaryWeapon.MissileDamage,"Ranged"));
                    }  
                }
                
                if (item.PrimaryWeapon.IsMeleeWeapon)
                {
                    var typetext = "Melee";
                    if (item.PrimaryWeapon.RelevantSkill == DefaultSkills.Polearm)
                    {
                        typetext = "Lance/Spear";
                    }
                    
                    var damage = Math.Max(item.PrimaryWeapon.SwingDamage, item.PrimaryWeapon.ThrustDamage);
                    listDamages.Add((damage,typetext));
                }
            }
            
            
            if (info == null) return list;

            var damagesList = new List<TooltipProperty>();
            
            foreach (var damageCategory in listDamages)
            {
                var resultDamge = 0;
                var propotionList = new List<TooltipProperty>();
                foreach (var tuple in info.DamageProportions)
                {
                    var value =(int)(damageCategory.DamageValue * tuple.Percent);
                    resultDamge +=value;
                    //if (tuple.DamageType == DamageType.Physical && tuple.Percent != 1f) break;
                    propotionList.Add(new TooltipProperty(tuple.DamageType.ToString, value.ToString,0, false,TooltipProperty.TooltipPropertyFlags.None));
                }
                
                if (!propotionList.IsEmpty())
                {
                    damagesList.Add(new TooltipProperty(damageCategory.damageType+" Damage ", resultDamge.ToString,0, false,TooltipProperty.TooltipPropertyFlags.RundownResult));
                    
                    if (propotionList.Count > 1)
                    {
                        propotionList.Insert(0,new TooltipProperty("-","",0, false,TooltipProperty.TooltipPropertyFlags.RundownSeperator));
                        damagesList.AddRange(propotionList);
                    }
                    
                }
            }
            list.Add(new TooltipProperty("-", "-",0, false,TooltipProperty.TooltipPropertyFlags.DefaultSeperator));
            list.AddRange(damagesList);

            var amplifierlist = new List<TooltipProperty>();
            foreach (var amplifier in info.DamageAmplifiers)
            {
                var percentText = amplifier.DamageAmplifier.ToString("P0");
                amplifierlist.Add(new TooltipProperty(amplifier.AmplifiedDamageType.ToString(), percentText,0, false,TooltipProperty.TooltipPropertyFlags.None));
            }
            
            if (!amplifierlist.IsEmpty())
            {
                amplifierlist.Insert(0,new TooltipProperty("-", "",0, false,TooltipProperty.TooltipPropertyFlags.DefaultSeperator));
                amplifierlist.Insert(1,new TooltipProperty("Damage Amplification", "",0, false,TooltipProperty.TooltipPropertyFlags.RundownResult));
                list.AddRange(amplifierlist);
            }
            
            var resistanceList = new List<TooltipProperty>();
            foreach (var resistanceTuple in info.Resistances)
            {
                var percentText = resistanceTuple.ReductionPercent.ToString("P0");
                
                if (resistanceTuple.ResistedDamageType != DamageType.All)
                {
                    resistanceList.Add(new TooltipProperty(resistanceTuple.ResistedDamageType.ToString(), percentText,0, false,TooltipProperty.TooltipPropertyFlags.None));
                    continue;
                }
                resistanceList.Add(new TooltipProperty("Wardsave", percentText,0, false,TooltipProperty.TooltipPropertyFlags.None));
            }
            
            if (!resistanceList.IsEmpty())
            {
                resistanceList.Insert(0,new TooltipProperty("-", "",0, false,TooltipProperty.TooltipPropertyFlags.DefaultSeperator));
                resistanceList.Insert(1,new TooltipProperty("Damage Resistances", "",0, false,TooltipProperty.TooltipPropertyFlags.RundownResult));
                list.AddRange(resistanceList);
            }


            if (info.CharacterAttributes.Any())
            {
                var texts = new List<TooltipProperty>();
                foreach (var attribute in info.CharacterAttributes)
                {
                    
                    if (attribute == "Undead")
                    {
                        texts.Add(new TooltipProperty("", "Undead- Is not affected by morale and instead of fleeing this unit crumble. If defeated, unit cannot be wounded.",0, false,TooltipProperty.TooltipPropertyFlags.MultiLine));
                    }
                    
                    if (attribute == "ArtilleryCrew")
                    {
                        texts.Add(new TooltipProperty("ArtilleryCrew", "Required for maintaining Cannons",0, false,TooltipProperty.TooltipPropertyFlags.MultiLine));
                    }
                }


                if (texts.Any())
                {
                    texts.Insert(0,new TooltipProperty("-", "",0, false,TooltipProperty.TooltipPropertyFlags.DefaultSeperator));
                    list.AddRange(texts);
                }
                
            }
            
            if (!list.IsEmpty())
            {
                list.Insert(0,new TooltipProperty(text, "", 0, false, TooltipProperty.TooltipPropertyFlags.Title));
            }
            return list;
            
            
        }
        
        [DataSourceProperty]
        public bool ShouldButtonBeVisible
        {
            get
            {
                return _shouldButtonBeVisible;
            }
            set
            {
                if (value != _shouldButtonBeVisible)
                {
                    _shouldButtonBeVisible = value;
                    _vm.OnPropertyChangedWithValue(value, "ShouldButtonBeVisible");
                }
            }
        }
        
        [DataSourceProperty]
        public BasicTooltipViewModel ExtendedInfoHint
        {
            get => this._extendedInfoHint;
            set
            {
                if (value == this._extendedInfoHint)
                    return;
                this._extendedInfoHint = value;
                this._vm.OnPropertyChangedWithValue(nameof (ExtendedInfoHint));
            }
        }
        
        [DataSourceProperty]
        public bool IsTroop
        {
            get
            {
                return _isTroop;
            }
            set
            {
                if (value != _isTroop)
                {
                    _isTroop = value;
                    _vm.OnPropertyChangedWithValue(value, "IsTroop");
                }
            }
        }

        [DataSourceProperty]
        public bool IsButtonEnabled
        {
            get
            {
                return _isButtonEnabled;
            }
            set
            {
                if (value != _isButtonEnabled)
                {
                    _isButtonEnabled = value;
                    _vm.OnPropertyChangedWithValue(value, "IsButtonEnabled");
                }
            }
        }

        [DataSourceProperty]
        public BasicTooltipViewModel ButtonHint
        {
            get
            {
                return _buttonHint;
            }
            set
            {
                if (value != _buttonHint)
                {
                    _buttonHint = value;
                    _vm.OnPropertyChangedWithValue(value, "ButtonHint");
                }
            }
        }
    }

    
}
