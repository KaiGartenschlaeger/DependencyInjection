using System;

namespace KeepSimple.DependencyInjection
{
    internal class DependencyItem
    {
        #region Properties

        public DependencyLifetime Lifetime { get; set; }
        public Type ServiceType { get; set; }
        public Type ImplementationType { get; set; }
        public Func<IDependencyResolver, object> Factory { get; set; }
        public object Instance { get; set; }

        #endregion

        #region Methods

        public override string ToString()
        {
            return $"Lifetime = {Lifetime}, ServiceType = {ServiceType}";
        }

        #endregion
    }
}