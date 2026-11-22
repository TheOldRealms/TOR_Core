using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ViewModelCollection;
using TaleWorlds.CampaignSystem.ViewModelCollection.CharacterDeveloper;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TOR_Core.CharacterDevelopment;
using TOR_Core.Utilities;

namespace TOR_Core.Extensions.UI
{
    /// <summary>
    /// Adds <see cref="PaddedSkills"/> to <see cref="CharacterDeveloperHeroItemVM"/>,
    /// a parallel view of the hero's skills grouped by attribute and padded
    /// to a fixed 4-column width. The CharacterDeveloper prefab binds its
    /// SkillsGrid to this list so Vigor's four skills (incl. Berserk)
    /// line up with the other attributes' three + empty slot without
    /// breaking the per-attribute row alignment.
    /// </summary>
    [ViewModelExtension(typeof(CharacterDeveloperHeroItemVM))]
    public class CharacterDeveloperHeroItemVMExtension : BaseViewModelExtension
    {
        /// <summary>
        /// Fixed column count the padded grid renders at. Any attribute
        /// with fewer than this many real skills is padded with placeholder
        /// SkillVMs (see <see cref="TORSkills.EmptySkillSlot"/>) so each
        /// attribute occupies exactly one grid row.
        /// </summary>
        public const int SlotsPerAttribute = 4;

        private MBBindingList<SkillVM> _paddedSkills = new MBBindingList<SkillVM>();

        public CharacterDeveloperHeroItemVMExtension(ViewModel vm) : base(vm)
        {
            RebuildPaddedSkills();
        }

        [DataSourceProperty]
        public MBBindingList<SkillVM> PaddedSkills
        {
            get => _paddedSkills;
            set
            {
                if (value == _paddedSkills) return;
                _paddedSkills = value;
                _vm.OnPropertyChangedWithValue(value, nameof(PaddedSkills));
            }
        }

        /// <summary>
        /// Walks the hero item's <c>Skills</c> list, groups by attribute,
        /// sorts both attributes and intra-attribute skills with vanilla's
        /// comparers, and pads each attribute's slice to exactly
        /// <see cref="SlotsPerAttribute"/> entries using placeholder SkillVMs.
        /// </summary>
        private void RebuildPaddedSkills()
        {
            try
            {
                RebuildPaddedSkillsCore();
            }
            catch (Exception e)
            {
                TORCommon.Log(LogLevel.Error,
                    "[PaddedSkills] RebuildPaddedSkills threw: " + e.GetType().Name
                    + ": " + e.Message + "\n" + e.StackTrace);
                PaddedSkills = new MBBindingList<SkillVM>();
            }
        }

        private void RebuildPaddedSkillsCore()
        {
            var heroItem = _vm as CharacterDeveloperHeroItemVM;
            if (heroItem?.Skills == null || heroItem.Hero == null)
            {
                PaddedSkills = new MBBindingList<SkillVM>();
                return;
            }

            // Group real skills by their first attribute (vanilla assumes 1:1).
            var grouped = new Dictionary<CharacterAttribute, List<SkillVM>>();
            foreach (var s in heroItem.Skills)
            {
                var attr = s.Skill?.Attributes != null && s.Skill.Attributes.Length > 0
                    ? s.Skill.Attributes[0]
                    : null;
                if (attr == null) continue;
                if (!grouped.TryGetValue(attr, out var list))
                {
                    list = new List<SkillVM>();
                    grouped[attr] = list;
                }
                list.Add(s);
            }

            // Preserve vanilla's attribute display order.
            var attributes = TaleWorlds.CampaignSystem.Extensions.Attributes.All.ToList();
            attributes.Sort(CampaignUIHelper.CharacterAttributeComparerInstance);

            var padded = new MBBindingList<SkillVM>();
            foreach (var attr in attributes)
            {
                if (!grouped.TryGetValue(attr, out var skills)) continue;
                skills.Sort((a, b) =>
                    CampaignUIHelper.SkillObjectComparerInstance.Compare(a.Skill, b.Skill));
                foreach (var s in skills) padded.Add(s);

                // Pad with placeholder SkillVMs so each attribute occupies
                // exactly SlotsPerAttribute cells in the grid.
                int shortfall = SlotsPerAttribute - skills.Count;
                for (int i = 0; i < shortfall; i++)
                {
                    padded.Add(new SkillVM(TORSkills.EmptySkillSlot, heroItem, _ => { }));
                }
            }
            PaddedSkills = padded;
        }

        public override void RefreshValues()
        {
            base.RefreshValues();
            // Vanilla's InitializeCharacter runs inside the hero-item VM
            // ctor; RefreshValues fires when the user flips between heroes
            // in the clan list. Rebuild so each hero's padded view is fresh.
            RebuildPaddedSkills();
        }
    }
}
