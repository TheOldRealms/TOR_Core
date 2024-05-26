using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.Library;

namespace TOR_Core.Extensions.UI
{
    public class ViewModelExtensionManager
    {
        private readonly Dictionary<ViewModel, IViewModelExtension> _extensionInstances = new Dictionary<ViewModel, IViewModelExtension>();
        public static ViewModelExtensionManager Instance { get; private set; }
        public List<Type> ExtensionTypes { get; private set; } = new List<Type>();
        private ViewModelExtensionManager()
        {
            Instance = this;
            CollectViewModelExtensions();
        }

        internal void RegisterExtension(IViewModelExtension extension, ViewModel vm)
        {
            if(vm != null && extension != null && extension.GetType().GetCustomAttribute<ViewModelExtensionAttribute>() != null)
            {
                _extensionInstances.Add(vm, extension);
            }
        }

        internal void UnRegisterExtension(IViewModelExtension extension, ViewModel vm)
        {
            if (_extensionInstances.ContainsKey(vm))
            {
                _extensionInstances.Remove(vm);
            }
        }

        public static void Initialize() => _ = new ViewModelExtensionManager();

        public void CollectViewModelExtensions()
        {
            IEnumerable<Assembly> assemblies = AccessTools.AllAssemblies();
			foreach (Assembly assembly in assemblies)
			{
                if (assembly.IsDynamic) continue;
                foreach(var type in assembly.GetExportedTypes())
                {
                    if ((typeof(IViewModelExtension)).IsAssignableFrom(type))
                    {
                        if(type.GetCustomAttribute<ViewModelExtensionAttribute>() != null)
                        {
                            ExtensionTypes.Add(type);
                        }
                    }
                }
			}
		}

        public bool HasViewModelExtension(ViewModel vm)
        {
            return ExtensionTypes.Any(x => x.GetCustomAttribute<ViewModelExtensionAttribute>().BaseType == vm.GetType());
        }

        public Type GetExtensionType(ViewModel vm)
        {
            if (!HasViewModelExtension(vm)) return null;
            return ExtensionTypes.FirstOrDefault(x => x.GetCustomAttribute<ViewModelExtensionAttribute>().BaseType == vm.GetType());
        }

        public IViewModelExtension GetExtensionInstance(ViewModel vm)
        {
            if (!HasViewModelExtension(vm)) return null;
            return _extensionInstances.FirstOrDefault(x => x.Key == vm).Value;
        }
    }
}
