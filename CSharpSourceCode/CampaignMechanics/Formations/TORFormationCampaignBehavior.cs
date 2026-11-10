using System.Collections.Generic;
using TaleWorlds.CampaignSystem;

namespace TOR_Core.CampaignMechanics.Formations
{
    public class TORFormationCampaignBehavior : CampaignBehaviorBase
    {
        // separate captain references are kept for different battle types
        private Dictionary<int, Hero> _formationCaptains = new();

        public override void RegisterEvents()
        {
            CampaignEvents.OnHeroUnregisteredEvent.AddNonSerializedListener(this, OnHeroUnregistered);
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("tor_formation_captains", ref _formationCaptains);
        }

        public Hero GetCaptain(int formationIndex)
        {
            return _formationCaptains.TryGetValue(formationIndex, out var captain) ? captain : null;
        }

        public void SetCaptain(int formationIndex, Hero captain)
        {
            foreach (var previousFormation in new List<int>(_formationCaptains.Keys))
            {
                if (previousFormation != formationIndex && _formationCaptains[previousFormation] == captain)
                {
                    _formationCaptains.Remove(previousFormation);
                }
            }
            _formationCaptains[formationIndex] = captain;
        }

        public void ClearCaptain(int formationIndex)
        {
            _formationCaptains.Remove(formationIndex);
        }

        private void OnHeroUnregistered(Hero hero)
        {
            for (var i = 0; i < 8; i++)
            {
                if (GetCaptain(i) == hero)
                {
                    _formationCaptains.Remove(i);
                }
            }
        }
    }
}