using System;

namespace PureFreak.DependencyInjection
{
    public interface IDependencyResolver : IDisposable
    {
        #region Methods

        void AddSingleton<TService>()
            where TService : class;

        void AddSingleton<TService, TImplementation>()
            where TService : class
            where TImplementation : class, TService;

        void AddSingleton<TService>(TService serviceInstance)
            where TService : class;

        void AddSingleton<TService>(Func<IDependencyResolver, TService> serviceFactory)
            where TService : class;

        void AddTransient<TService>()
            where TService : class;

        void AddTransient<TService, TImplementation>()
            where TService : class
            where TImplementation : class, TService;

        void AddTransient<TService>(Func<IDependencyResolver, TService> serviceFactory)
            where TService : class;

        object GetService(Type type);

        TService GetService<TService>()
            where TService : class;

        object CreateInstance(Type serviceType);

        TInstance CreateInstance<TInstance>()
            where TInstance : class;

        #endregion
    }
}