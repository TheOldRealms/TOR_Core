using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using NLog.Fluent;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TOR_Core.Utilities;

namespace TOR_Core.Extensions.UI
{
    public class ViewModelExtensionManager
    {
        private readonly Dictionary<ViewModel, IViewModelExtension> _extensionInstances = [];
        public static ViewModelExtensionManager Instance { get; private set; }
        public Dictionary<Type, Type> ExtensionTypes { get; private set; } = new Dictionary<Type, Type>();
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
                        if(type.GetCustomAttribute<ViewModelExtensionAttribute>()?.BaseType != null)
                        {
                            ExtensionTypes.Add(type.GetCustomAttribute<ViewModelExtensionAttribute>().BaseType, type);
                        }
                    }
                }
			}
		}

        public bool HasViewModelExtensionType(ViewModel vm)
        {
            return ExtensionTypes.TryGetValue(vm.GetType(), out _);
        }

        public bool HasViewModelExtensionInstance(ViewModel vm)
        {
            return GetExtensionInstance(vm) != null;
        }

        public Type GetExtensionType(ViewModel vm)
        {
            if (ExtensionTypes.TryGetValue(vm.GetType(), out var extension))
            {
                return extension;
            }
            return null;
        }

        public IViewModelExtension GetExtensionInstance(ViewModel vm)
        {
            return _extensionInstances.FirstOrDefaultQ(x => x.Key == vm).Value;
        }       
    }
}
