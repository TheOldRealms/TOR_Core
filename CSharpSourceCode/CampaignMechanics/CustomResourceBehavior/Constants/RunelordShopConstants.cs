using System.Collections.Generic;

namespace TOR_Core.CampaignMechanics.CustomResourceBehavior.Constants;

public static class RunelordShopConstants
{

    //TODO: Magic strings mapping, also this should be in another class "constants" or something.
    //I don't like the reliance on magic strings here, but I haven't thought of another way yet.
    //These have to be a set list, as the game engine assigns arbitrary tiers based on armour per piece.
    public static List<string> TIER_1_ARMOUR_UNLOCKS =
    [
        //Militia
        "tor_dw_head_helm_militia_001",
        "tor_dw_body_armour_militia_001",
        "tor_dw_shoulderpads_militia_001",
        "tor_dw_arm_bracers_militia_001",
        "Item.tor_dw_leg_boots_militia_001",

        //Miner & Prospector
        "tor_dw_head_helm_miner_001",
        "tor_dw_head_helm_miner_002",
        "tor_dw_head_helm_miner_003",
        "tor_dw_body_armour_miner_001",
        "tor_dw_arm_bracers_miner_001",
        "tor_dw_leg_boots_miner_001",

        //Warrior
        "tor_dw_head_helm_warrior_001",
        "tor_dw_body_armour_warrior_001",
        "tor_dw_body_armour_warrior_002",
        "tor_dw_shoulderpads_warrior_001",
        "tor_dw_arm_bracers_warrior_001",
        "tor_dw_leg_boots_warrior_001",
    ];

    public static List<string> TIER_2_ARMOUR_UNLOCKS =
    [
        //Longbeard,
        "tor_dw_head_helm_longbeard_001",
        "tor_dw_body_armour_longbeard_001",
        "tor_dw_shoulder_shoulderpads_longbeard_001",
        "tor_dw_arm_bracers_longbeard_001",
        "tor_dw_leg_boots_longbeard_001",
    ];


    //Either legendary weapons or items sold in the engineering shop.
    public static List<string> EXCLUDED_ITEMS =
    [
        // Legendary Weapons
        "tor_dwarf_weapon_1h_axe_of_grimnir",
        "tor_dwarf_weapon_hammer_of_angrund",
        "tor_dwarf_weapon_klad_brakak",
        "tor_dwarf_weapon_greataxe_ungrim_001",

        //Weapons
        "tor_dwarf_weapon_greataxe_001",
        "tor_dwarf_1h_spanner_001",
        "dwarf_1h_engineer_hammer_001",
        "tor_dwarf_2h_spanner_001",
        "dwarf_2h_engineer_hammer_001",

        // Armors - Head
        "tor_dw_head_helm_ungrim_001",
        "tor_dw_head_helm_ranger_001",
        "tor_dw_head_helm_ranger_002",
        "tor_dw_head_helm_ranger_003",
        "tor_dw_head_helm_ranger_004",
        "tor_dw_head_apprentice_002",
        "tor_dw_head_apprentice_001",
        "tor_dw_head_journeyman_001",
        "tor_dw_head_engineer_001",
        "tor_dw_head_engineer_002",
        // Armors - Shoulder
        "tor_dw_shoulder_cape_ranger_001",
        "tor_dw_shoulder_cape_ranger_002",
        "tor_dw_shoulder_shoulderpads_apprentice_001",
        "tor_dw_shoulder_shoulderpads_journeyman_001",
        "tor_dw_shoulder_shoulderpads_engineer_001",
        "tor_dw_shoulder_shoulderpads_ungrim_001",
        // Armors - Body
        "tor_dw_body_armour_apprentice_001",
        "tor_dw_body_armour_journeyman_001",
        "tor_dw_body_armour_engineer_001",
        "tor_dw_body_armour_ungrim_001",
        "tor_dw_body_armour_ranger_001",
        // Armors - Arms
        "tor_dw_arm_gloves_apprentice_001",
        "tor_dw_arm_gloves_journeyman_001",
        "tor_dw_arm_gloves_engineer_001",
        "tor_dw_arm_bracers_ranger_001",
        // Armors - Legs
        "tor_dw_leg_boots_apprentice_001",
        "tor_dw_leg_boots_journeyman_001",
        "tor_dw_leg_boots_engineer_001",
        "tor_dw_leg_boots_ranger_001"
    ];
}
