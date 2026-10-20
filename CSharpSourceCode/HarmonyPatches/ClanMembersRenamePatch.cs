using HarmonyLib;
using System.Collections.Generic;
using System.IO;
using TaleWorlds.GauntletUI.PrefabSystem;

namespace TOR_Core.HarmonyPatches
{
    [HarmonyPatch(typeof(WidgetPrefab), nameof(WidgetPrefab.LoadFrom))]
    public static class ClanMembersRenamePatch
    {
        [HarmonyPostfix]
        public static void EnableCompanionRenaming(string path, WidgetAttributeContext widgetAttributeContext, WidgetPrefab __result)
        {
            if (Path.GetFileName(path) != "ClanMembers.xml") return;

            var widgets = new Stack<WidgetTemplate>();
            widgets.Push(__result.RootTemplate);

            while (widgets.Count > 0)
            {
                var widget = widgets.Pop();
                if (widget.Type == "ButtonWidget" && widget.Id == "ChangeNameButton")
                {
                    widget.AddAttributeTo(widgetAttributeContext, "IsEnabled", "true");

                    for (var i = 0; i < widget.ChildCount; i++)
                    {
                        var child = widget.GetChildAt(i);
                        if (child.Type == "BrushWidget")
                        {
                            child.AddAttributeTo(widgetAttributeContext, "IsVisible", "true");
                        }
                    }
                    return;
                }
                for (var i = 0; i < widget.ChildCount; i++)
                {
                    widgets.Push(widget.GetChildAt(i));
                }
            }
        }
    }
}
