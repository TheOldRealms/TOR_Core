using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TOR_Core.CharacterDevelopment;
using TOR_Core.Extensions;
using TOR_Core.Ink;

namespace TOR_Core.CampaignMechanics.CustomEvents
{
    /// <summary>
    /// Campaign behavior that launches career-specific ink stories at the start of the game
    /// after character creation is complete.
    /// </summary>
    public class SimpleCareerQuestBehavior : CampaignBehaviorBase
    {
        private readonly Dictionary<string, string> _careerStories = new Dictionary<string, string>();
        
        private bool _hasShownCareerStory = false;

        public override void RegisterEvents()
        {
            if (!_hasShownCareerStory)
            {
                CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, HourlyTick);
            }
     
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionStart);
        }

        private void OnSessionStart(CampaignGameStarter starter)
        {
            InitializeCareerStories();
        }

        /// <summary>
        /// Initialize the dictionary of career IDs to story paths
        /// </summary>
        private void InitializeCareerStories()
        {
            _careerStories.Clear();

            // For now, only OrcBoss career has an initial story
            _careerStories.Add("OrcBoss", "OrcBossQuest1");
        }

        private void HourlyTick()
        {
            // Only check once and only if we haven't shown a career story yet
            if (_hasShownCareerStory)
                return;
            
            var id = Hero.MainHero.GetCareer()!=null ? Hero.MainHero.GetCareer().StringId : "";
            if(!_careerStories.ContainsKey(id))
                return;
            
            if (!StandardMovingCheck())
                return;
            
            if (TryLaunchCareerStory())
            {
                _hasShownCareerStory = true;
            }
        }

        private bool StandardMovingCheck()
        {
            return MobileParty.MainParty.IsMoving && 
                   MobileParty.MainParty.Army == null && 
                   !Hero.MainHero.IsPrisoner && 
                   MobileParty.MainParty.CurrentSettlement == null && 
                   MobileParty.MainParty.BesiegedSettlement == null;
        }
        
        private bool TryLaunchCareerStory()
        {
            if (Hero.MainHero == null)
                return false;

            var playerCareer = Hero.MainHero.GetCareer();
            if (playerCareer == null)
                return false;

            // Check if this career has an associated story
            if (_careerStories.TryGetValue(playerCareer.StringId, out string storyId))
            {
                InkStoryManager.OpenStory(storyId);
                return true;
            }

            return false;
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("_hasShownCareerStory", ref _hasShownCareerStory);
        }
    }
}