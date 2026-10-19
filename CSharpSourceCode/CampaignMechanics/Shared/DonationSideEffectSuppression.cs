using System;

namespace TOR_Core.CampaignMechanics.Shared
{
    /// <summary>
    /// Shared flag read by Harmony patches that suppress unwanted side-effects
    /// of vanilla's "donate prisoners to town" flow when a TOR custom donation
    /// path (Eonir envoy, Witch Hunter Lodge, future donors) is using the
    /// vanilla screen as a UI vehicle but handling rewards itself.
    ///
    /// <para>Ref-counted so nested Begin/End pairs don't collide. Pair every
    /// <see cref="Begin"/> with an <see cref="End"/> in a <c>try/finally</c>
    /// to guarantee the flag clears even on exception paths.</para>
    /// </summary>
    public static class DonationSideEffectSuppression
    {
        private static int _depth;

        public static bool IsActive => _depth > 0;

        public static void Begin() => _depth++;
        public static void End()   => _depth = Math.Max(0, _depth - 1);
    }
}
