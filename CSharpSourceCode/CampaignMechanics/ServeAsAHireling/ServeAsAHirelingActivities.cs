using System.Collections.Generic;
using TaleWorlds.Core;
using TOR_Core.CharacterDevelopment;
using TOR_Core.CharacterDevelopment.CareerSystem;

namespace TOR_Core.CampaignMechanics.ServeAsAHireling;

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
                    DefaultSkills.Charm,
                    DefaultSkills.Riding,
                    DefaultSkills.Polearm,
                    DefaultSkills.Roguery
                ]);
            }

            if (career == TORCareers.GrailKnight)
            {
                _activitySets.Add(career, [
                    DefaultSkills.OneHanded,
                    DefaultSkills.Charm,
                    DefaultSkills.Riding,
                    DefaultSkills.Steward,
                    TORSkills.Faith
                ]);

            }
            
            if (career == TORCareers.GrailDamsel)
            {
                _activitySets.Add(career, [
                    DefaultSkills.Riding,
                    TORSkills.SpellCraft,
                    TORSkills.Faith,
                    DefaultSkills.Steward,
                    DefaultSkills.Medicine
                ]);
            }
            
            if (career == TORCareers.Necromancer)
            {
                _activitySets.Add(career, [
                    TORSkills.SpellCraft,
                    DefaultSkills.Roguery,
                    DefaultSkills.Medicine,
                    DefaultSkills.Scouting,
                    DefaultSkills.Steward
                ]);
            }

            if (career == TORCareers.BloodKnight)
            {
                _activitySets.Add(career, [
                    DefaultSkills.OneHanded,
                    DefaultSkills.TwoHanded,
                    DefaultSkills.Riding,
                    DefaultSkills.Tactics,
                    DefaultSkills.Leadership
                ]);
            }
            
            if (career == TORCareers.MinorVampire)
            {
                _activitySets.Add(career, [
                    DefaultSkills.OneHanded,
                    DefaultSkills.Charm,
                    TORSkills.SpellCraft,
                    DefaultSkills.Roguery,
                    DefaultSkills.Leadership,
                ]);
            }
            
            if (career == TORCareers.Necrarch)
            {
                _activitySets.Add(career, [
                    TORSkills.SpellCraft,
                    DefaultSkills.Roguery,
                    DefaultSkills.Medicine,
                    DefaultSkills.Engineering,
                    DefaultSkills.Steward
                ]);
            }
            
            
            //Empire careers
            if (career == TORCareers.Mercenary)
            {
                _activitySets.Add(career, [
                    DefaultSkills.TwoHanded,
                    DefaultSkills.Bow,
                    TORSkills.GunPowder,
                    DefaultSkills.Trade,
                    DefaultSkills.Tactics,
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
                    DefaultSkills.TwoHanded,
                    DefaultSkills.Crossbow,
                    TORSkills.GunPowder,
                    TORSkills.Faith,
                    DefaultSkills.OneHanded
                ]);
            }
            
            if (career == TORCareers.WarriorPriest)
            {
                _activitySets.Add(career, [
                    DefaultSkills.OneHanded,
                    DefaultSkills.TwoHanded,
                    DefaultSkills.Athletics,
                    DefaultSkills.Medicine,
                    TORSkills.Faith,
                ]);
            }
            
            if (career == TORCareers.WarriorPriestUlric)
            {
                _activitySets.Add(career, [
                    DefaultSkills.Scouting,
                    DefaultSkills.TwoHanded,
                    DefaultSkills.Athletics,
                    DefaultSkills.Leadership,
                    TORSkills.Faith,
                ]);
            }
            
            if (career == TORCareers.Waywatcher)
            {
                _activitySets.Add(career, [
                    DefaultSkills.Bow,
                    DefaultSkills.Scouting,
                    DefaultSkills.Roguery,
                    DefaultSkills.Athletics,
                    DefaultSkills.Medicine
                ]);
            }
            
            if (career == TORCareers.Spellsinger)
            {
                _activitySets.Add(career, [
                    TORSkills.SpellCraft,
                    DefaultSkills.Riding,
                    TORSkills.Faith,
                    DefaultSkills.Charm,
                    DefaultSkills.Medicine
                ]);
            }
            if (career == TORCareers.GreyLord)
            {
                _activitySets.Add(career, [
                    TORSkills.SpellCraft,
                    DefaultSkills.Steward,
                    DefaultSkills.Leadership,
                    DefaultSkills.Charm,
                    DefaultSkills.Medicine
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