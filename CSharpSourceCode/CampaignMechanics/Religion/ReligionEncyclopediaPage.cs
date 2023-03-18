using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encyclopedia;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace TOR_Core.CampaignMechanics.Religion
{
    [TorEncyclopediaModel(new Type[]
    {
        typeof(ReligionObject)
    })]
    public class ReligionEncyclopediaPage : EncyclopediaPage, IPublicEncyclopediaPage
    {
        public ReligionEncyclopediaPage()
        {
            base.HomePageOrderIndex = 700;
        }

        protected override IEnumerable<EncyclopediaFilterGroup> InitializeFilterItems()
        {
            List<EncyclopediaFilterGroup> filterGroup = new List<EncyclopediaFilterGroup>();
            List<EncyclopediaFilterItem> filterItems = new List<EncyclopediaFilterItem>();
            List<CultureObject> culturesWithReligions = new List<CultureObject>();
            foreach (var religion in ReligionObject.All)
            {
                if (!culturesWithReligions.Contains(religion.Culture)) culturesWithReligions.Add(religion.Culture);
            }
            foreach(var culture in culturesWithReligions)
            {
                filterItems.Add(new EncyclopediaFilterItem(culture.Name, (object c) => ((ReligionObject)c).Culture == culture));
            }
            filterGroup.Add(new EncyclopediaFilterGroup(filterItems, GameTexts.FindText("str_culture", null)));
            return filterGroup;
        }

        protected override IEnumerable<EncyclopediaListItem> InitializeListItems()
        {
            foreach (var religion in ReligionObject.All)
            {
                yield return new EncyclopediaListItem(religion, religion.Name.ToString(), religion.LoreText.ToString(), religion.StringId, "Religion", true, null);
            }
            yield break;
        }

        protected override IEnumerable<EncyclopediaSortController> InitializeSortControllers()
        {
            return new List<EncyclopediaSortController>();
        }

        public override string GetViewFullyQualifiedName() => "EncyclopediaReligionPage";

        public override TextObject GetName() => GameTexts.FindText("str_concepts", null);

        public override TextObject GetDescriptionText() => GameTexts.FindText("str_concepts", null);

        public override string GetStringID() => "EncyclopediaReligionObject";

        public override bool IsValidEncyclopediaItem(object o)
        {
            ReligionObject obj = o as ReligionObject;
            return obj != null && obj.Name != null && obj.LoreText != null;
        }

        public override MBObjectBase GetObject(string typeName, string stringID) => MBObjectManager.Instance.GetObject<ReligionObject>(stringID);

        public IEnumerable<EncyclopediaFilterGroup> PublicInitializeFilterItems() => InitializeFilterItems();

        public IEnumerable<EncyclopediaListItem> PublicInitializeListItems() => InitializeListItems();

        public IEnumerable<EncyclopediaSortController> PublicInitializeSortControllers() => InitializeSortControllers();
    }

    public class TorEncyclopediaModel : OverrideEncyclopediaModel
    {
        public TorEncyclopediaModel(Type[] pageTargetType) : base(pageTargetType) { }
    }

    public interface IPublicEncyclopediaPage
    {
        IEnumerable<EncyclopediaFilterGroup> PublicInitializeFilterItems();
        IEnumerable<EncyclopediaListItem> PublicInitializeListItems();
        IEnumerable<EncyclopediaSortController> PublicInitializeSortControllers();
    }

    public class TorEncyclopediaListItemNameComparer : EncyclopediaListItemComparerBase
    {
        public override int Compare(EncyclopediaListItem x, EncyclopediaListItem y)
        {
            return base.ResolveEquality(x, y);
        }

        public override string GetComparedValueText(EncyclopediaListItem item)
        {
            return "";
        }
    }
}
