using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TOR_Core.CharacterDevelopment;
using TOR_Core.CharacterDevelopment.CareerSystem;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignMechanics.ServeAsAHireling;

public class ServeAsAHirelingActivities
{
    public Dictionary<CareerObject, List<SkillObject>> _activitySets;
    public ServeAsAHirelingActivities()
    {
        _activitySets = new Dictionary<CareerObject, List<SkillObject>>
        {
            //Mousillon
            {
                TORCareers.BlackGrailKnight, [
                    DefaultSkills.OneHanded,  // Practice sword-fighting
                    DefaultSkills.Charm,      // Spread The Lady's Lie among the nobles
                    DefaultSkills.Riding,     // Ride ahead of the army
                    DefaultSkills.Polearm,    // Saddle up and joust with other nobles
                    DefaultSkills.Roguery     // Discipline the peasantry
                ]
            },
            //Bretonnia
            {
                TORCareers.GrailKnight, [
                    DefaultSkills.OneHanded,  // Practice sword-fighting
                    DefaultSkills.Charm,      // Socialize with other nobles
                    DefaultSkills.Riding,     // Ride ahead of the army
                    DefaultSkills.Polearm,    // Practice jousting
                    TORSkills.Faith           // Pray to the Lady of the Lake
                ]
            },
            {
                TORCareers.GrailDamsel, [
                    DefaultSkills.Riding,     // Converse with the stabled horses
                    TORSkills.Spellcraft,     // Prepare warding rituals
                    TORSkills.Faith,          // Bless the troops in the name of The Lady
                    DefaultSkills.Steward,    // Advise the army commander
                    DefaultSkills.Medicine    // Mediate the quarreling nobles
                ]
            },
            //Vampire Counts
            {
                TORCareers.Necromancer, [
                    TORSkills.Spellcraft,     // Study tomes of knowledge
                    DefaultSkills.Roguery,    // Raid nearby cemeteries
                    DefaultSkills.Medicine,   // Experiment on dead bodies
                    DefaultSkills.Leadership, // Send your minions ahead of the army
                    DefaultSkills.Steward     // Assist the vampire lieutenants
                ]
            },
            {
                TORCareers.BloodKnight, [
                    DefaultSkills.OneHanded,  // Train with a sword
                    DefaultSkills.TwoHanded,  // Train with a great sword
                    DefaultSkills.Riding,     // Ride in the vanguard
                    DefaultSkills.Tactics,    // Participate in strategy meetings
                    DefaultSkills.Leadership  // Separate the weak from the strong
                ]
            },
            {
                TORCareers.MinorVampire, [
                    DefaultSkills.OneHanded,  // Spar with the troops
                    DefaultSkills.Charm,      // Curry favor with the commander
                    TORSkills.Spellcraft,     // Focus on the vile energies in the area
                    DefaultSkills.Roguery,    // Search for prey to feed on
                    DefaultSkills.Leadership  // Command the sheep
                ]
            },
            {
                TORCareers.Necrarch, [
                    TORSkills.Spellcraft,     // Hone your Witchsight
                    DefaultSkills.Roguery,    // Search for prey to feed on
                    DefaultSkills.Medicine,   // Dissect test subjects
                    DefaultSkills.Engineering,// Study arcane blueprints
                    DefaultSkills.Steward     // Issue orders to your servants
                ]
            },
            //Empire
            {
                TORCareers.Mercenary, [
                    DefaultSkills.TwoHanded,  // Discuss techniques with shock troops
                    DefaultSkills.Bow,        // Target practice with archer regiments
                    TORSkills.GunPowder,      // Maintain gunpowder weapons
                    DefaultSkills.Trade,      // Barter and haggle with the quartermaster
                    DefaultSkills.Tactics     // Eavesdrop by the commander's tent
                ]
            },
            {
                TORCareers.ImperialMagister, [
                    TORSkills.Spellcraft,     // Focus on your studies
                    DefaultSkills.Steward,    // Assist your lord with accounting tasks
                    DefaultSkills.OneHanded,  // Join the sparring soldiers
                    DefaultSkills.Medicine,   // Care for the wounded troops
                    DefaultSkills.Tactics     // Advise the commander
                ]
            },
            {
                TORCareers.WitchHunter, [
                    DefaultSkills.TwoHanded,  // Great sword sparring
                    DefaultSkills.Crossbow,   // Target practice with crossbows
                    TORSkills.GunPowder,      // Maintain gunpowder weapons
                    TORSkills.Faith,          // Lecture on the dangers of Corruption
                    DefaultSkills.OneHanded   // Practice with a rapier
                ]
            },
            {
                TORCareers.WarriorPriest, [
                    DefaultSkills.OneHanded,  // Spar with the troops
                    DefaultSkills.TwoHanded,  // Train with a warhammer
                    DefaultSkills.Athletics,  // Help set up tents
                    DefaultSkills.Medicine,   // Assist the priestesses of Shallya
                    TORSkills.Faith           // Preach about Sigmar Heldenhammer
                ]
            },
            {
                TORCareers.WarriorPriestUlric, [
                    DefaultSkills.Scouting,   // Scour the wilds
                    DefaultSkills.TwoHanded,  // Train with a battleaxe
                    DefaultSkills.Athletics,  // Help with firewood
                    DefaultSkills.Leadership, // Inspire the troops to drive away weakness
                    TORSkills.Faith           // Preach the teachings of Ulric
                ]
            },
            {
                TORCareers.KnightOldWorld, [
                    DefaultSkills.OneHanded,  // Sparring practice with a sword
                    DefaultSkills.TwoHanded,  // Sparring practice with a greatsword
                    DefaultSkills.Polearm,    // Joust with other cavalrymen
                    DefaultSkills.Riding,     // Tend to your mount
                    DefaultSkills.Leadership  // Give battlefield advice to common troops
                ]
            },
            //Woodelves
            {
                TORCareers.Waywatcher, [
                    DefaultSkills.Bow,        // Target practice in solitude
                    DefaultSkills.Scouting,   // Climb trees and scout the area
                    DefaultSkills.Roguery,    // Cover the perimeter and set up traps
                    DefaultSkills.Athletics,  // Advanced sparring sessions
                    DefaultSkills.Medicine    // Gather medical herbs
                ]
            },
            {
                TORCareers.Spellsinger, [
                    TORSkills.Spellcraft,     // Commune with forest spirits
                    DefaultSkills.Riding,     // Converse with the stabled horses
                    TORSkills.Faith,          // Offer a prayer to Isha, the Mother
                    DefaultSkills.Charm,      // Discuss diplomatic matters with the commander
                    DefaultSkills.Medicine    // Gather medicinal herbs
                ]
            },
            {
                TORCareers.Warden, [// Glade Lord in-game
                    DefaultSkills.Polearm,    // Sparring practice with spears
                    DefaultSkills.Throwing,   // Javelin target practice
                    DefaultSkills.Bow,        // Bow target practice
                    DefaultSkills.Scouting,   // Scout the perimeter with your hawk
                    DefaultSkills.Leadership  // Discuss army movements with other nobles
                ]
            },
            //Eonir
            {
                TORCareers.GreyLord, [
                    TORSkills.Spellcraft,     // Study manuscripts
                    DefaultSkills.Steward,    // Engage in book keeping tasks
                    DefaultSkills.Leadership, // Hold speeches about the dragons of Caledor
                    DefaultSkills.Charm,      // Maintain Asurian etiquette
                    DefaultSkills.Medicine    // Experiment with alchemistic substances
                ]
            },

            //Dwarfs
            {
                TORCareers.Ironbreaker, [
                    DefaultSkills.OneHanded,  // Train with an az
                    DefaultSkills.Crafting,   // Maintain your armor and weapons
                    DefaultSkills.Scouting,   // Scout nearby tunnels
                    DefaultSkills.Athletics,  // Arm wrestle with other soldiers
                    TORSkills.GunPowder       // Help the army engineer with grenades
                ]
            },
            {
                TORCareers.Slayer, [
                    DefaultSkills.OneHanded,  // Train with an az
                    DefaultSkills.TwoHanded,  // Practice with a great az
                    DefaultSkills.Athletics,  // Patrol the camp perimeter
                    TORSkills.Faith,          // Repeat the words of your Slayer Oath
                    DefaultSkills.Medicine    // Drown your shame in ale
                ]
            },
            {
                TORCareers.Runelord, [
                    DefaultSkills.TwoHanded,   
                    TORSkills.Faith,          
                    DefaultSkills.Crafting,
                    DefaultSkills.OneHanded, 
                    DefaultSkills.Trade
                ]
            },

            //Orcs
            {
                TORCareers.OrcBoss, [
                    DefaultSkills.OneHanded,  // Smash fings wiv a smaller choppa
                    DefaultSkills.TwoHanded,  // Smash fings wiv da biggest choppa
                    DefaultSkills.Polearm,    // Smash fings wiv a long choppa
                    DefaultSkills.Athletics,  // Yell rude names at da Arrer Boys
                    DefaultSkills.Leadership  // Explain Gork an' Mork to sum Trolls
                ]
            },
            {
                TORCareers.OrcShaman, [
                    DefaultSkills.OneHanded,     // Smash fings wiv a clobba
                    TORSkills.Spellcraft,          // Dance an' kunnect to da Waaagh!!!
                    TORSkills.Faith,          // Huff sum smoke an' talk wiv da Godz
                    DefaultSkills.Medicine,   //  Cut sum gobbos open
                    DefaultSkills.Scouting, // Look fer sum 'shrooms in da caves
                ]
            },

        };

        foreach (var career in TORCareers.All)
        {
            if (!_activitySets.ContainsKey(career))
            {
                TORCommon.Log("ServeAsAHirelingActivities : Zerca, you forgot hireling activities for :" + career.StringId, NLog.LogLevel.Error);
                TORCommon.Say("Add the fooking hireling activites for " + career.Id + ", ya nincompooop.");
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