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
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignMechanics.CustomDialogs
{
    public class BloodKissSceneNotificationItem : SceneNotificationData
    {
        private Kingdom _sylvania; //Sly : this needs to be updated for the fact that bloodkissable lords are spread across 3 kingdoms initially, and a 4th is possible via Mousillon
        public override RelevantContextType RelevantContext => RelevantContextType.Map;

        public override TextObject TitleText => TORTextHelper.GetTextObject("tor_bloodkiss_player_notification", "Player recieves the Blood Kiss.");

        public override string SceneID => "scn_cutscene_bloodkiss";

        public override Banner[] GetBanners()
        {
            return
            [
                _sylvania.Banner,
                _sylvania.Banner
            ];
        }

        public override SceneNotificationCharacter[] GetSceneNotificationCharacters()
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
            return list.ToArray();
        }

        public BloodKissSceneNotificationItem()
        {
            _sylvania = Kingdom.All.FirstOrDefault(x => x.StringId == "sylvania");
            _sylvania ??= Kingdom.All.FirstOrDefault(x => x.StringId == "necrachs");
            _sylvania ??= Kingdom.All.FirstOrDefault(x => x.StringId == "blooddragons");
            _sylvania ??= Kingdom.All.FirstOrDefault(x => x.StringId == "mousillon");
            if (_sylvania == null)
            {
                _sylvania = Kingdom.All.FirstOrDefault();
                TORCommon.Log("BloodKissSceneNotificationItem : all vampire cultures are dead, using fallback kingdom for bloodkiss. How is a vampire even still alive to grant the bloodkiss?", NLog.LogLevel.Warn);
            }
        }
    }
}