using System.Collections.Generic;
using TaleWorlds.Core;
using TOR_Core.CharacterDevelopment;
using TOR_Core.CharacterDevelopment.CareerSystem;

namespace TOR_Core.CampaignMechanics.ServeAsAMerc;

public class ServeAsAHirelingActivities
{
    private readonly Dictionary<CareerObject, List<SkillObject>> _activitySets;
    public ServeAsAHirelingActivities()
    {
        _activitySets = new Dictionary<CareerObject, List<SkillObject>>();
        foreach (var career in TORCareers.All)
        {
            //note order matters!

            
            if (career == TORCareers.BlackGrailKnight)
            {
                _activitySets.Add(career, [
                    DefaultSkills.OneHanded,
                    DefaultSkills.Roguery,
                    DefaultSkills.TwoHanded,
                    DefaultSkills.Athletics,
                    DefaultSkills.Tactics
                ]);
            }

            if (career == TORCareers.GrailKnight)
            {
                _activitySets.Add(career, [
                    DefaultSkills.Charm,
                    DefaultSkills.Riding,
                    DefaultSkills.TwoHanded,
                    TORSkills.Faith,
                    DefaultSkills.Steward
                ]);

            }
            
            if (career == TORCareers.GrailDamsel)
            {
                _activitySets.Add(career, [
                    TORSkills.SpellCraft,
                    TORSkills.Faith,
                    DefaultSkills.Charm,
                    DefaultSkills.Steward,
                    DefaultSkills.Medicine
                ]);
            }
            
            if (career == TORCareers.Necromancer)
            {
                _activitySets.Add(career, [
                    TORSkills.SpellCraft,
                    DefaultSkills.Tactics,
                    DefaultSkills.Engineering,
                    DefaultSkills.Medicine,
                    DefaultSkills.Steward
                ]);
            }

            if (career == TORCareers.BloodKnight)
            {
                _activitySets.Add(career, [
                    DefaultSkills.OneHanded,
                    DefaultSkills.TwoHanded,
                    DefaultSkills.Athletics,
                    DefaultSkills.Trade,
                    DefaultSkills.Medicine
                ]);
            }
            
            if (career == TORCareers.MinorVampire)
            {
                _activitySets.Add(career, [
                    DefaultSkills.OneHanded,
                    TORSkills.SpellCraft,
                    DefaultSkills.Athletics,
                    DefaultSkills.Trade,
                    DefaultSkills.Medicine
                ]);
            }
            
            if (career == TORCareers.Necrarch)
            {
                _activitySets.Add(career, [
                    TORSkills.SpellCraft,
                    DefaultSkills.Tactics,
                    DefaultSkills.Engineering,
                    DefaultSkills.Medicine,
                    DefaultSkills.Steward
                ]);
            }
            
            
            //Empire careers
            if (career == TORCareers.Mercenary)
            {
                _activitySets.Add(career, [
                    DefaultSkills.OneHanded,
                    DefaultSkills.TwoHanded,
                    DefaultSkills.Athletics,
                    DefaultSkills.Trade,
                    DefaultSkills.Medicine
                ]);
            }
            
            if (career == TORCareers.ImperialMagister)
            {
                _activitySets.Add(career, [
                    TORSkills.SpellCraft,
                    DefaultSkills.Steward,
                    DefaultSkills.OneHanded,
                    DefaultSkills.Medicine,
                    DefaultSkills.Tactics
                ]);
            }
            
            if (career == TORCareers.WitchHunter)
            {
                _activitySets.Add(career, [
                    TORSkills.GunPowder,
                    TORSkills.Faith,
                    DefaultSkills.Roguery,
                    DefaultSkills.Scouting,
                    DefaultSkills.Tactics
                ]);
            }
            
            if (career == TORCareers.WarriorPriest)
            {
                _activitySets.Add(career, [
                    DefaultSkills.OneHanded,
                    TORSkills.Faith,
                    DefaultSkills.TwoHanded,
                    DefaultSkills.Athletics,
                    DefaultSkills.Tactics
                ]);
            }
            
            if (career == TORCareers.WarriorPriestUlric)
            {
                _activitySets.Add(career, [
                    DefaultSkills.OneHanded,
                    TORSkills.Faith,
                    DefaultSkills.TwoHanded,
                    DefaultSkills.Athletics,
                    DefaultSkills.Tactics
                ]);
            }
        }
    }
    
    public List<SkillObject> GetHirelingActivities(CareerObject careerObject)
    {
        if (_activitySets.TryGetValue(careerObject, out var activities))
        {
            return activities;
        }

        return new List<SkillObject>();
    }
}