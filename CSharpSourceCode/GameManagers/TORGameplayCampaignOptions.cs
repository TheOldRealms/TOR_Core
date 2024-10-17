using System;
using TaleWorlds.Engine.Options;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Options.ManagedOptions;
using TOR_Core.HarmonyPatches;
using TOR_Core.Utilities;

namespace TOR_Core.GameManagers;

public class TORGameplayCampaignOptions: ManagedOptionData, INumericOptionData, IOptionData
{
    private TORManagedOptionsType _underlyingType;
    public TORGameplayCampaignOptions(TORManagedOptionsType type) : base((ManagedOptions.ManagedOptionsType)type)
    {
        _underlyingType = type;

        switch (_underlyingType)
        {
            case TORManagedOptionsType.NumberOfMaximumLooterPartiesEarly: 
                SetValue(TORConfig.NumberOfMaximumLooterPartiesEarly);
                break;
            case TORManagedOptionsType.NumberOfMaximumLooterParties:
                SetValue(TORConfig.NumberOfMaximumLooterParties);
                break;
            case TORManagedOptionsType.NumberOfMaximumLooterPartiesLate:
                SetValue(TORConfig.NumberOfMaximumLooterPartiesLate);
                break;
            case TORManagedOptionsType.NumberOfMaximumBanditPartiesAroundEachHideout:
                SetValue(TORConfig.NumberOfMaximumBanditPartiesAroundEachHideout);
                break;
            case TORManagedOptionsType.NumberOfMaximumBanditPartiesInEachHideout:
                SetValue(TORConfig.NumberOfMaximumBanditPartiesInEachHideout);
                break;
            case TORManagedOptionsType.NumberOfInitialHideoutsAtEachBanditFaction:
                SetValue(TORConfig.NumberOfInitialHideoutsAtEachBanditFaction);
                break;
            case TORManagedOptionsType.NumberOfMaximumHideoutsAtEachBanditFaction:
                SetValue(TORConfig.NumberOfMaximumHideoutsAtEachBanditFaction);
                break;
            
            case TORManagedOptionsType.FakeBannerFrequency:
                SetValue(TORConfig.FakeBannerFrequency);
                break;
            case TORManagedOptionsType.NumberOfTroopsPerFormationWithStandard:
                SetValue(TORConfig.NumberOfTroopsPerFormationWithStandard);
                break;
            case TORManagedOptionsType.DeclareWarScoreDistanceMultiplier:
                SetValue(TORConfig.DeclareWarScoreDistanceMultiplier);
                break;
            case TORManagedOptionsType.DeclareWarScoreFactionStrengthMultiplier:
                SetValue(TORConfig.DeclareWarScoreFactionStrengthMultiplier);
                break;
            case TORManagedOptionsType.DeclareWarScoreReligiousEffectMultiplier:
                SetValue(TORConfig.DeclareWarScoreReligiousEffectMultiplier);
                break;
            case TORManagedOptionsType.NumMinKingdomWars:
                SetValue(TORConfig.NumMinKingdomWars);
                break;
            case TORManagedOptionsType.NumMaxKingdomWars:
                SetValue(TORConfig.NumMaxKingdomWars);
                break;
            case TORManagedOptionsType.MinPeaceDays:
                SetValue(TORConfig.MinPeaceDays);
                break;
            case TORManagedOptionsType.MinWarDays:
                SetValue(TORConfig.MinWarDays);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
    
    
    public float GetDefaultValue()
    {
        return 0;
    }

    public void Commit()
    {
        switch (_underlyingType)
        {
            case TORManagedOptionsType.NumberOfMaximumLooterPartiesEarly:
                TORConfig.NumberOfMaximumLooterPartiesEarly = (int) this.GetValue(false);
                break;
            case TORManagedOptionsType.NumberOfMaximumLooterParties:
                TORConfig.NumberOfMaximumLooterParties = (int) this.GetValue(false);
                break;
            case TORManagedOptionsType.NumberOfMaximumLooterPartiesLate:
                TORConfig.NumberOfMaximumLooterPartiesLate = (int) this.GetValue(false);
                break;
            case TORManagedOptionsType.NumberOfMaximumBanditPartiesAroundEachHideout:
                TORConfig.NumberOfMaximumBanditPartiesAroundEachHideout = (int) this.GetValue(false);
                break;
            case TORManagedOptionsType.NumberOfMaximumBanditPartiesInEachHideout:
                TORConfig.NumberOfMaximumBanditPartiesInEachHideout = (int) this.GetValue(false);
                break;
            case TORManagedOptionsType.NumberOfInitialHideoutsAtEachBanditFaction:
                TORConfig.NumberOfInitialHideoutsAtEachBanditFaction = (int) this.GetValue(false);
                break;
            case TORManagedOptionsType.NumberOfMaximumHideoutsAtEachBanditFaction:
                TORConfig.NumberOfMaximumHideoutsAtEachBanditFaction = (int) this.GetValue(false);
                break;
            case TORManagedOptionsType.DeclareWarScoreDistanceMultiplier:
                TORConfig.DeclareWarScoreDistanceMultiplier = (float) this.GetValue(false);
                break;
            case TORManagedOptionsType.DeclareWarScoreFactionStrengthMultiplier:
                TORConfig.DeclareWarScoreFactionStrengthMultiplier = (float) this.GetValue(false);
                break;
            case TORManagedOptionsType.DeclareWarScoreReligiousEffectMultiplier:
                TORConfig.DeclareWarScoreReligiousEffectMultiplier = (float) this.GetValue(false);
                break;
            case TORManagedOptionsType.NumMinKingdomWars:
                TORConfig.NumMinKingdomWars = (int) this.GetValue(false);
                break;
            case TORManagedOptionsType.NumMaxKingdomWars:
                TORConfig.NumMaxKingdomWars = (int) this.GetValue(false);
                break;
            case TORManagedOptionsType.MinPeaceDays:
                TORConfig.MinPeaceDays = (int) this.GetValue(false);
                break;
            case TORManagedOptionsType.MinWarDays:
                TORConfig.MinWarDays = (int) this.GetValue(false);
                break;
            case TORManagedOptionsType.FakeBannerFrequency:
                TORConfig.FakeBannerFrequency = (int) this.GetValue(false);
                break;
            case TORManagedOptionsType.NumberOfTroopsPerFormationWithStandard:
                TORConfig.NumberOfTroopsPerFormationWithStandard = (int) this.GetValue(false);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        
    }
    
    

    public object GetOptionType()
    {
        return (object)this.Type;
    }

    public (string, bool) GetIsDisabledAndReasonID()
    {
        return ("",false);
    }

    public float GetMinValue()
    {
        switch (_underlyingType)
        {
            case TORManagedOptionsType.NumberOfMaximumLooterPartiesEarly:
                break;
            case TORManagedOptionsType.NumberOfMaximumLooterParties:
                break;
            case TORManagedOptionsType.NumberOfMaximumLooterPartiesLate:
                break;
            case TORManagedOptionsType.NumberOfMaximumBanditPartiesAroundEachHideout:
                break;
            case TORManagedOptionsType.NumberOfMaximumBanditPartiesInEachHideout:
                break;
            case TORManagedOptionsType.NumberOfInitialHideoutsAtEachBanditFaction:
                break;
            case TORManagedOptionsType.NumberOfMaximumHideoutsAtEachBanditFaction:
                break;
            
            
            case TORManagedOptionsType.DeclareWarScoreDistanceMultiplier:
                return 0;
            case TORManagedOptionsType.DeclareWarScoreFactionStrengthMultiplier:
                return 0.25f;
            case TORManagedOptionsType.DeclareWarScoreReligiousEffectMultiplier:
                return 0;
            case TORManagedOptionsType.NumMinKingdomWars: return 1;
            case TORManagedOptionsType.NumMaxKingdomWars: return 1;
            case TORManagedOptionsType.MinPeaceDays : return 1;
            case TORManagedOptionsType.MinWarDays : return 1;
            default:
                return 0f;
        }

        return 0;
    }

    public float GetMaxValue()
    {
        switch (_underlyingType)
        {
            case TORManagedOptionsType.NumberOfMaximumLooterPartiesEarly:
                return 500f;
            case TORManagedOptionsType.NumberOfMaximumLooterParties:
                return 500f;
            case TORManagedOptionsType.NumberOfMaximumLooterPartiesLate:
                return 500f;
            case TORManagedOptionsType.NumberOfMaximumBanditPartiesAroundEachHideout:
                return 100f;
            case TORManagedOptionsType.NumberOfMaximumBanditPartiesInEachHideout:
                return 8f;
            case TORManagedOptionsType.NumberOfInitialHideoutsAtEachBanditFaction:
                return 100f;
            case TORManagedOptionsType.NumberOfMaximumHideoutsAtEachBanditFaction:
                return 100f;
            
            case TORManagedOptionsType.NumMinKingdomWars: return 10;
            case TORManagedOptionsType.NumMaxKingdomWars: return 10;
            
            case TORManagedOptionsType.MinPeaceDays : return 100;
            case TORManagedOptionsType.MinWarDays : return 100;
            
            
            case TORManagedOptionsType.FakeBannerFrequency : return 25;
            case TORManagedOptionsType.NumberOfTroopsPerFormationWithStandard: return 10;
            case TORManagedOptionsType.DeclareWarScoreDistanceMultiplier:
                return 100f;
                break;
            case TORManagedOptionsType.DeclareWarScoreFactionStrengthMultiplier:
                return 10f;
                break;
            case TORManagedOptionsType.DeclareWarScoreReligiousEffectMultiplier:
                return 10f;
                break;
            default:
                return 500f;
        }

        return 0;
    }

    public bool GetIsDiscrete()
    {
        switch (_underlyingType)
        {
            case TORManagedOptionsType.NumberOfMaximumLooterPartiesEarly:
                break;
            case TORManagedOptionsType.NumberOfMaximumLooterParties:
                break;
            case TORManagedOptionsType.NumberOfMaximumLooterPartiesLate:
                break;
            case TORManagedOptionsType.NumberOfMaximumBanditPartiesAroundEachHideout:
                break;
            case TORManagedOptionsType.NumberOfMaximumBanditPartiesInEachHideout:
                break;
            case TORManagedOptionsType.NumberOfInitialHideoutsAtEachBanditFaction:
                break;
            case TORManagedOptionsType.NumberOfMaximumHideoutsAtEachBanditFaction:
                break;
            case TORManagedOptionsType.DeclareWarScoreDistanceMultiplier:
                return false;
                break;
            case TORManagedOptionsType.DeclareWarScoreFactionStrengthMultiplier:
                return false;
                break;
            case TORManagedOptionsType.DeclareWarScoreReligiousEffectMultiplier:
                return false;
                break;
            case TORManagedOptionsType.NumMinKingdomWars:
                break;
            case TORManagedOptionsType.NumMaxKingdomWars:
                break;
            case TORManagedOptionsType.MinPeaceDays:
                break;
            case TORManagedOptionsType.MinWarDays:
                break;
            case TORManagedOptionsType.FakeBannerFrequency:
                break;
            case TORManagedOptionsType.NumberOfTroopsPerFormationWithStandard:
                break;
            default:
                return true;
        }
        return true;
    }

    public int GetDiscreteIncrementInterval()
    {
        return 1;
    }

    public bool GetShouldUpdateContinuously()
    {
        return true;
    }

  
}