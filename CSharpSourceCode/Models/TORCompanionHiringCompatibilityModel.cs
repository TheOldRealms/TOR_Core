using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.Models;

/// <summary>
/// Model for determining companion hiring compatibility based on racial and cultural restrictions.
/// </summary>
public class TORCompanionHiringCompatibilityModel : GameModel
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
}
