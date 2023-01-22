﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.Library;

namespace TOR_Core.Extensions.UI
{
    public interface IViewModelExtension
    {
        void RefreshValues();
        void OnFinalize();
        object GetViewModelAtPath(BindingPath path);
        object GetPropertyValue(string name);
        void SetPropertyValue(string name, object value);
        void ExecuteCommand(string commandName, object[] parameters);
        Dictionary<string, PropertyInfo> GetProperties();
        Dictionary<string, MethodInfo> GetMethods();
    }

    public abstract class BaseViewModelExtension : IViewModelExtension
    {
        protected ViewModel _vm;
        public BaseViewModelExtension(ViewModel vm)
        {
            _vm = vm;
            ViewModelExtensionManager.Instance.RegisterExtension(this, vm);
        }

        public virtual void RefreshValues() { }

        public virtual void OnFinalize()
        {
            ViewModelExtensionManager.Instance.UnRegisterExtension(this, _vm);
            _vm = null;
        }

        public object GetViewModelAtPath(BindingPath path)
        {
            BindingPath subPath = path.SubPath;
            if (subPath != null)
            {
                PropertyInfo property = _vm.GetType().GetProperty(subPath.FirstNode);
                if (property == null) property = GetType().GetProperty(subPath.FirstNode);
                if (property != null)
                {
                    object obj = null;
                    if(IsExtendedProperty(property)) obj = property.GetGetMethod().InvokeWithLog(this , null);
                    else obj = property.GetGetMethod().InvokeWithLog(_vm, null);
                    ViewModel viewModel;
                    if ((viewModel = (obj as ViewModel)) != null)
                    {
                        return viewModel.GetViewModelAtPath(subPath);
                    }
                    if (obj is IMBBindingList)
                    {
                        return GetChildAtPath(obj as IMBBindingList, subPath);
                    }
                }
                return null;
            }
            return _vm;
        }

        private object GetChildAtPath(IMBBindingList bindingList, BindingPath path)
        {
            BindingPath subPath = path.SubPath;
            if (subPath == null)
            {
                return bindingList;
            }
            if (bindingList.Count > 0)
            {
                int num = Convert.ToInt32(subPath.FirstNode);
                if (num >= 0 && num < bindingList.Count)
                {
                    object obj = bindingList[num];
                    if (obj is ViewModel)
                    {
                        return (obj as ViewModel).GetViewModelAtPath(subPath);
                    }
                    if (obj is IMBBindingList)
                    {
                        return GetChildAtPath(obj as IMBBindingList, subPath);
                    }
                }
            }
            return null;
        }

        public Dictionary<string, PropertyInfo> GetProperties()
        {
            Dictionary<string, PropertyInfo> dictionary = new Dictionary<string, PropertyInfo>();
            foreach (PropertyInfo propertyInfo in GetType().GetProperties((BindingFlags)52))
            {
                if (!dictionary.ContainsKey(propertyInfo.Name))
                {
                    dictionary.Add(propertyInfo.Name, propertyInfo);
                }
            }
            return dictionary;
        }

        public Dictionary<string, MethodInfo> GetMethods()
        {
            Dictionary<string, MethodInfo> dictionary = new Dictionary<string, MethodInfo>();
            foreach (MethodInfo methodInfo in GetType().GetMethods((BindingFlags)52))
            {
                if (!dictionary.ContainsKey(methodInfo.Name))
                {
                    dictionary.Add(methodInfo.Name, methodInfo);
                }
            }
            return dictionary;
        }

        public object GetPropertyValue(string name)
        {
            PropertyInfo property = _vm.GetType().GetProperty(name);
            if (property == null) property = GetType().GetProperty(name);
            object result = null;
            if (property != null)
            {
                if(IsExtendedProperty(property)) result = property.GetGetMethod().InvokeWithLog(this, null);
                else result = property.GetGetMethod().InvokeWithLog(_vm, null);
            }
            return result;
        }

        public void SetPropertyValue(string name, object value)
        {
            PropertyInfo property = _vm.GetType().GetProperty(name);
            if (property == null) property = GetType().GetProperty(name);
            if (property != null)
            {
                MethodInfo setMethod = property.GetSetMethod();
                if (setMethod == null)
                {
                    return;
                }
                if (IsExtendedProperty(property)) setMethod.InvokeWithLog(this, new object[] { value });
                else setMethod.InvokeWithLog(_vm, new object[] { value });
            }
        }

        public void ExecuteCommand(string commandName, object[] parameters)
        {
            throw new NotImplementedException();
        }

        private bool IsExtendedProperty(PropertyInfo prop)
        {
            return typeof(IViewModelExtension).IsAssignableFrom(prop.DeclaringType);
        }
    }
}
