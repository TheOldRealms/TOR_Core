using psai.net;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignMechanics
{
    public class TORCampaignMusicHandler : IMusicHandler
    {
        private const float MinRestDurationInSeconds = 30f;
        private const float MaxRestDurationInSeconds = 60f;
        private float _restTimer;
        private const int ElvesThemeId = 502;
        private const int VampireThemeId = 503;
        private const int BretonniaThemeId = 504;
        private const int EmpireThemeId = 505;

        public bool IsPausable => false;

        private TORCampaignMusicHandler() { }

        public static void Create()
        {
            TORCampaignMusicHandler campaignMusicHandler = new TORCampaignMusicHandler();
            MBMusicManager.Current.OnCampaignMusicHandlerInit(campaignMusicHandler);
        }

        public void OnUpdated(float dt)
        {
            CheckMusicMode();
            TickCampaignMusic(dt);
        }

        private void TickCampaignMusic(float dt)
        {
            bool isPlaying = PsaiCore.Instance.GetPsaiInfo().psaiState == PsaiState.playing;
            if (_restTimer <= 0f)
            {
                _restTimer += dt;
                if (_restTimer > 0f)
                {
                    StartTheme(GetThemeId());
                    return;
                }
            }
            else if (!isPlaying)
            {
                MBMusicManager.Current.ForceStopThemeWithFadeOut();
                _restTimer = -(MinRestDurationInSeconds + (MBRandom.RandomFloat * MaxRestDurationInSeconds));
            }
        }

        private void StartTheme(int themeId)
        {
            PsaiCore.Instance.HoldCurrentIntensity(true);
            PsaiCore.Instance.TriggerMusicTheme(themeId, 0f);
        }

        private int GetThemeId()
        {
            var culture = GetNearbyCulture();
            return culture.StringId switch
            {
                TORConstants.Cultures.ASRAI => ElvesThemeId,
                TORConstants.Cultures.EONIR => ElvesThemeId,
                TORConstants.Cultures.SYLVANIA => VampireThemeId,
                TORConstants.Cultures.BRETONNIA => BretonniaThemeId,
                TORConstants.Cultures.EMPIRE => EmpireThemeId,
                _ => EmpireThemeId,
            };
        }

        private void CheckMusicMode()
        {
            if (MBMusicManager.Current.CurrentMode != MusicMode.Campaign)
            {
                MBMusicManager.Current.ActivateCampaignMode();
            }
        }

        private CultureObject GetNearbyCulture()
        {
            CultureObject cultureObject = null;
            float num = float.MaxValue;
            
            var settlement = TORCommon.FindNearestSettlement(MobileParty.MainParty, Campaign.Current.GetAverageDistanceBetweenClosestTwoTownsWithNavigationType(MobileParty.NavigationType.Default));//Sly : average distance was ~60 when last observed.

            cultureObject = settlement.Culture;

            //foreach (Settlement settlement in Campaign.Current.Settlements)
            //{
            //    if (settlement.IsTown || settlement.IsVillage)
            //    {
            //        float num2 = settlement.Position.DistanceSquared(MobileParty.MainParty.Position);
            //        if (settlement.IsVillage)
            //        {
            //            num2 *= 1.05f;
            //        }
            //        if (num > num2)
            //        {
            //            cultureObject = settlement.Culture;
            //            num = num2;
            //        }
            //    }
            //}
            return cultureObject;
        }
    }
}
