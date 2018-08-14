using System;

namespace PureFreak.DependencyInjection
{
    [AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public class DependencyAttribute : Attribute
    {

    }
}