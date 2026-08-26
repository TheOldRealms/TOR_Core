using TaleWorlds.Core;
using TOR_Core.Utilities;

namespace TOR_Core.AbilitySystem.SpellBook
{
    public class SpellBookState : GameState
    {
        public override bool IsMenuState => true;

        public bool IsTrainerMode { get; internal set; } = false;
        public string TrainerCulture { get; internal set; } = TORConstants.Cultures.EMPIRE;

        public SpellBookState() { }
    }
}