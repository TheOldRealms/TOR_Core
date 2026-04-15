using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.Models;

/// <summary>
/// Model for determining companion hiring compatibility based on racial and cultural restrictions.
/// </summary>
public class TORHiringCompatibilityModel : GameModel
{
    /// <summary>
    /// Determines if a player can hire a wanderer based on their racial/cultural compatibility.
    /// Rules:
    /// - Greenskins can only hire other greenskins
    /// - Dwarfs can hire dwarfs and humans
    /// - Humans can hire humans,  dwarfs and eonir
    /// - Vampires (Mousillon and Sylvania) can only hire Mousillon and Sylvania
    /// - Asrai can hire Eonir and Asrai
    /// - Eonir can hire humans, Eonir, and Asrai
    /// - Chaos can hire vampires and humans
    /// </summary>
    public virtual bool CanPlayerHireWanderer(Hero player, Hero wanderer)
    {
        if (player == null || wanderer == null)
            return false;

        string playerCulture = player.Culture?.StringId;
        string wandererCulture = wanderer.Culture?.StringId;

        if (string.IsNullOrEmpty(playerCulture) || string.IsNullOrEmpty(wandererCulture))
            return false; // Can't determine, block hiring

        // Greenskins can only hire other greenskins
        if (playerCulture == TORConstants.Cultures.GREENSKIN)
        {
            return wandererCulture == TORConstants.Cultures.GREENSKIN;
        }

        // Dwarfs can hire dwarfs and humans
        if (playerCulture == TORConstants.Cultures.DAWI)
        {
            if (wanderer.IsSpellCaster())
            {
                return false;
            }
            
            return wandererCulture == TORConstants.Cultures.DAWI ||
                   IsHumanCulture(wandererCulture);
        }

        // Humans can hire dwarfs and eonir
        if (IsHumanCulture(playerCulture))
        {
            if (wanderer.HasAttribute("Runesmith"))
            {
                return false;
            }
            
            return wandererCulture == TORConstants.Cultures.DAWI || 
                   IsHumanCulture(wandererCulture) || 
                   wandererCulture == TORConstants.Cultures.EONIR;
        }

        // Vampires (Mousillon and Sylvania) can only hire Mousillon and Sylvania
        if (IsVampireCulture(playerCulture))
        {
            return IsVampireCulture(wandererCulture);
        }

        // Asrai can hire Eonir and Asrai
        if (playerCulture == TORConstants.Cultures.ASRAI)
        {
            return wandererCulture == TORConstants.Cultures.EONIR ||
                   wandererCulture == TORConstants.Cultures.ASRAI;
        }

        // Eonir can hire humans, Eonir, and Asrai
        if (playerCulture == TORConstants.Cultures.EONIR)
        {
            return IsHumanCulture(wandererCulture) ||
                   wandererCulture == TORConstants.Cultures.EONIR ||
                   wandererCulture == TORConstants.Cultures.ASRAI;
        }

        // Chaos can hire vampires and humans
        if (playerCulture == TORConstants.Cultures.CHAOS)
        {
            return IsVampireCulture(wandererCulture) ||
                   IsHumanCulture(wandererCulture);
        }

        // Default: block hiring for unknown cultures
        return false;
    }

    private bool IsHumanCulture(string cultureId)
    {
        return cultureId == TORConstants.Cultures.EMPIRE ||
               cultureId == TORConstants.Cultures.BRETONNIA;
    }

    private bool IsVampireCulture(string cultureId)
    {
        return cultureId == TORConstants.Cultures.MOUSILLON ||
               cultureId == TORConstants.Cultures.SYLVANIA;
    }

    private bool IsCastleBoundVillage(Settlement settlement)
    {
        // noble pool
        return settlement.IsVillage && settlement.Village.Bound.IsCastle;
    }
    private bool IsRoRVolunteer(Hero seller)
    {
        Settlement settlement = seller?.CurrentSettlement;

        if (!(settlement?.IsRoRSettlement() ?? false))
        {
            return false;
        }

        return seller.Occupation == Occupation.Artisan ||
               seller.Occupation == Occupation.Merchant ||
               seller.Occupation == Occupation.Headman ||
               seller.Occupation == Occupation.RuralNotable;
    }

    /// <summary>
    /// Slightly more forgiving hiring allowed for troops
    /// Humans can hire humans, and eonir and dwarfs unless the latter two are nobles. 
    /// Dwarfs can hire dwarfs and humans unless humans are nobles
    /// Bretonnians can hire mousillon and bretonnians, other humans and dwarfs
    /// mousillon can hire bretonnians, empire and vampires
    /// greenskins can only hire other greenskins
    /// vampires can hire from vc, mousillon, empire and bretonnia unless the latter two have nobles.
    /// Asrai can hire from Eonir and other Asrai
    /// 
    /// </summary>
    /// <param name="player"></param>
    /// <param name="seller"></param>
    /// <returns></returns>
    public virtual bool CanPlayerHireTroopFromSeller(Hero player, Hero seller)
    {
        if (player == null || seller == null)
            return false;

        string playerCulture = player.Culture?.StringId;
        string sellerCulture = seller.Culture?.StringId;
        Settlement settlement = seller.CurrentSettlement;

        bool hasEliteUnits = IsCastleBoundVillage(settlement) || IsRoRVolunteer(seller);

        if (string.IsNullOrEmpty(playerCulture) || string.IsNullOrEmpty(sellerCulture))
            return false; // Can't determine, block hiring

        bool isCrossCultureRecruitment = playerCulture != sellerCulture;

        if (isCrossCultureRecruitment)
        {
            // no cross culture from castle villages, except bretonnia & mousillon and mousillon -> sylvania
            if (hasEliteUnits)
            {
                bool isMousillonCastleException =
                    playerCulture == TORConstants.Cultures.MOUSILLON &&
                    (sellerCulture == TORConstants.Cultures.BRETONNIA ||
                     sellerCulture == TORConstants.Cultures.SYLVANIA);

                bool isBretonniaCastleException =
                    playerCulture == TORConstants.Cultures.BRETONNIA &&
                    sellerCulture == TORConstants.Cultures.MOUSILLON;

                if (!isMousillonCastleException && !isBretonniaCastleException)
                {
                    return false;
                }
            }
        }

        // Greenskins can only hire other greenskins
        if (playerCulture == TORConstants.Cultures.GREENSKIN)
        {
            return sellerCulture == TORConstants.Cultures.GREENSKIN;
        }

        // Dwarfs can hire dwarfs and humans
        if (playerCulture == TORConstants.Cultures.DAWI)
        {
            if (IsHumanCulture(sellerCulture) && hasEliteUnits)
            {
                return false;
            }
            
            return sellerCulture == TORConstants.Cultures.DAWI ||
                   IsHumanCulture(sellerCulture);
        }

        // Empire can hire dwarfs, eonir and humans
        if (playerCulture == TORConstants.Cultures.EMPIRE)
        {
            if ((sellerCulture == TORConstants.Cultures.EONIR || sellerCulture == TORConstants.Cultures.DAWI || sellerCulture == TORConstants.Cultures.SYLVANIA)  && hasEliteUnits)
            {
                return false;
            }
            
            return sellerCulture == TORConstants.Cultures.DAWI ||
                   IsHumanCulture(sellerCulture) ||
                   sellerCulture == TORConstants.Cultures.EONIR;
        }

        // Bretonnians can hire mousillon, bretonnians, other humans and dwarfs
        if (playerCulture == TORConstants.Cultures.BRETONNIA)
        {
            if ((sellerCulture == TORConstants.Cultures.SYLVANIA || sellerCulture == TORConstants.Cultures.EONIR || sellerCulture == TORConstants.Cultures.DAWI)  && hasEliteUnits)
            {
                return false;
            }

            return sellerCulture == TORConstants.Cultures.BRETONNIA ||
                   sellerCulture == TORConstants.Cultures.MOUSILLON ||
                   sellerCulture == TORConstants.Cultures.EMPIRE ||
                   sellerCulture == TORConstants.Cultures.DAWI;
        }

        // Mousillon can hire bretonnians, empire and vampires
        if (playerCulture == TORConstants.Cultures.MOUSILLON)
        {
            if (sellerCulture == TORConstants.Cultures.EMPIRE //Bretonnia excluded deliberately because of Black Grail knight who can "persuade" knights to his side.
                && hasEliteUnits)
            {
                return false;
            }
            return sellerCulture == TORConstants.Cultures.BRETONNIA ||
                   sellerCulture == TORConstants.Cultures.EMPIRE ||
                   IsVampireCulture(sellerCulture);
        }

        // Vampires (Sylvania) can hire from VC, empire, mousillon and bretonnia
        if (playerCulture == TORConstants.Cultures.SYLVANIA)
        {
            if ((
                    sellerCulture == TORConstants.Cultures.EMPIRE || 
                    sellerCulture == TORConstants.Cultures.BRETONNIA)
                && hasEliteUnits)
            {
                return false;
            }

            return IsVampireCulture(sellerCulture) ||
                   sellerCulture == TORConstants.Cultures.EMPIRE ||
                   sellerCulture == TORConstants.Cultures.MOUSILLON ||
                   sellerCulture == TORConstants.Cultures.BRETONNIA;
        }

        // Asrai can hire from Eonir and other Asrai
        if (playerCulture == TORConstants.Cultures.ASRAI)
        {
            return sellerCulture == TORConstants.Cultures.EONIR ||
                   sellerCulture == TORConstants.Cultures.ASRAI;
        }

        // Eonir can hire humans, Eonir, and Asrai (same as wanderer rules)
        if (playerCulture == TORConstants.Cultures.EONIR)
        {
            return IsHumanCulture(sellerCulture) ||
                   sellerCulture == TORConstants.Cultures.EONIR ||
                   sellerCulture == TORConstants.Cultures.ASRAI;
        }

        // Chaos can hire vampires and humans (same as wanderer rules)
        if (playerCulture == TORConstants.Cultures.CHAOS)
        {
            return IsVampireCulture(sellerCulture) ||
                   IsHumanCulture(sellerCulture);
        }

        // Default: block hiring for unknown cultures
        return false;
    }
}
