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
        public static bool HasExtensionType(this ViewModel model) => ViewModelExtensionManager.Instance.HasViewModelExtensionType(model);
        public static bool HasExtensionInstance(this ViewModel model) => ViewModelExtensionManager.Instance.HasViewModelExtensionInstance(model);
        public static IViewModelExtension GetExtensionInstance(this ViewModel model) => ViewModelExtensionManager.Instance.GetExtensionInstance(model);
        public static Type GetExtensionType(this ViewModel model) => ViewModelExtensionManager.Instance.GetExtensionType(model);
    }
}
