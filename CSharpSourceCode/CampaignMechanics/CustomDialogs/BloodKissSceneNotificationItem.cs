using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.SceneInformationPopupTypes;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace TOR_Core.CampaignMechanics.CustomDialogs
{
    public class BloodKissSceneNotificationItem : SceneNotificationData
    {
        private Kingdom _sylvania;
        public override RelevantContextType RelevantContext => RelevantContextType.Map;

        public override TextObject TitleText => new TextObject("{=tor_bloodkiss_player_notification_str}Player recieves the Blood Kiss.");

        public override string SceneID => "scn_cutscene_bloodkiss";

        public override IEnumerable<Banner> GetBanners()
        {
            return new List<Banner>
            {
                _sylvania.Banner,
                _sylvania.Banner
            };
        }

        public override IEnumerable<SceneNotificationCharacter> GetSceneNotificationCharacters()
        {
            List<SceneNotificationCharacter> list = new List<SceneNotificationCharacter>();
            Hero leader = Hero.MainHero;
            Equipment overridenEquipment = leader.CivilianEquipment.Clone(false);
            CampaignSceneNotificationHelper.RemoveWeaponsFromEquipment(ref overridenEquipment, true, false);
            list.Add(CampaignSceneNotificationHelper.CreateNotificationCharacterFromHero(leader, overridenEquipment, false, default(BodyProperties), uint.MaxValue, uint.MaxValue, false));
            foreach (Hero hero in Enumerable.Take(CampaignSceneNotificationHelper.GetMilitaryAudienceForKingdom(_sylvania, true), 5))
            {
                Equipment overridenEquipment2 = hero.CivilianEquipment.Clone(false);
                CampaignSceneNotificationHelper.RemoveWeaponsFromEquipment(ref overridenEquipment2, true, false);
                list.Add(CampaignSceneNotificationHelper.CreateNotificationCharacterFromHero(hero, overridenEquipment2, false, default(BodyProperties), uint.MaxValue, uint.MaxValue, false));
            }
            return list;
        }

        public BloodKissSceneNotificationItem()
        {
            _sylvania = Kingdom.All.FirstOrDefault(x => x.StringId == "sylvania");
        }
    }
}
