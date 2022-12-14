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

    public class SpellCastingTypeDefiner : SaveableTypeDefiner
    {
        public SpellCastingTypeDefiner() : base(1_143_199) { }
        protected override void DefineEnumTypes()
        {
            AddEnumDefinition(typeof(SpellCastingLevel), 1);
        }
    }
}
