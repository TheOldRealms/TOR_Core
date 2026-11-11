using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.MountAndBlade;

namespace TOR_Core.Missions
{
    /// <summary>
    /// Mission controller for POI "visit scene" missions launched outside the vanilla
    /// LocationEncounter flow (shrines, Oak of Ages, custom raiding sites, …).
    ///
    /// Scope: player spawn + leave handling only. NPC placement is composed in via
    /// separate behaviors like <see cref="PoiCivilianAgentSpawner"/> so each
    /// concern stays single-responsibility and future POIs can mix spawners freely.
    /// </summary>
    public class PoiVisitMissionController : MissionLogic
    {
        private readonly string _returnMenuId;

        public PoiVisitMissionController(string returnMenuId)
        {
            _returnMenuId = returnMenuId;
        }

        public override void EarlyStart()
        {
            base.EarlyStart();
            // Without a LocationEncounter driving entry, vanilla doesn't auto-spawn the
            // player — the agent ends up "unconscious". Explicitly spawn as civilian.
            var agentHandler = Mission.GetMissionBehavior<TORMissionAgentHandler>();
            agentHandler?.SpawnPlayer(civilianEquipment: true, noWeapon: true);
        }

        protected override void OnEndMission()
        {
            base.OnEndMission();
            // Route back to the parent menu so vanilla doesn't fall through to
            // game_menu_town_on_init (which NREs on any non-town settlement).
            if (!string.IsNullOrEmpty(_returnMenuId))
            {
                GameMenu.ActivateGameMenu(_returnMenuId);
            }
        }
    }
}
