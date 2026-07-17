using System;
using System.Collections.Generic;
using System.Globalization;
using TaleWorlds.Engine.Options;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace TOR_Core.Extensions.UI.MainMenu
{
    internal static class TORRecommendedSettingsService
    {
        private const float ComparisonTolerance = 0.001f;
        private const float TextureBudgetVeryHigh = 3f;
        private const float TessellationDisabled = 0f;
        private const float TerrainQualityMedium = 1f;
        private const float ShadowmapTypeStaticOnly = 1f;
        private const float FrameLimiter120 = 120f;
        private const float NumberOfRagDolls10 = 4f;
        private const float NumberOfCorpsesVeryHigh = 4f;

        private static readonly NativeSetting[] NativeSettings =
        {
            new NativeSetting(NativeOptions.NativeOptionsType.TextureBudget, TextureBudgetVeryHigh),
            new NativeSetting(NativeOptions.NativeOptionsType.Tesselation, TessellationDisabled),
            new NativeSetting(NativeOptions.NativeOptionsType.TerrainQuality, TerrainQualityMedium),
            new NativeSetting(NativeOptions.NativeOptionsType.ShadowmapType, ShadowmapTypeStaticOnly),
            new NativeSetting(NativeOptions.NativeOptionsType.FrameLimiter, FrameLimiter120),
            new NativeSetting(NativeOptions.NativeOptionsType.NumberOfRagDolls, NumberOfRagDolls10)
        };

        private static readonly ManagedSetting[] ManagedSettings =
        {
            new ManagedSetting(ManagedOptions.ManagedOptionsType.NumberOfCorpses, NumberOfCorpsesVeryHigh)
        };

        internal static TORRecommendedSettingsApplyResult ApplyAndVerify()
        {
            try
            {
                foreach (NativeSetting setting in NativeSettings)
                {
                    NativeOptions.SetConfig(setting.Option, setting.Value);
                }
                foreach (ManagedSetting setting in ManagedSettings)
                {
                    ManagedOptions.SetConfig(setting.Option, setting.Value);
                }

                NativeOptions.ApplyConfigChanges(false);
                SaveResult nativeSaveResult = NativeOptions.SaveConfig();
                SaveResult managedSaveResult = ManagedOptions.SaveConfig();

                if (nativeSaveResult != SaveResult.Success || managedSaveResult != SaveResult.Success)
                {
                    return TORRecommendedSettingsApplyResult.SaveFailed(FormatSaveFailures(nativeSaveResult, managedSaveResult));
                }

                List<string> verificationFailures = GetVerificationFailures();
                return verificationFailures.Count == 0
                    ? TORRecommendedSettingsApplyResult.Success()
                    : TORRecommendedSettingsApplyResult.VerificationFailed(string.Join(", ", verificationFailures));
            }
            catch (Exception ex)
            {
                string details = string.IsNullOrWhiteSpace(ex.Message) ? ex.GetType().Name : ex.Message;
                return TORRecommendedSettingsApplyResult.UnexpectedFailure(details);
            }
        }

        private static string FormatSaveFailures(SaveResult nativeSaveResult, SaveResult managedSaveResult)
        {
            var failures = new List<string>(2);
            if (nativeSaveResult != SaveResult.Success)
            {
                failures.Add("NativeOptions=" + nativeSaveResult);
            }
            if (managedSaveResult != SaveResult.Success)
            {
                failures.Add("ManagedOptions=" + managedSaveResult);
            }
            return string.Join(", ", failures);
        }

        private static List<string> GetVerificationFailures()
        {
            var failures = new List<string>(NativeSettings.Length + ManagedSettings.Length);
            foreach (NativeSetting setting in NativeSettings)
            {
                AddFailureIfDifferent(failures, setting.Option.ToString(), setting.Value, NativeOptions.GetConfig(setting.Option));
            }
            foreach (ManagedSetting setting in ManagedSettings)
            {
                AddFailureIfDifferent(failures, setting.Option.ToString(), setting.Value, ManagedOptions.GetConfig(setting.Option));
            }
            return failures;
        }

        private static void AddFailureIfDifferent(List<string> failures, string optionName, float expectedValue, float actualValue)
        {
            if (!float.IsNaN(actualValue) && Math.Abs(actualValue - expectedValue) <= ComparisonTolerance)
            {
                return;
            }

            failures.Add(string.Format(
                CultureInfo.InvariantCulture,
                "{0} (expected {1:0.###}, actual {2:0.###})",
                optionName,
                expectedValue,
                actualValue));
        }

        private readonly struct NativeSetting
        {
            internal NativeOptions.NativeOptionsType Option { get; }
            internal float Value { get; }

            internal NativeSetting(NativeOptions.NativeOptionsType option, float value)
            {
                Option = option;
                Value = value;
            }
        }

        private readonly struct ManagedSetting
        {
            internal ManagedOptions.ManagedOptionsType Option { get; }
            internal float Value { get; }

            internal ManagedSetting(ManagedOptions.ManagedOptionsType option, float value)
            {
                Option = option;
                Value = value;
            }
        }
    }

    internal enum TORRecommendedSettingsApplyStatus
    {
        Success,
        SaveFailed,
        VerificationFailed,
        UnexpectedFailure
    }

    internal readonly struct TORRecommendedSettingsApplyResult
    {
        internal TORRecommendedSettingsApplyStatus Status { get; }
        internal string Details { get; }

        private TORRecommendedSettingsApplyResult(TORRecommendedSettingsApplyStatus status, string details)
        {
            Status = status;
            Details = details;
        }

        internal static TORRecommendedSettingsApplyResult Success()
        {
            return new TORRecommendedSettingsApplyResult(TORRecommendedSettingsApplyStatus.Success, string.Empty);
        }

        internal static TORRecommendedSettingsApplyResult SaveFailed(string details)
        {
            return new TORRecommendedSettingsApplyResult(TORRecommendedSettingsApplyStatus.SaveFailed, details);
        }

        internal static TORRecommendedSettingsApplyResult VerificationFailed(string details)
        {
            return new TORRecommendedSettingsApplyResult(TORRecommendedSettingsApplyStatus.VerificationFailed, details);
        }

        internal static TORRecommendedSettingsApplyResult UnexpectedFailure(string details)
        {
            return new TORRecommendedSettingsApplyResult(TORRecommendedSettingsApplyStatus.UnexpectedFailure, details);
        }
    }
}
