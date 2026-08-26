using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using TaleWorlds.Library;

namespace TOR_Core.Extensions.UI
{
    public class ViewModelExtensionManager
    {
        private readonly ConditionalWeakTable<ViewModel, ExtensionHolder> _extensionInstances = new();

        private sealed class ExtensionHolder
        {
            public IViewModelExtension Extension { get; }

            public ExtensionHolder(IViewModelExtension extension)
            {
                Extension = extension;
            }
        }
        private static ViewModelExtensionManager _instance;

        public static ViewModelExtensionManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new ViewModelExtensionManager();
                }

                return _instance;
            }
        }
        public Dictionary<Type, Type> ExtensionTypes { get; private set; } = new Dictionary<Type, Type>();

        private ViewModelExtensionManager()
        {
            _instance = this;
            CollectViewModelExtensions();
        }

        internal void RegisterExtension(IViewModelExtension extension, ViewModel vm)
        {
            if (vm != null && extension != null && extension.GetType().GetCustomAttribute<ViewModelExtensionAttribute>() != null)
            {
                _extensionInstances.Remove(vm);
                _extensionInstances.Add(vm, new ExtensionHolder(extension));
            }
        }

        internal void UnRegisterExtension(IViewModelExtension extension, ViewModel vm)
        {
            if (vm != null)
            {
                _extensionInstances.Remove(vm);
            }
        }

        public static void Initialize()
        {
            _ = Instance;
        }

        public void CollectViewModelExtensions()
        {

            IEnumerable<Assembly> assemblies = AccessTools.AllAssemblies();
            foreach (Assembly assembly in assemblies)
            {
                if (assembly.IsDynamic) continue;
                foreach (var type in assembly.GetTypesSafe())
                {
                    if ((typeof(IViewModelExtension)).IsAssignableFrom(type))
                    {
                        var attribute = type.GetCustomAttribute<ViewModelExtensionAttribute>();
                        if (attribute?.BaseType != null)
                        {
                            ExtensionTypes[attribute.BaseType] = type;
                        }
                    }
                }
            }
        }

        public bool HasViewModelExtensionType(ViewModel vm)
        {
            if (vm == null)
            {
                return false;
            }
            return ExtensionTypes.TryGetValue(vm.GetType(), out _);
        }

        public bool HasViewModelExtensionInstance(ViewModel vm)
        {
            return vm != null && _extensionInstances.TryGetValue(vm, out _);
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
            return vm != null && _extensionInstances.TryGetValue(vm, out var holder) ? holder.Extension : null;
        }
    }
}