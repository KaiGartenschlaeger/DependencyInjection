using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace PureFreak.DependencyInjection
{
    public class DependencyResolver : IDependencyResolver
    {
        #region Fields

        private readonly Dictionary<Type, DependencyItem> _dependencies;
        private readonly HashSet<Type> _requestedTypes;
        private bool _isDisposed;

        #endregion

        #region Constructor

        public DependencyResolver()
        {
            _dependencies = new Dictionary<Type, DependencyItem>();
            _isDisposed = false;
            _requestedTypes = new HashSet<Type>();
        }

        #endregion

        #region Methods

        private void AddInternal(
            Type serviceType,
            Type implementationType,
            DependencyLifetime lifetime,
            object instance,
            Func<IDependencyResolver, object> factory)
        {
            if (_dependencies.ContainsKey(serviceType))
                throw new InvalidOperationException($"A instance of type \"{serviceType}\" has been already added.");

            _dependencies.Add(serviceType, new DependencyItem
            {
                ServiceType = serviceType,
                ImplementationType = implementationType,
                Lifetime = lifetime,
                Instance = instance,
                Factory = factory
            });
        }

        public void AddSingleton<TService>()
            where TService : class
        {
            AddInternal(
                serviceType: typeof(TService),
                implementationType: null,
                lifetime: DependencyLifetime.Singleton,
                instance: null,
                factory: null);
        }

        public void AddSingleton<TService, TImplementation>()
           where TService : class
           where TImplementation : class, TService
        {
            AddInternal(
                serviceType: typeof(TService),
                implementationType: typeof(TImplementation),
                lifetime: DependencyLifetime.Singleton,
                instance: null,
                factory: null);
        }

        public void AddSingleton<TService>(TService serviceInstance)
            where TService : class
        {
            if (serviceInstance == null)
                throw new ArgumentNullException(nameof(serviceInstance));

            AddInternal(
                serviceType: typeof(TService),
                implementationType: null,
                lifetime: DependencyLifetime.Singleton,
                instance: serviceInstance,
                factory: null);
        }

        public void AddSingleton<TService>(Func<IDependencyResolver, TService> serviceFactory)
            where TService : class
        {
            if (serviceFactory == null)
                throw new ArgumentNullException(nameof(serviceFactory));

            AddInternal(
                serviceType: typeof(TService),
                implementationType: null,
                lifetime: DependencyLifetime.Singleton,
                instance: null,
                factory: serviceFactory);
        }

        public void AddTransient<TService>()
            where TService : class
        {
            AddInternal(
                serviceType: typeof(TService),
                implementationType: null,
                lifetime: DependencyLifetime.Transient,
                instance: null,
                factory: null);
        }

        public void AddTransient<TService, TImplementation>()
            where TService : class
            where TImplementation : class, TService
        {
            AddInternal(
                serviceType: typeof(TService),
                implementationType: typeof(TImplementation),
                lifetime: DependencyLifetime.Transient,
                instance: null,
                factory: null);
        }

        public void AddTransient<TService>(Func<IDependencyResolver, TService> serviceFactory)
            where TService : class
        {
            AddInternal(
                serviceType: typeof(TService),
                implementationType: null,
                lifetime: DependencyLifetime.Transient,
                instance: null,
                factory: serviceFactory);
        }

        public object GetService(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            if (type == typeof(IDependencyResolver))
                return this;

            if (_requestedTypes.Contains(type))
                throw new InvalidOperationException($"Could not resolve cross reference type \"{type}\".");

            _requestedTypes.Add(type);

            if (_dependencies.TryGetValue(type, out DependencyItem dependency))
            {
                object instance = null;
                switch (dependency.Lifetime)
                {
                    case DependencyLifetime.Singleton:
                        if (dependency.Instance == null)
                        {
                            if (dependency.Factory != null)
                            {
                                dependency.Instance = dependency.Factory(this);
                                if (dependency.Instance == null)
                                {
                                    throw new InvalidOperationException($"Factory of type \"{dependency.ServiceType}\" must not return null.");
                                }
                            }
                            else
                            {
                                dependency.Instance = CreateInstance(
                                    dependency.ImplementationType != null ?
                                    dependency.ImplementationType
                                    :
                                    dependency.ServiceType);
                            }
                        }

                        instance = dependency.Instance;
                        break;

                    case DependencyLifetime.Transient:
                        if (dependency.Factory != null)
                        {
                            instance = dependency.Factory(this);
                            if (instance == null)
                            {
                                throw new InvalidOperationException($"Factory of type \"{dependency.ServiceType}\" must not return null.");
                            }
                        }
                        else
                        {
                            instance = CreateInstance(
                                dependency.ImplementationType != null ?
                                dependency.ImplementationType
                                :
                                dependency.ServiceType);
                        }
                        break;

                    default:
                        throw new NotImplementedException();
                }

                _requestedTypes.Remove(type);

                return instance;
            }

            throw new InvalidOperationException($"No service for type \"{type}\" has been registered.");
        }

        public TService GetService<TService>()
            where TService : class
        {
            return (TService)GetService(typeof(TService));
        }

        public object CreateInstance(Type type)
        {
            if (!type.IsClass)
                throw new InvalidOperationException($"Cannot resolve type of \"{type}\".");

            var constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
            if (constructors.Length == 0)
            {
                // no constructor found
                throw new InvalidOperationException($"The type \"{type}\" has no public constructor.");
            }
            else if (constructors.Length == 1)
            {
                // single constructor
                var constructor = constructors[0];
                var constructorParameters = constructor.GetParameters();

                return CreateInstanceInternal(type, constructorParameters);
            }
            else
            {
                // multiple constructors found
                var constructor = constructors.FirstOrDefault(c => c.GetCustomAttribute<DependencyAttribute>() != null);
                if (constructor == null)
                {
                    throw new InvalidOperationException($"Multiple constructors found. Add the {nameof(DependencyAttribute)} to the constructor.");
                }

                var constructorParameters = constructor.GetParameters();

                return CreateInstanceInternal(type, constructorParameters);
            }
        }

        private void ResolveProperties(Type requestedType, object instance)
        {
            var instanceType = instance.GetType();
            foreach (var property in instanceType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.SetMethod != null && p.SetMethod.IsPublic && p.GetCustomAttribute<DependencyAttribute>() != null))
            {
                property.SetValue(instance, GetService(property.PropertyType));
            }
        }

        private object CreateInstanceInternal(Type type, ParameterInfo[] constructorParameter)
        {
            object instance;
            if (constructorParameter.Length == 0)
            {
                instance = Activator.CreateInstance(type);
            }
            else
            {
                var constructorParameters = ResolveConstructorParameters(constructorParameter);
                instance = Activator.CreateInstance(type, constructorParameters);
            }

            ResolveProperties(type, instance);

            return instance;
        }

        private object[] ResolveConstructorParameters(ParameterInfo[] parameters)
        {
            var constructorParameters = new object[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                constructorParameters[i] = GetService(parameters[i].ParameterType);
                if (constructorParameters[i] == null)
                {
                    throw new InvalidOperationException($"Unable to resolve dependency of type \"{parameters[i].ParameterType}\".");
                }
            }

            return constructorParameters;
        }

        public TInstance CreateInstance<TInstance>()
           where TInstance : class
        {
            return (TInstance)CreateInstance(typeof(TInstance));
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;

            foreach (var dependency in _dependencies)
            {
                if (dependency.Value.Instance != null)
                {
                    var disposable = dependency.Value.Instance as IDisposable;
                    if (disposable != null)
                        disposable.Dispose();
                }
            }
        }

        #endregion
    }
}