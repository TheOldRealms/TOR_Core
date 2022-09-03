using TaleWorlds.Core;

public static class ItemUtilities
{
    
    
    /// <summary>
    /// Checks if the current weapon is shooting scatter shots or grenades, or is scatter/grenade ammunition
    /// </summary>
    /// <param name="itemObject"></param>
    /// <returns></returns>
    public static bool IsSpecialAmmunitionItem(ItemObject itemObject)
    {
        if (!IsAmmunitionItem(itemObject))
            return false;
        
        if (itemObject.ToString().Contains("blunderbuss"))
        {
            return true;
        }
        
        if (itemObject.ToString().Contains("grenade") || itemObject.ToString().Contains("scatter"))
        {
            return true;
        }

        return false;
    }

    public static bool IsAmmunitionItem(ItemObject itemObject)
    {
        if (itemObject.WeaponComponent == null) return false;
        if ( itemObject.WeaponComponent.PrimaryWeapon == null) return false;
        
        return itemObject.WeaponComponent.PrimaryWeapon.IsRangedWeapon ||
               itemObject.WeaponComponent.PrimaryWeapon.IsAmmo;
    }
}