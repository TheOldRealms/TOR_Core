using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            var manager = ViewModelExtensionManager.Instance;
            return manager != null && manager.HasViewModelExtensionType(model);
        }
        public static Type GetExtensionType(this ViewModel model)
        {
            if (model == null)
            {
                return null;
            }
            var manager = ViewModelExtensionManager.Instance;
            return manager != null ? manager.GetExtensionType(model) : null;
        }

        public static IViewModelExtension GetExtensionInstance(this ViewModel model)
        {
            var manager = ViewModelExtensionManager.Instance;
            return manager != null ? manager.GetExtensionInstance(model) : null;
        }

        public static bool HasExtensionInstance(this ViewModel model)
        {
            var manager = ViewModelExtensionManager.Instance;
            return manager != null && manager.HasViewModelExtensionInstance(model);
        }
    }
}