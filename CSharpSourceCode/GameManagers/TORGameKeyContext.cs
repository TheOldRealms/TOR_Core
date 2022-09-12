using TaleWorlds.InputSystem;

namespace TOR_Core.GameManagers
{
    public class TORGameKeyContext : GameKeyContext
    {
        public const string SpellCasterModeVariable = "SpellCastingMode";

        public TORGameKeyContext() :  base("TOR", 0, GameKeyContext.GameKeyContextType.AuxiliarySerializedAndShownInOptions)
        {
            this.RegisterTorHotKey(nameof (SpellCasterModeVariable), InputKey.Q, HotKey.Modifiers.None);
        }
        
        private void RegisterTorHotKey(
            string id,
            InputKey hotkeyKey,
            HotKey.Modifiers modifiers,
            HotKey.Modifiers negativeModifiers = HotKey.Modifiers.None)
        {
            this.RegisterHotKey(new HotKey(id, "TOR", hotkeyKey, modifiers, negativeModifiers));
        }
    }
}