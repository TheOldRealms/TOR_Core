using System;
using TaleWorlds.Library;
using TOR_Core.Extensions.UI;

namespace TOR_Core.Extensions
{
    public static class ViewModelExtensions
    {
        public static bool HasExtensionType(this ViewModel model)
        {
            if (model == null)
            {
                return false;
            }
            return ViewModelExtensionManager.Instance.HasViewModelExtensionType(model);
        }
        public static Type GetExtensionType(this ViewModel model)
        {
            if (model == null)
            {
                return null;
            }
            return ViewModelExtensionManager.Instance.GetExtensionType(model);
        }

        public static IViewModelExtension GetExtensionInstance(this ViewModel model)
        {
            return ViewModelExtensionManager.Instance.GetExtensionInstance(model);
        }

        public static bool HasExtensionInstance(this ViewModel model)
        {
            return ViewModelExtensionManager.Instance.HasViewModelExtensionInstance(model);
        }
    }
}