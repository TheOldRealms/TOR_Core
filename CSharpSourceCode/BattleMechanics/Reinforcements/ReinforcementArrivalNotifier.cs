using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;
using TaleWorlds.Localization;
using TOR_Core.Audio;
using TOR_Core.Utilities;

namespace TOR_Core.BattleMechanics.Reinforcements
{
    /// <summary>
    /// keeps dialogue arrivals synced to their quick info banners
    /// </summary>
    internal static class ReinforcementArrivalNotifier
    {
        private static GameNotificationVM _notificationVm;
        private static ArrivalLine? _activeArrivalLine;
        private static TORModuleSound _activeArrivalSound;
        private static float _arrivalElapsed;
        private static bool _arrivalStarted;
        private static bool _introFinished;
        private static bool _notificationFinished;

        private enum ArrivalSpeaker
        {
            Lord,
            LordTroops,
            ReinforcedSideTroops,
            OpposingSideTroops
        }

        private readonly struct ArrivalLine
        {
            public string TextId { get; }
            public ArrivalSpeaker Speaker { get; }
            public string ModuleSound { get; }
            public bool IsLine { get; }
            public bool IsHorn => !IsLine;
            public string IntroText { get; }
            public ArrivalSpeaker? IntroSpeaker { get; }
            public float IntroDuration { get; }
            public float SoundDuration { get; }
            public float LineEndOffset { get; }

            public ArrivalLine(
                string textId,
                ArrivalSpeaker speaker,
                string moduleSound = null,
                bool isLine = false,
                string introText = null,
                ArrivalSpeaker? introSpeaker = null,
                float introDuration = 0f,
                float soundDuration = 0f,
                float lineEndOffset = 1.2f)
            {
                TextId = textId;
                Speaker = speaker;
                ModuleSound = moduleSound;
                IsLine = isLine;
                IntroText = introText;
                IntroSpeaker = introSpeaker;
                IntroDuration = introDuration;
                SoundDuration = soundDuration;
                LineEndOffset = lineEndOffset;
            }
        }

        private readonly struct CultureArrivalLine
        {
            public string TextId { get; }
            public IReadOnlyList<string> SoundEvents { get; }

            public CultureArrivalLine(string textId, params string[] soundEvents)
            {
                TextId = textId;
                SoundEvents = soundEvents;
            }
        }

        private sealed class ArrivalPool
        {
            public IReadOnlyList<ArrivalLine> Lines { get; }
            public IReadOnlyList<string> SoundEvents { get; }

            public ArrivalPool(IReadOnlyList<ArrivalLine> lines, IReadOnlyList<string> soundEvents)
            {
                Lines = lines;
                SoundEvents = soundEvents;
            }
        }

        public static void ShowGreenKnightCheat(CharacterObject lostSon, bool reinforcesPlayer)
        {
            ShowGreenKnight(lostSon, reinforcesPlayer);
        }

        public static void ShowGreenKnight(CharacterObject lostSon, bool reinforcesPlayer)
        {
            var battle = MapEvent.PlayerMapEvent;
            var opposingSide = reinforcesPlayer ? battle.PlayerSide.GetOppositeSide() : battle.PlayerSide;
            var opposingSideTroop = GetRandomTroop(battle.GetMapEventSide(opposingSide).Parties);
            var greenKnightPool = GetSpecialArrivalPool("tor_br_greenknight_mission");
            var arrivalLine = greenKnightPool.Lines.First(x => x.TextId == "tor_reinforcement_green_knight_cheat");
            var introSpeaker = arrivalLine.IntroSpeaker == ArrivalSpeaker.OpposingSideTroops ? opposingSideTroop : lostSon;

            if (TryStartLineArrival(arrivalLine, lostSon, introSpeaker))
                return;

            MBInformationManager.AddQuickInformation(GameTexts.FindText(arrivalLine.TextId), 0, lostSon);
            TORAudioManager.CreateSoundInstance(arrivalLine.ModuleSound, false)?.Play();
        }

        private static bool TryStartLineArrival(ArrivalLine arrivalLine, CharacterObject speaker, CharacterObject introSpeaker)
        {
            if (!arrivalLine.IsLine || arrivalLine.ModuleSound == null || arrivalLine.SoundDuration <= 0f)
                return false;

            var arrivalSound = TORAudioManager.CreateSoundInstance(arrivalLine.ModuleSound, false);
            if (arrivalSound == null)
                return false;

            _activeArrivalSound?.Remove();
            _activeArrivalLine = arrivalLine;
            _activeArrivalSound = arrivalSound;
            _arrivalElapsed = 0f;
            _arrivalStarted = false;
            _introFinished = arrivalLine.IntroText == null;
            _notificationFinished = false;

            var notificationHoldMs = (int)System.Math.Ceiling(arrivalLine.SoundDuration * 1000f);
            if (arrivalLine.IntroText != null)
                MBInformationManager.AddQuickInformation(new TextObject(arrivalLine.IntroText), notificationHoldMs, introSpeaker);

            MBInformationManager.AddQuickInformation(GameTexts.FindText(arrivalLine.TextId), notificationHoldMs, speaker);
            return true;
        }

        public static void TickArrivalNotification(float dt)
        {
            if (_activeArrivalLine == null || _activeArrivalSound == null || _notificationVm == null)
                return;

            var arrivalLine = _activeArrivalLine.Value;
            var arrivalText = GameTexts.FindText(arrivalLine.TextId).ToString();
            var openingText = arrivalLine.IntroText ?? arrivalText;

            if (!_arrivalStarted)
            {
                if (!_notificationVm.GotNotification || _notificationVm.CurrentNotification.GameNotificationText != openingText)
                    return;

                if (!_activeArrivalSound.Play())
                {
                    _notificationVm.SkipCurrentNotification();
                    ResetArrivalNotification();
                    return;
                }

                _arrivalStarted = true;
                return;
            }

            _arrivalElapsed += dt;
            if (!_introFinished)
            {
                if (_arrivalElapsed < arrivalLine.IntroDuration)
                    return;

                if (_notificationVm.GotNotification && _notificationVm.CurrentNotification.GameNotificationText == arrivalLine.IntroText)
                    _notificationVm.SkipCurrentNotification();

                _introFinished = true;
                return;
            }

            var notificationEndTime = System.Math.Max(arrivalLine.IntroDuration, arrivalLine.SoundDuration - arrivalLine.LineEndOffset);
            if (!_notificationFinished && _arrivalElapsed >= notificationEndTime)
            {
                if (_notificationVm.GotNotification && _notificationVm.CurrentNotification.GameNotificationText == arrivalText)
                {
                    _notificationVm.SkipCurrentNotification();
                    _notificationFinished = true;
                }
            }

            if (_activeArrivalSound.IsPlaybackRequested)
                return;

            if (_notificationVm.GotNotification &&
                (_notificationVm.CurrentNotification.GameNotificationText == arrivalText || _notificationVm.CurrentNotification.GameNotificationText == arrivalLine.IntroText))
            {
                _notificationVm.SkipCurrentNotification();
            }

            ClearArrivalNotification();
        }

        public static void ResetArrivalNotification()
        {
            _activeArrivalSound?.Remove();
            ClearArrivalNotification();
        }

        private static void ClearArrivalNotification()
        {
            _activeArrivalLine = null;
            _activeArrivalSound = null;
            _arrivalElapsed = 0f;
            _arrivalStarted = false;
            _introFinished = false;
            _notificationFinished = false;
        }

        internal static bool IsArrivalNotification(string notificationText)
        {
            if (_activeArrivalLine == null)
                return false;

            var arrivalLine = _activeArrivalLine.Value;
            return notificationText == arrivalLine.IntroText || notificationText == GameTexts.FindText(arrivalLine.TextId).ToString();
        }

        internal static void BindNotificationVm(GameNotificationVM notificationVm)
        {
            _notificationVm = notificationVm;
        }

        public static void Show(IReadOnlyList<MapEventParty> joinedParties, bool reinforcesPlayer)
        {
            if (TryShowSpecialArrival(joinedParties, reinforcesPlayer))
                return;

            ShowCultureArrival(reinforcesPlayer);
        }

        private static bool TryShowSpecialArrival(IReadOnlyList<MapEventParty> joinedParties, bool reinforcesPlayer)
        {
            // army reinforcements may include a named lord in an attached party
            foreach (var joinedParty in joinedParties)
            {
                var leader = joinedParty.Party.MobileParty.LeaderHero;
                if (leader == null)
                    continue;

                var arrivalPool = GetSpecialArrivalPool(leader.StringId);
                if (arrivalPool == null)
                    continue;

                var battle = MapEvent.PlayerMapEvent;
                var reinforcedSide = reinforcesPlayer ? battle.PlayerSide : battle.PlayerSide.GetOppositeSide();
                var existingSideParties = battle.GetMapEventSide(reinforcedSide).Parties.Where(x => !joinedParties.Contains(x));
                var lordTroop = GetRandomTroop(new[] { joinedParty });
                var reinforcedSideTroop = GetRandomTroop(existingSideParties);
                var opposingSideTroop = GetRandomTroop(battle.GetMapEventSide(reinforcedSide.GetOppositeSide()).Parties);

                if (TryShowArrival(arrivalPool, leader.CharacterObject, lordTroop, reinforcedSideTroop, opposingSideTroop))
                    return true;
            }

            return false;
        }

        private static bool TryShowArrival(ArrivalPool arrivalPool, CharacterObject lord, CharacterObject lordTroop, CharacterObject reinforcedSideTroop, CharacterObject opposingSideTroop = null)
        {
            var candidates = new List<(ArrivalLine Line, CharacterObject Speaker, CharacterObject IntroSpeaker)>();
            foreach (var line in arrivalPool.Lines)
            {
                if (line.TextId == null)
                    continue;

                var speaker = line.Speaker switch
                {
                    ArrivalSpeaker.Lord => lord,
                    ArrivalSpeaker.LordTroops => lordTroop,
                    ArrivalSpeaker.ReinforcedSideTroops => reinforcedSideTroop,
                    ArrivalSpeaker.OpposingSideTroops => opposingSideTroop,
                    _ => null
                };

                var introSpeaker = line.IntroSpeaker switch
                {
                    ArrivalSpeaker.Lord => lord,
                    ArrivalSpeaker.LordTroops => lordTroop,
                    ArrivalSpeaker.ReinforcedSideTroops => reinforcedSideTroop,
                    ArrivalSpeaker.OpposingSideTroops => opposingSideTroop,
                    _ => speaker
                };

                if (speaker != null)
                    candidates.Add((line, speaker, introSpeaker));
            }

            if (candidates.Count == 0)
                return false;

            var selected = candidates[MBRandom.RandomInt(candidates.Count)];
            if (selected.Line.IsLine)
            {
                if (TryStartLineArrival(selected.Line, selected.Speaker, selected.IntroSpeaker))
                    return true;

                MBInformationManager.AddQuickInformation(GameTexts.FindText(selected.Line.TextId), 0, selected.Speaker);
                TORAudioManager.CreateSoundInstance(selected.Line.ModuleSound, false)?.Play();
                return true;
            }

            MBInformationManager.AddQuickInformation(GameTexts.FindText(selected.Line.TextId), 0, selected.Speaker);
            if (selected.Line.IsHorn)
                PlayArrivalSound(arrivalPool.SoundEvents);

            return true;
        }

        private static void ShowCultureArrival(bool reinforcesPlayer)
        {
            var battle = MapEvent.PlayerMapEvent;
            var playerCulture = Hero.MainHero.Culture.StringId;

            // enemy arrivals can be heard from either side
            if (!reinforcesPlayer && MBRandom.RandomInt(2) == 1)
            {
                var reinforcedSide = battle.PlayerSide.GetOppositeSide();
                var reinforcedCulture = battle.GetLeaderParty(reinforcedSide).Culture.StringId;
                var alliedArrivalPool = GetCultureArrivalPool(reinforcedCulture, true);
                var alliedArrival = alliedArrivalPool[MBRandom.RandomInt(alliedArrivalPool.Count)];
                var reinforcedSideTroop = GetRandomTroop(battle.GetMapEventSide(reinforcedSide).Parties);

                MBInformationManager.AddQuickInformation(GameTexts.FindText(alliedArrival.TextId), 0, reinforcedSideTroop);
                PlayArrivalSound(alliedArrival.SoundEvents);
                return;
            }

            var arrivalPool = GetCultureArrivalPool(playerCulture, reinforcesPlayer);
            var arrival = arrivalPool[MBRandom.RandomInt(arrivalPool.Count)];
            var playerTroop = GetRandomTroop(battle.GetMapEventSide(battle.PlayerSide).Parties);

            MBInformationManager.AddQuickInformation(GameTexts.FindText(arrival.TextId), 0, playerTroop);
            PlayArrivalSound(arrival.SoundEvents);
        }

        private static ArrivalPool GetSpecialArrivalPool(string characterId)
        {
            switch (characterId)
            {
                case "tor_dw_lord_karak_kadrin_factionleader":
                    return new ArrivalPool(
                        new[]
                        {
                            new ArrivalLine("tor_reinforcement_ungrim_lord_1", ArrivalSpeaker.Lord),
                            new ArrivalLine("tor_reinforcement_ungrim_troops_1", ArrivalSpeaker.LordTroops),
                            new ArrivalLine("tor_reinforcement_ungrim_reinforced_side_1", ArrivalSpeaker.ReinforcedSideTroops)
                        },
                        new[] { "event:/ui/mission/horns/attack" });
                case "tor_greenskin_lord_neck_snappers_factionleader":
                    return new ArrivalPool(
                        new[]
                        {
                            new ArrivalLine("tor_reinforcement_morglum_lord_1", ArrivalSpeaker.Lord),
                            new ArrivalLine("tor_reinforcement_morglum_troops_1", ArrivalSpeaker.LordTroops),
                            new ArrivalLine("tor_reinforcement_morglum_reinforced_side_1", ArrivalSpeaker.ReinforcedSideTroops)
                        },
                        new[] { "event:/ui/mission/horns/attack" });
                case "tor_br_lord_couronne_factionleader":
                    return new ArrivalPool(
                        new[]
                        {
                            new ArrivalLine("tor_reinforcement_louen_lord_1", ArrivalSpeaker.Lord),
                            new ArrivalLine("tor_reinforcement_louen_troops_1", ArrivalSpeaker.LordTroops),
                            new ArrivalLine("tor_reinforcement_louen_reinforced_side_1", ArrivalSpeaker.ReinforcedSideTroops)
                        },
                        new[] { "event:/ui/mission/horns/attack" });
                case "tor_br_greenknight_mission":  // example of a line with a sound and an intro text
                    return new ArrivalPool(
                        new[]
                        {
                            new ArrivalLine("tor_reinforcement_green_knight_lord_1", ArrivalSpeaker.Lord),
                            new ArrivalLine("tor_reinforcement_green_knight_troops_1", ArrivalSpeaker.LordTroops),
                            new ArrivalLine(
                                "tor_reinforcement_green_knight_cheat",
                                ArrivalSpeaker.LordTroops,
                                "ReinforcementNoises/tor_bret_greenknight_01",
                                isLine: true,
                                introText: "??!?!?",
                                introSpeaker: ArrivalSpeaker.OpposingSideTroops,
                                introDuration: 2.9f,
                                soundDuration: 10f,
                                lineEndOffset: 1.2f),
                            new ArrivalLine("tor_reinforcement_green_knight_reinforced_side_1", ArrivalSpeaker.ReinforcedSideTroops),
                            new ArrivalLine("tor_reinforcement_beastmen_green_knight_enemy", ArrivalSpeaker.OpposingSideTroops)
                        },
                        new[] { "event:/ui/mission/horns/attack" });
                case "tor_emp_lord_reikland_factionleader":
                    return new ArrivalPool(
                        new[]
                        {
                            new ArrivalLine("tor_reinforcement_karl_franz_lord_1", ArrivalSpeaker.Lord),
                            new ArrivalLine("tor_reinforcement_karl_franz_troops_1", ArrivalSpeaker.LordTroops),
                            new ArrivalLine("tor_reinforcement_karl_franz_reinforced_side_1", ArrivalSpeaker.ReinforcedSideTroops)
                        },
                        new[] { "event:/ui/mission/horns/attack" });
                case "tor_eo_lord_laurelorn_factionleader":
                    return new ArrivalPool(
                        new[]
                        {
                            new ArrivalLine("tor_reinforcement_marrisith_lord_1", ArrivalSpeaker.Lord),
                            new ArrivalLine("tor_reinforcement_marrisith_troops_1", ArrivalSpeaker.LordTroops),
                            new ArrivalLine("tor_reinforcement_marrisith_reinforced_side_1", ArrivalSpeaker.ReinforcedSideTroops)
                        },
                        new[] { "event:/ui/mission/horns/move" });
                case "tor_we_orion_factionleader":
                    return new ArrivalPool(
                        new[]
                        {
                            new ArrivalLine("tor_reinforcement_orion_lord_1", ArrivalSpeaker.Lord),
                            new ArrivalLine("tor_reinforcement_orion_troops_1", ArrivalSpeaker.LordTroops),
                            new ArrivalLine("tor_reinforcement_orion_reinforced_side_1", ArrivalSpeaker.ReinforcedSideTroops)
                        },
                        new[] { "event:/ui/mission/horns/attack" });
                case "tor_we_lord_athel_loren_factionleader":
                    return new ArrivalPool(
                        new[]
                        {
                            new ArrivalLine("tor_reinforcement_araloth_lord_1", ArrivalSpeaker.Lord),
                            new ArrivalLine("tor_reinforcement_araloth_troops_1", ArrivalSpeaker.LordTroops),
                            new ArrivalLine("tor_reinforcement_araloth_reinforced_side_1", ArrivalSpeaker.ReinforcedSideTroops)
                        },
                        new[] { "event:/ui/mission/horns/move" });
                case "tor_chaos_lord_brasskeep_factionleader":
                    return new ArrivalPool(
                        new[]
                        {
                            new ArrivalLine("tor_reinforcement_pyucius_lord_1", ArrivalSpeaker.Lord),
                            new ArrivalLine("tor_reinforcement_pyucius_troops_1", ArrivalSpeaker.LordTroops),
                            new ArrivalLine("tor_reinforcement_pyucius_reinforced_side_1", ArrivalSpeaker.ReinforcedSideTroops)
                        },
                        new[] { "event:/ui/mission/horns/attack" });
                case "tor_m_lord_mousillon_factionleader":
                    return new ArrivalPool(
                        new[]
                        {
                            new ArrivalLine("tor_reinforcement_serpent_lord_1", ArrivalSpeaker.Lord),
                            new ArrivalLine("tor_reinforcement_serpent_troops_1", ArrivalSpeaker.LordTroops),
                            new ArrivalLine("tor_reinforcement_serpent_reinforced_side_1", ArrivalSpeaker.ReinforcedSideTroops)
                        },
                        new[] { "event:/ui/mission/horns/attack" });
                default:
                    return null;
            }
        }

        private static IReadOnlyList<CultureArrivalLine> GetCultureArrivalPool(string cultureId, bool reinforcesPlayer)
        {
            switch (cultureId)
            {
                case TORConstants.Cultures.EMPIRE:
                    return reinforcesPlayer
                        ? new[]
                        {
                            new CultureArrivalLine("tor_reinforcement_empire_allied_1", "event:/ui/mission/horns/move", "event:/alerts/horns/reinforcements"),
                            new CultureArrivalLine("tor_reinforcement_empire_allied_2", "event:/ui/mission/horns/move", "event:/alerts/horns/reinforcements")
                        }
                        : new[]
                        {
                            new CultureArrivalLine("tor_reinforcement_empire_enemy_1", "event:/ui/mission/horns/move", "event:/alerts/horns/reinforcements"),
                            new CultureArrivalLine("tor_reinforcement_empire_enemy_2", "event:/ui/mission/horns/move", "event:/alerts/horns/reinforcements")
                        };
                case TORConstants.Cultures.BRETONNIA:
                    return reinforcesPlayer
                        ? new[]
                        {
                            new CultureArrivalLine("tor_reinforcement_bretonnia_allied_1", "event:/ui/mission/horns/attack", "event:/alerts/horns/reinforcements"),
                            new CultureArrivalLine("tor_reinforcement_bretonnia_allied_2", "event:/ui/mission/horns/attack", "event:/alerts/horns/reinforcements")
                        }
                        : new[]
                        {
                            new CultureArrivalLine("tor_reinforcement_bretonnia_enemy_1", "event:/ui/mission/horns/attack", "event:/alerts/horns/reinforcements"),
                            new CultureArrivalLine("tor_reinforcement_bretonnia_enemy_2", "event:/ui/mission/horns/attack", "event:/alerts/horns/reinforcements")
                        };
                case TORConstants.Cultures.SYLVANIA:
                    return reinforcesPlayer
                        ? new[]
                        {
                            new CultureArrivalLine("tor_reinforcement_sylvania_allied_1", "event:/ui/mission/horns/attack", "event:/alerts/horns/reinforcements"),
                            new CultureArrivalLine("tor_reinforcement_sylvania_allied_2", "event:/ui/mission/horns/attack", "event:/alerts/horns/reinforcements")
                        }
                        : new[]
                        {
                            new CultureArrivalLine("tor_reinforcement_sylvania_enemy_1", "event:/ui/mission/horns/attack", "event:/alerts/horns/reinforcements"),
                            new CultureArrivalLine("tor_reinforcement_sylvania_enemy_2", "event:/ui/mission/horns/attack", "event:/alerts/horns/reinforcements")
                        };
                case TORConstants.Cultures.MOUSILLON:
                    return reinforcesPlayer
                        ? new[]
                        {
                            new CultureArrivalLine("tor_reinforcement_mousillon_allied_1", "event:/ui/mission/horns/attack", "event:/alerts/horns/reinforcements"),
                            new CultureArrivalLine("tor_reinforcement_mousillon_allied_2", "event:/ui/mission/horns/attack", "event:/alerts/horns/reinforcements")
                        }
                        : new[]
                        {
                            new CultureArrivalLine("tor_reinforcement_mousillon_enemy_1", "event:/ui/mission/horns/attack", "event:/alerts/horns/reinforcements"),
                            new CultureArrivalLine("tor_reinforcement_mousillon_enemy_2", "event:/ui/mission/horns/attack", "event:/alerts/horns/reinforcements")
                        };
                case TORConstants.Cultures.ASRAI:
                    return reinforcesPlayer
                        ? new[]
                        {
                            new CultureArrivalLine("tor_reinforcement_asrai_allied_1", "event:/ui/mission/horns/move", "event:/alerts/horns/reinforcements"),
                            new CultureArrivalLine("tor_reinforcement_asrai_allied_2", "event:/ui/mission/horns/move", "event:/alerts/horns/reinforcements")
                        }
                        : new[]
                        {
                            new CultureArrivalLine("tor_reinforcement_asrai_enemy_1", "event:/ui/mission/horns/move", "event:/alerts/horns/reinforcements"),
                            new CultureArrivalLine("tor_reinforcement_asrai_enemy_2", "event:/ui/mission/horns/move", "event:/alerts/horns/reinforcements")
                        };
                case TORConstants.Cultures.EONIR:
                    return reinforcesPlayer
                        ? new[]
                        {
                            new CultureArrivalLine("tor_reinforcement_eonir_allied_1", "event:/ui/mission/horns/move", "event:/alerts/horns/reinforcements"),
                            new CultureArrivalLine("tor_reinforcement_eonir_allied_2", "event:/ui/mission/horns/move", "event:/alerts/horns/reinforcements")
                        }
                        : new[]
                        {
                            new CultureArrivalLine("tor_reinforcement_eonir_enemy_1", "event:/ui/mission/horns/move", "event:/alerts/horns/reinforcements"),
                            new CultureArrivalLine("tor_reinforcement_eonir_enemy_2", "event:/ui/mission/horns/move", "event:/alerts/horns/reinforcements")
                        };
                case TORConstants.Cultures.DAWI:
                    return reinforcesPlayer
                        ? new[]
                        {
                            new CultureArrivalLine("tor_reinforcement_dawi_allied_1", "event:/ui/mission/horns/move", "event:/alerts/horns/reinforcements"),
                            new CultureArrivalLine("tor_reinforcement_dawi_allied_2", "event:/ui/mission/horns/move", "event:/alerts/horns/reinforcements")
                        }
                        : new[]
                        {
                            new CultureArrivalLine("tor_reinforcement_dawi_enemy_1", "event:/ui/mission/horns/move", "event:/alerts/horns/reinforcements"),
                            new CultureArrivalLine("tor_reinforcement_dawi_enemy_2", "event:/ui/mission/horns/move", "event:/alerts/horns/reinforcements")
                        };
                case TORConstants.Cultures.GREENSKIN:
                    return reinforcesPlayer
                        ? new[]
                        {
                            new CultureArrivalLine("tor_reinforcement_greenskin_allied_1", "event:/ui/mission/horns/attack", "event:/alerts/horns/reinforcements"),
                            new CultureArrivalLine("tor_reinforcement_greenskin_allied_2", "event:/ui/mission/horns/attack", "event:/alerts/horns/reinforcements")
                        }
                        : new[]
                        {
                            new CultureArrivalLine("tor_reinforcement_greenskin_enemy_1", "event:/ui/mission/horns/attack", "event:/alerts/horns/reinforcements"),
                            new CultureArrivalLine("tor_reinforcement_greenskin_enemy_2", "event:/ui/mission/horns/attack", "event:/alerts/horns/reinforcements")
                        };
                case TORConstants.Cultures.BEASTMEN:
                    return reinforcesPlayer
                        ? new[]
                        {
                            new CultureArrivalLine("tor_reinforcement_beastmen_allied_1", "event:/ui/mission/horns/attack", "event:/alerts/horns/reinforcements"),
                            new CultureArrivalLine("tor_reinforcement_beastmen_allied_2", "event:/ui/mission/horns/attack", "event:/alerts/horns/reinforcements")
                        }
                        : new[]
                        {
                            new CultureArrivalLine("tor_reinforcement_beastmen_enemy_1", "event:/ui/mission/horns/attack", "event:/alerts/horns/reinforcements"),
                            new CultureArrivalLine("tor_reinforcement_beastmen_enemy_2", "event:/ui/mission/horns/attack", "event:/alerts/horns/reinforcements")
                        };
                default:
                    return reinforcesPlayer
                        ? new[]
                        {
                            new CultureArrivalLine("tor_reinforcement_generic_allied_1", "event:/alerts/horns/reinforcements"),
                            new CultureArrivalLine("tor_reinforcement_generic_allied_2", "event:/alerts/horns/reinforcements")
                        }
                        : new[]
                        {
                            new CultureArrivalLine("tor_reinforcement_generic_enemy_1", "event:/alerts/horns/reinforcements"),
                            new CultureArrivalLine("tor_reinforcement_generic_enemy_2", "event:/alerts/horns/reinforcements")
                        };
            }
        }

        private static CharacterObject GetRandomTroop(IEnumerable<MapEventParty> parties, string cultureId = null)
        {
            var troops = parties
                .SelectMany(x => x.Troops)
                .Where(x => x.State == RosterTroopState.Active && !x.Troop.IsHero && (cultureId == null || x.Troop.Culture.StringId == cultureId))
                .Select(x => x.Troop)
                .ToList();

            return troops.Count > 0 ? troops[MBRandom.RandomInt(troops.Count)] : null;
        }

        private static void PlayArrivalSound(IReadOnlyList<string> soundEvents)
        {
            var soundEvent = soundEvents[MBRandom.RandomInt(soundEvents.Count)];
            var soundId = SoundEvent.GetEventIdFromString(soundEvent);
            if (soundId < 0)
                return;

            var cameraFrame = Mission.Current.GetCameraFrame();
            var soundPosition = cameraFrame.origin + cameraFrame.rotation.u;
            MBSoundEvent.PlaySound(soundId, soundPosition);
        }
    }

    // provides the instance needed to watch when the queued reinforcement banner becomes current
    [HarmonyPatch(typeof(GameNotificationVM), nameof(GameNotificationVM.AddGameNotification))]
    internal static class ReinforcementArrivalNotificationPatch
    {
        [HarmonyPostfix]
        private static void Postfix(GameNotificationVM __instance, string notificationText)
        {
            if (ReinforcementArrivalNotifier.IsArrivalNotification(notificationText))
                ReinforcementArrivalNotifier.BindNotificationVm(__instance);
        }
    }
}