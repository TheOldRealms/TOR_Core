using Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.Inventory;
using TaleWorlds.CampaignSystem.ViewModelCollection;
using TaleWorlds.CampaignSystem.ViewModelCollection.Inventory;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TOR_Core.BattleMechanics.DamageSystem;
using TOR_Core.CampaignMechanics.SkillBooks;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.Items
{
    public class TorItemMenuVM : ItemMenuVM
    {
		private ItemObject _lastSetItem;
		private bool _isMagicItem = false;
		private MBBindingList<TorItemTraitVM> _itemTraitList;
		private TextObject speedText = new TextObject("{=74dc1908cb0b990e80fb977b5a0ef10d}Speed: ", null);
		private TextObject damageText = new TextObject("{=c9c5dfed2ca6bcb7a73d905004c97b23}Damage: ", null);
		private TextObject accuracyText = new TextObject("{=5dec16fa0be433ade3c4cb0074ef366d}Accuracy: ", null);
		private TextObject missileSpeedText = GameTexts.FindText("str_missile_speed", null);
		private TextObject ammoLimitText = new TextObject("{=6adabc1f82216992571c3e22abc164d7}Ammo Limit: ", null);

		// Description Text
		private string _itemDescription = "";
		private bool _hasDescription = false;

		// Read Button
		private HintViewModel _readHint;
		private bool _isSkillBook = false;

		public TorItemMenuVM(Action<ItemVM, int> resetComparedItems, InventoryLogic inventoryLogic, Func<WeaponComponentData, ItemObject.ItemUsageSetFlags> getItemUsageSetFlags, Func<EquipmentIndex, SPItemVM> getEquipmentAtIndex) : base(resetComparedItems, inventoryLogic, getItemUsageSetFlags, getEquipmentAtIndex)
        {
			_itemTraitList = new MBBindingList<TorItemTraitVM>();
			_readHint = new HintViewModel(new TaleWorlds.Localization.TextObject("Read scroll"));
			inventoryLogic.AfterTransfer += CheckItem;
        }

		private void CheckItem(InventoryLogic inventoryLogic, List<TransferCommandResult> results)
		{
			foreach (var result in results)
			{
				if(result.ResultSide != InventoryLogic.InventorySide.Equipment)
					continue;
				
				var movedItem = result.EffectedItemRosterElement.EquipmentElement.Item;
				
				if(!ItemUtilities.IsAmmunitionItem(movedItem))
					continue;
				
				var targetEquipment = result.TransferCharacter.GetCharacterEquipment(EquipmentIndex.Weapon0,
					EquipmentIndex.NumAllWeaponSlots);
				
				foreach (var equipmentItem in targetEquipment.Where(x => x.ToString() !=result.EffectedItemRosterElement.EquipmentElement.Item.ToString()))
				{
					if(!ItemUtilities.IsAmmunitionItem(equipmentItem))
						continue; //we are only interested for now in ranged and ammo items

					if(ItemUtilities.IsSpecialAmmunitionItem(movedItem))
						if(ItemUtilities.IsSpecialAmmunitionItem(equipmentItem))
							continue;

					if (!ItemUtilities.IsSpecialAmmunitionItem(movedItem))
						if(!ItemUtilities.IsSpecialAmmunitionItem(equipmentItem))
							continue;

					//no you don't... items were not compatible return to sender
					var command = TransferCommand.Transfer(1,
						InventoryLogic.InventorySide.Equipment,
						InventoryLogic.InventorySide.PlayerInventory,
						result.EffectedItemRosterElement,
						result.EffectedEquipmentIndex,
						EquipmentIndex.None,
						result.TransferCharacter, result.IsCivilianEquipment);

					inventoryLogic.AddTransferCommand(command);
					
					break;
				}
			}
		}

        public void SetItemExtra(SPItemVM item, ItemVM comparedItem = null, BasicCharacterObject character = null, int alternativeUsageIndex = 0)
        {
			AddTooltipForGunpowderWeapons(item, comparedItem);
			ItemTraitList.Clear();
			IsMagicItem = false;
			_lastSetItem = item.ItemRosterElement.EquipmentElement.Item;
			ItemDescription = _lastSetItem.GetTorSpecificData().Description;
			HasDescription = !ItemDescription.IsEmpty();
			UpdateReadButton(_lastSetItem);

			if (_lastSetItem != null && _lastSetItem.GetTorSpecificData() != null)
            {
				var info = _lastSetItem.GetTorSpecificData();
				if(info != null && (info.DamageProportions.Any(x=>x.DamageType != DamageType.Physical) || info.ItemTraits.Count > 0))
				{
					IsMagicItem = true;
					if(info.ItemTraits.Count > 0)
                    {
						foreach(var itemTrait in info.ItemTraits)
                        {
							ItemTraitList.Add(new TorItemTraitVM(itemTrait));
                        }
                    }
                }
                if (_lastSetItem.HasWeaponComponent)
                {
					var damageprops = base.TargetItemProperties.Where(x => x.DefinitionLabel.Contains("Damage"));
					foreach(var prop in damageprops)
                    {
						int damagenum = 0;
						bool success = int.TryParse(prop.ValueLabel.Split(' ')[0], out damagenum);
                        if (success)
                        {
							prop.ValueLabel = "";
							if(info != null && info.DamageProportions.Count > 1)
                            {
								prop.ValueLabel += damagenum.ToString() + " (";
								for (int i = 0; i < info.DamageProportions.Count; i++)
								{
									var tuple = info.DamageProportions[i];
									prop.ValueLabel += ((int)(tuple.Percent * damagenum)).ToString() + " " + tuple.DamageType.ToString() + (i == info.DamageProportions.Count - 1 ? "" : "+");
								}
								prop.ValueLabel += ")";
							}
							else if (info != null && info.DamageProportions.Count == 1)
                            {
								prop.ValueLabel = damagenum.ToString() + " " + info.DamageProportions[0].DamageType.ToString();
							}
							if(prop.ValueLabel == "")
                            {
								prop.ValueLabel = damagenum.ToString() + " Physical";
                            }
                        }
                    }
                }
            }
        }

        private void AddTooltipForGunpowderWeapons(SPItemVM item, ItemVM comparedItem)
        {
			var equipmentElement = item.ItemRosterElement.EquipmentElement;
			if (!equipmentElement.Item.HasWeaponComponent) return;
			var comparedEquipmentElement = comparedItem == null ? EquipmentElement.Invalid : comparedItem.ItemRosterElement.EquipmentElement;
			var weaponData = equipmentElement.Item.GetWeaponWithUsageIndex(AlternativeUsageIndex);
			var comparedWeaponData = comparedEquipmentElement.Item == null ? null : comparedEquipmentElement.Item.GetWeaponWithUsageIndex(AlternativeUsageIndex);
			var weaponClass = weaponData.WeaponClass;
			if (weaponClass != WeaponClass.Musket && weaponClass != WeaponClass.Pistol) return;

			this.AddIntProperty(speedText, equipmentElement.GetModifiedSwingSpeedForUsage(AlternativeUsageIndex), comparedEquipmentElement.IsEmpty ? null : new int?(comparedEquipmentElement.GetModifiedSwingSpeedForUsage(AlternativeUsageIndex)));
			this.AddThrustDamageProperty(damageText, equipmentElement, AlternativeUsageIndex, comparedEquipmentElement, AlternativeUsageIndex);
			this.AddIntProperty(accuracyText, weaponData.Accuracy, (comparedWeaponData != null) ? new int?(comparedWeaponData.Accuracy) : null);
			this.AddIntProperty(missileSpeedText, equipmentElement.GetModifiedMissileSpeedForUsage(AlternativeUsageIndex), comparedEquipmentElement.IsEmpty ? null : new int?(comparedEquipmentElement.GetModifiedMissileSpeedForUsage(AlternativeUsageIndex)));
			short? num = (comparedWeaponData != null) ? new short?(comparedWeaponData.MaxDataValue) : null;
			this.AddIntProperty(ammoLimitText, weaponData.MaxDataValue, (num != null) ? new int?((int)num.GetValueOrDefault()) : null);
		}

		private void UpdateReadButton(ItemObject selectedItem)
        {
			IsSkillBook = TORSkillBookCampaignBehavior.Instance.IsSkillBook(selectedItem) 
				&& InventoryManager.Instance.CurrentMode == InventoryMode.Default;
        }

		private void ExecuteReadItem()
		{
			if (!IsSkillBook
				|| !TORSkillBookCampaignBehavior.Instance.IsBookUseful(_lastSetItem))
            {
				TORCommon.Say(String.Format("It seems that there is nothing more to gain from studying {0}", _lastSetItem.Name));
				return;
            }
			if (TORSkillBookCampaignBehavior.Instance.CurrentBook.Equals(_lastSetItem.StringId ?? "")) {
				TORCommon.Say(String.Format("You are already reading {0}", _lastSetItem.Name));
				return;
            }

			TORSkillBookCampaignBehavior.Instance.CurrentBook =
				_lastSetItem.StringId ?? "";
			UpdateReadButton(_lastSetItem);
			TORCommon.Say(String.Format("Selected {0} for reading!", _lastSetItem?.Name));
			return;
		}

		private void AddThrustDamageProperty(TextObject description, in EquipmentElement targetWeapon, int targetWeaponUsageIndex, in EquipmentElement comparedWeapon, int comparedWeaponUsageIndex)
		{
			EquipmentElement equipmentElement = targetWeapon;
			int modifiedThrustDamageForUsage = equipmentElement.GetModifiedThrustDamageForUsage(targetWeaponUsageIndex);
			equipmentElement = targetWeapon;
			WeaponComponentData weaponWithUsageIndex = equipmentElement.Item.GetWeaponWithUsageIndex(targetWeaponUsageIndex);
			equipmentElement = targetWeapon;
			string value = ItemHelper.GetThrustDamageText(weaponWithUsageIndex, equipmentElement.ItemModifier).ToString();
			if (this.IsComparing)
			{
				equipmentElement = comparedWeapon;
				if (!equipmentElement.IsEmpty)
				{
					equipmentElement = comparedWeapon;
					int modifiedThrustDamageForUsage2 = equipmentElement.GetModifiedThrustDamageForUsage(comparedWeaponUsageIndex);
					equipmentElement = comparedWeapon;
					WeaponComponentData weaponWithUsageIndex2 = equipmentElement.Item.GetWeaponWithUsageIndex(comparedWeaponUsageIndex);
					equipmentElement = comparedWeapon;
					string value2 = ItemHelper.GetThrustDamageText(weaponWithUsageIndex2, equipmentElement.ItemModifier).ToString();
					int result = this.CompareValues(modifiedThrustDamageForUsage, modifiedThrustDamageForUsage2);
					this.CreateColoredProperty(this.TargetItemProperties, description.ToString(), value, this.GetColorFromComparison(result, false), 0, null, TooltipProperty.TooltipPropertyFlags.None);
					this.CreateColoredProperty(this.ComparedItemProperties, " ", value2, this.GetColorFromComparison(result, true), 0, null, TooltipProperty.TooltipPropertyFlags.None);
					return;
				}
			}
			this.CreateColoredProperty(this.TargetItemProperties, description.ToString(), value, this.GetColorFromComparison(0, true), 0, null, TooltipProperty.TooltipPropertyFlags.None);
		}

		private void AddIntProperty(TextObject description, int targetValue, int? comparedValue)
		{
			string value = targetValue.ToString();
			if (this.IsComparing && comparedValue != null)
			{
				string value2 = comparedValue.Value.ToString();
				int result = this.CompareValues(targetValue, comparedValue.Value);
				this.CreateColoredProperty(this.TargetItemProperties, description.ToString(), value, this.GetColorFromComparison(result, false), 0, null, TooltipProperty.TooltipPropertyFlags.None);
				this.CreateColoredProperty(this.ComparedItemProperties, " ", value2, this.GetColorFromComparison(result, true), 0, null, TooltipProperty.TooltipPropertyFlags.None);
				return;
			}
			this.CreateColoredProperty(this.TargetItemProperties, description.ToString(), value, this.GetColorFromComparison(0, false), 0, null, TooltipProperty.TooltipPropertyFlags.None);
		}

		private ItemMenuTooltipPropertyVM CreateColoredProperty(MBBindingList<ItemMenuTooltipPropertyVM> targetList, string definition, string value, Color color, int textHeight = 0, HintViewModel hint = null, TooltipProperty.TooltipPropertyFlags propertyFlags = TooltipProperty.TooltipPropertyFlags.None)
		{
			if (color == Colors.Black)
			{
				this.CreateProperty(targetList, definition, value, textHeight, hint);
				return null;
			}
			ItemMenuTooltipPropertyVM itemMenuTooltipPropertyVM = new ItemMenuTooltipPropertyVM(definition, value, textHeight, color, false, hint, propertyFlags);
			targetList.Add(itemMenuTooltipPropertyVM);
			return itemMenuTooltipPropertyVM;
		}

		private ItemMenuTooltipPropertyVM CreateProperty(MBBindingList<ItemMenuTooltipPropertyVM> targetList, string definition, string value, int textHeight = 0, HintViewModel hint = null)
		{
			ItemMenuTooltipPropertyVM itemMenuTooltipPropertyVM = new ItemMenuTooltipPropertyVM(definition, value, textHeight, false, hint);
			targetList.Add(itemMenuTooltipPropertyVM);
			return itemMenuTooltipPropertyVM;
		}

		private int CompareValues(float currentValue, float comparedValue)
		{
			int num = (int)(currentValue * 10000f);
			int num2 = (int)(comparedValue * 10000f);
			if ((num != 0 && (float)MathF.Abs(num) <= MathF.Abs(currentValue)) || (num2 != 0 && (float)MathF.Abs(num2) <= MathF.Abs(currentValue)))
			{
				return 0;
			}
			return this.CompareValues(num, num2);
		}

		private Color GetColorFromComparison(int result, bool isCompared)
		{
			if (result != -1)
			{
				if (result != 1)
				{
					return Colors.Black;
				}
				if (!isCompared)
				{
					return UIColors.PositiveIndicator;
				}
				return UIColors.NegativeIndicator;
			}
			else
			{
				if (!isCompared)
				{
					return UIColors.NegativeIndicator;
				}
				return UIColors.PositiveIndicator;
			}
		}

		[DataSourceProperty]
		public bool IsMagicItem
		{
			get
			{
				return this._isMagicItem;
			}
			set
			{
				if (value != this._isMagicItem)
				{
					this._isMagicItem = value;
					base.OnPropertyChangedWithValue(value, "IsMagicItem");
				}
			}
		}

		[DataSourceProperty]
		public string ItemDescription
		{
			get
			{
				return this._itemDescription;
			}
			set
			{
				if (value != this._itemDescription)
				{
					this._itemDescription = value;
					base.OnPropertyChangedWithValue(value, "ItemDescription");
				}
			}
		}

		[DataSourceProperty]
		public bool HasDescription
		{
			get
			{
				return this._hasDescription;
			}
			set
			{
				if (value != this._hasDescription)
				{
					this._hasDescription = value;
					base.OnPropertyChangedWithValue(value, "HasDescription");
				}
			}
		}

		[DataSourceProperty]
		public MBBindingList<TorItemTraitVM> ItemTraitList
		{
			get
			{
				return this._itemTraitList;
			}
			set
			{
				if (value != this._itemTraitList)
				{
					this._itemTraitList = value;
					base.OnPropertyChangedWithValue(value, "ItemTraitList");
				}
			}
		}

		[DataSourceProperty]
		public HintViewModel ReadHint
		{
			get
			{
				return this._readHint;
			}
			set
			{
				if (value != this._readHint)
				{
					this._readHint = value;
					base.OnPropertyChangedWithValue(value, "ReadHint");
				}
			}
		}

		[DataSourceProperty]
		public bool IsSkillBook
		{
			get
			{
				return this._isSkillBook;
			}
			set
			{
				if (value != this._isSkillBook)
				{
					this._isSkillBook = value;
					base.OnPropertyChangedWithValue(value, "IsSkillBook");
				}
			}
		}
	}
}
