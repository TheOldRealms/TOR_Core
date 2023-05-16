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
        public static bool HasExtension(this ViewModel model) => ViewModelExtensionManager.Instance.HasViewModelExtension(model);
        public static IViewModelExtension GetExtension(this ViewModel model) => ViewModelExtensionManager.Instance.GetExtensionInstance(model);
        public static Type GetExtensionType(this ViewModel model) => ViewModelExtensionManager.Instance.GetExtensionType(model);
    }
}
