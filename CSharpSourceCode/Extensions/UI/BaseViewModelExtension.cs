using System;
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

    public abstract class BaseViewModelExtension : IViewModelExtension, IDisposable
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
                PropertyInfo property = _vm.GetType().GetProperty(subPath.FirstNode) ?? GetType().GetProperty(subPath.FirstNode);
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
            Dictionary<string, PropertyInfo> dictionary = [];
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
            Dictionary<string, MethodInfo> dictionary = [];
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
            PropertyInfo property = _vm.GetType().GetProperty(name) ?? GetType().GetProperty(name);
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
            PropertyInfo property = _vm.GetType().GetProperty(name) ?? GetType().GetProperty(name);
            if (property != null)
            {
                MethodInfo setMethod = property.GetSetMethod();
                if (setMethod == null)
                {
                    return;
                }
                if (IsExtendedProperty(property)) setMethod.InvokeWithLog(this, [value]);
                else setMethod.InvokeWithLog(_vm, [value]);
            }
        }

        public void ExecuteCommand(string commandName, object[] parameters)
        {
            var methodInfo = _vm.GetType().GetMethod(commandName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (methodInfo == null) methodInfo = GetType().GetMethod(commandName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if(methodInfo != null)
            {
                if (methodInfo.GetParameters().Length == parameters.Length)
                {
                    object[] array = new object[parameters.Length];
                    ParameterInfo[] args = methodInfo.GetParameters();
                    for (int i = 0; i < parameters.Length; i++)
                    {
                        object obj = parameters[i];
                        Type parameterType = args[i].ParameterType;
                        array[i] = obj;
                        if (obj is string && parameterType != typeof(string))
                        {
                            object stringObj = ConvertValueTo((string)obj, parameterType);
                            array[i] = stringObj;
                        }
                    }
                    if(IsExtendedMethod(methodInfo)) methodInfo.InvokeWithLog(this, array);
                    else methodInfo.InvokeWithLog(_vm, array);
                    return;
                }
                if (methodInfo.GetParameters().Length == 0)
                {
                    if (IsExtendedMethod(methodInfo)) methodInfo.InvokeWithLog(this, null);
                    else methodInfo.InvokeWithLog(_vm, null);
                }
            }
        }

        private object ConvertValueTo(string value, Type parameterType)
        {
            object result = null;
            if (parameterType == typeof(string))
            {
                result = value;
            }
            else if (parameterType == typeof(int))
            {
                result = Convert.ToInt32(value);
            }
            else if (parameterType == typeof(float))
            {
                result = Convert.ToSingle(value);
            }
            return result;
        }

        private bool IsExtendedProperty(PropertyInfo prop)
        {
            return typeof(IViewModelExtension).IsAssignableFrom(prop.DeclaringType);
        }

        private bool IsExtendedMethod(MethodInfo method)
        {
            return typeof(IViewModelExtension).IsAssignableFrom(method.DeclaringType);
        }

        public void Dispose()
        {
            _vm = null;
        }
    }
}
