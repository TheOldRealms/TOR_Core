using System;

namespace TOR_Core.Extensions.UI
{
    public class ViewModelExtensionAttribute : Attribute
    {
        public Type BaseType { get; }
        public string RefreshMethodName { get; }

        public ViewModelExtensionAttribute(Type type, string refreshMethodName = "RefreshValues")
        {
            BaseType = type;
            RefreshMethodName = refreshMethodName;
        }
    }
}