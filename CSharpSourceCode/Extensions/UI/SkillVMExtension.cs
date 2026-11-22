using System;
using NLog;
using TaleWorlds.CampaignSystem.ViewModelCollection.CharacterDeveloper;
using TaleWorlds.Library;
using TOR_Core.CharacterDevelopment;
using TOR_Core.Utilities;

namespace TOR_Core.Extensions.UI
{
    /// <summary>
    /// Adds <see cref="IsPlaceholder"/> to <see cref="SkillVM"/> so
    /// <see cref="CharacterDeveloperHeroItemVMExtension.PaddedSkills"/>
    /// can mark the padding entries. The Character Developer prefab's
    /// SkillGridItem hides its content when IsPlaceholder is true, so
    /// an empty attribute slot renders as a blank cell instead of a
    /// broken 0-level skill chip.
    /// </summary>
    [ViewModelExtension(typeof(SkillVM))]
    public class SkillVMExtension : BaseViewModelExtension
    {
        private bool _isPlaceholder;

        public SkillVMExtension(ViewModel vm) : base(vm)
        {
            try
            {
                var skillVm = vm as SkillVM;
                IsPlaceholder = skillVm?.Skill?.StringId == TORSkills.EmptySkillSlotStringId;
            }
            catch (Exception e)
            {
                TORCommon.Log(LogLevel.Error,
                    "[SkillVMExtension] ctor threw: " + e.GetType().Name
                    + ": " + e.Message + "\n" + e.StackTrace);
            }
        }

        [DataSourceProperty]
        public bool IsPlaceholder
        {
            get => _isPlaceholder;
            set
            {
                if (value == _isPlaceholder) return;
                _isPlaceholder = value;
                _vm.OnPropertyChangedWithValue(value, nameof(IsPlaceholder));
            }
        }
    }
}
