using System;
using TaleWorlds.SaveSystem;

namespace TOR_Core.AbilitySystem.Spells
{
    public enum SpellCastingLevel
    {
        [SaveableField(0)] None,
        [SaveableField(1)] Minor,
        [SaveableField(2)] Entry,
        [SaveableField(3)] Adept,
        [SaveableField(4)] Master
    }
    
    public enum PrayerLevel
    {
        [SaveableField(0)] None,
        [SaveableField(1)] Minor,
        [SaveableField(2)] Novice,
        [SaveableField(3)] Adept,
        [SaveableField(4)] Grand
    }
}
