using System.Collections.Generic;
using System.Linq;
using System.Text;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignMechanics.Ambush
{
    public class AmbushCampaignBehavior : CampaignBehaviorBase
    {
        public const float StealthRadius = 15f;
        public const float StrikeRadius = 15f;
        public const int CooldownHours = 6;
        public const int MaxTargetSlots = 6;

        public static readonly InputKey StealthToggleKey = InputKey.X;

        public const string StealthMenuId = "tor_ambush_stealth_menu";

        private bool _playerInStealth;
        private CampaignTime _stealthEnteredAt = CampaignTime.Zero;
        private CampaignTime _playerAmbushCooldownUntil = CampaignTime.Zero;

        private readonly List<MobileParty> _currentTargets = new List<MobileParty>();
        private CampaignTimeControlMode _cachedTimeControlMode = CampaignTimeControlMode.StoppablePlay;

        public bool IsPlayerInStealth => _playerInStealth;
        public CampaignTime PlayerAmbushCooldownUntil => _playerAmbushCooldownUntil;

        public bool IsPartyInStealth(MobileParty party)
        {
            if (party == null) return false;
            if (party == MobileParty.MainParty) return _playerInStealth;
            return false;
        }

        public void EnterStealth(MobileParty party)
        {
            if (party != MobileParty.MainParty) return;
            if (_playerInStealth) return;

            _playerInStealth = true;
            _stealthEnteredAt = CampaignTime.Now;

            party.Ai.SetDoNotMakeNewDecisions(true);
            party.SetMoveModeHold();

            _cachedTimeControlMode = Campaign.Current.TimeControlMode;
            Campaign.Current.TimeControlMode = CampaignTimeControlMode.Stop;

            TORCommon.Say("Entered stealth stance.");
            GameMenu.ActivateGameMenu(StealthMenuId);
        }

        public void LeaveStealth(MobileParty party)
        {
            if (party != MobileParty.MainParty) return;
            if (!_playerInStealth) return;

            _playerInStealth = false;
            party.Ai.SetDoNotMakeNewDecisions(false);
            party.Ai.RethinkAtNextHourlyTick = true;
            _currentTargets.Clear();

            Campaign.Current.TimeControlMode = _cachedTimeControlMode;

            TryCloseStealthMenu();
            TORCommon.Say("Left stealth stance.");
        }

        private static void TryCloseStealthMenu()
        {
            var ctx = Campaign.Current?.CurrentMenuContext;
            if (ctx != null && ctx.GameMenu?.StringId == StealthMenuId)
                GameMenu.ExitToLast();
        }

        public IReadOnlyList<MobileParty> GetAmbushTargets(MobileParty attacker)
        {
            if (attacker == null) return new List<MobileParty>();
            var attackerPos = attacker.Position.ToVec2();
            var radiusSq = StealthRadius * StealthRadius;
            var attackerFaction = attacker.MapFaction;

            return MobileParty.All
                .Where(p => p != null
                            && p.IsActive
                            && p != attacker
                            && p.MapFaction != null
                            && attackerFaction != null
                            && FactionManager.IsAtWarAgainstFaction(attackerFaction, p.MapFaction)
                            && p.Position.ToVec2().DistanceSquared(attackerPos) <= radiusSq)
                .OrderBy(p => p.Position.ToVec2().DistanceSquared(attackerPos))
                .Take(MaxTargetSlots)
                .ToList();
        }

        public void StampAmbushCooldown()
        {
            _playerAmbushCooldownUntil = CampaignTime.HoursFromNow(CooldownHours);
        }

        public bool IsOnAmbushCooldown => _playerAmbushCooldownUntil.IsFuture;

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
            CampaignEvents.TickEvent.AddNonSerializedListener(this, OnTick);
            CampaignEvents.OnPlayerBattleEndEvent.AddNonSerializedListener(this, OnPlayerBattleEnd);
        }

        private void OnPlayerBattleEnd(MapEvent mapEvent)
        {
            if (_playerInStealth) LeaveStealth(MobileParty.MainParty);
        }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            RegisterMenus(starter);

            // Defensive: a save mid-stealth loads with the flag set but no menu / no movement lock.
            // Clear the flag so the player resumes with a normal map state. Phase A choice: preserving stealth
            // across saves is not worth the menu-reopen complexity until the detection model is in (Phase B).
            if (_playerInStealth)
            {
                _playerInStealth = false;
                _currentTargets.Clear();
                MobileParty.MainParty?.Ai?.SetDoNotMakeNewDecisions(false);
            }
        }

        private void RegisterMenus(CampaignGameStarter starter)
        {
            starter.AddWaitGameMenu(
                StealthMenuId,
                "{STEALTH_MENU_TEXT}",
                OnStealthMenuInit,
                _ => true,
                null,
                (args, dt) => { },
                GameMenu.MenuAndOptionType.WaitMenuHideProgressAndHoursOption,
                GameMenu.MenuOverlayType.Encounter,
                targetWaitHours: 0f,
                GameMenu.MenuFlags.None,
                null);

            for (int i = 0; i < MaxTargetSlots; i++)
            {
                int slot = i;
                starter.AddGameMenuOption(
                    StealthMenuId,
                    "tor_ambush_strike_" + slot,
                    "{STRIKE_OPT_TEXT}",
                    args => StrikeSlotCondition(args, slot),
                    args => OnStrike(slot),
                    isLeave: false,
                    index: slot,
                    isRepeatable: false);
            }

            starter.AddGameMenuOption(
                StealthMenuId,
                "tor_ambush_leave",
                "{=tor_ambush_leave}Break cover and move on.",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Leave;
                    return true;
                },
                _ => LeaveStealthViaMenu(),
                isLeave: true,
                index: -1,
                isRepeatable: false);
        }

        private void OnStealthMenuInit(MenuCallbackArgs args)
        {
            args.MenuContext?.GameMenu?.StartWait();

            var culture = Hero.MainHero?.MapFaction?.Culture;
            if (culture != null && args.MenuContext != null)
                args.MenuContext.SetBackgroundMeshName(culture.EncounterBackgroundMesh);

            _currentTargets.Clear();
            _currentTargets.AddRange(GetAmbushTargets(MobileParty.MainParty));

            var intro = new TextObject("{=tor_ambush_menu_intro}You crouch in cover. Nothing moves nearby.");
            if (_currentTargets.Count > 0)
            {
                var sb = new StringBuilder();
                sb.AppendLine("You crouch in cover. Enemies within strike range:");
                for (int i = 0; i < _currentTargets.Count; i++)
                {
                    var p = _currentTargets[i];
                    sb.AppendLine("- " + GetPartyLabel(p) + " (" + p.MemberRoster.TotalManCount + ")");
                }
                intro = new TextObject(sb.ToString());
            }
            MBTextManager.SetTextVariable("STEALTH_MENU_TEXT", intro);
        }

        private bool StrikeSlotCondition(MenuCallbackArgs args, int slot)
        {
            if (slot >= _currentTargets.Count) return false;
            var target = _currentTargets[slot];
            if (target == null || !target.IsActive) return false;
            args.Text = new TextObject("Strike at " + GetPartyLabel(target) + ".");
            args.optionLeaveType = GameMenuOption.LeaveType.HostileAction;
            return true;
        }

        private void OnStrike(int slot)
        {
            if (slot >= _currentTargets.Count) return;
            var target = _currentTargets[slot];
            if (target == null || !target.IsActive) return;

            TORCommon.Say("Would ambush " + GetPartyLabel(target) + " — rolling as a regular battle (Phase C adds the success roll).");

            // Drop stealth before combat; cooldown fires regardless of outcome.
            LeaveStealth(MobileParty.MainParty);
            StampAmbushCooldown();

            EncounterManager.StartPartyEncounter(PartyBase.MainParty, target.Party);
        }

        private void LeaveStealthViaMenu()
        {
            // LeaveStealth closes the stealth menu via TryCloseStealthMenu — no second ExitToLast here,
            // calling it twice leaves the menu context in a half-open state that blocks re-activation.
            LeaveStealth(MobileParty.MainParty);
        }

        private static string GetPartyLabel(MobileParty p)
        {
            if (p.LeaderHero != null) return p.LeaderHero.Name.ToString();
            return p.Name.ToString();
        }

        private void OnTick(float dt)
        {
            if (!CanToggleStealth()) return;
            if (!Input.IsKeyReleased(StealthToggleKey)) return;

            if (_playerInStealth)
                LeaveStealth(MobileParty.MainParty);
            else if (!IsOnAmbushCooldown)
                EnterStealth(MobileParty.MainParty);
            else
                TORCommon.Say("Stealth is still on cooldown.");
        }

        private bool CanToggleStealth()
        {
            if (Mission.Current != null) return false;
            if (Hero.MainHero == null || Hero.MainHero.IsPrisoner) return false;
            if (MobileParty.MainParty == null || !MobileParty.MainParty.IsActive) return false;
            if (MobileParty.MainParty.CurrentSettlement != null) return false;
            if (MapEvent.PlayerMapEvent != null) return false;

            // While another menu is still on screen (ours is tracked via _playerInStealth),
            // activating a new menu via ActivateGameMenu silently fails — leaving the hotkey press
            // "eaten" with no visible effect until something else refreshes the UI.
            var ctx = Campaign.Current?.CurrentMenuContext;
            if (ctx != null && !_playerInStealth) return false;

            return true;
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("_playerInStealth", ref _playerInStealth);
            dataStore.SyncData("_stealthEnteredAt", ref _stealthEnteredAt);
            dataStore.SyncData("_playerAmbushCooldownUntil", ref _playerAmbushCooldownUntil);
        }
    }
}
