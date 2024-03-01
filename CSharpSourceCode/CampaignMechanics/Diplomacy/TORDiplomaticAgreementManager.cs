using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;

using TaleWorlds.CampaignSystem;
using TaleWorlds.SaveSystem;
using TOR_Core.CampaignMechanics.Diplomacy;
using TOR_Core.CampaignMechanics.Diplomacy.Mechanics.Actions;

namespace Diplomacy.DiplomaticAction
{
    class TORDiplomaticAgreementManager
    {
        private static readonly Lazy<TORDiplomaticAgreementManager> lazy = new Lazy<TORDiplomaticAgreementManager>(() => new TORDiplomaticAgreementManager());
        public static TORDiplomaticAgreementManager Instance { get => lazy.Value; }

        [SaveableField(1)]
        [UsedImplicitly]
        private Dictionary<FactionPair, List<DiplomaticAgreement>> _agreements;

        public TORDiplomaticAgreementManager()
        {
            _agreements = new Dictionary<FactionPair, List<DiplomaticAgreement>>();
        }

        public Dictionary<FactionPair, List<DiplomaticAgreement>> Agreements => _agreements;

        public static void RegisterAgreement(Kingdom kingdom, Kingdom otherKingdom, DiplomaticAgreement diplomaticAgreement)
        {
            var factionMapping = new FactionPair(kingdom, otherKingdom);
            if (Instance.Agreements.TryGetValue(factionMapping, out var agreements))
            {
                agreements.Add(diplomaticAgreement);
            }
            else
            {
                Instance.Agreements[factionMapping] = new List<DiplomaticAgreement> { diplomaticAgreement };
            }
        }
    }
}