using System;
using System.Collections.Generic;
using System.Linq;
using Reflex.Extensions;
using Reflex.Generics;
using Reflex.Resolvers;

namespace Reflex.Core
{
    public class ContainerBuilder
    {
        private readonly List<Binding> _bindings = new();
        public string Name { get; private set; }
        public Container Parent { get; private set; }
        public event Action<Container> OnContainerBuilt;

        public Container Build()
        {
            var disposables = new DisposableCollection();
            var resolversByContract = new Dictionary<Type, List<IResolver>>();

            // Inherit parent resolvers
            if (Parent != null)
            {
                foreach (var kvp in Parent.ResolversByContract)
                {
                    resolversByContract[kvp.Key] = kvp.Value.ToList();
                }
            }

            // Owned Resolvers
            foreach (var binding in _bindings)
            {
                disposables.Add(binding.Resolver);

                foreach (var contract in binding.Contracts)
                {
                    resolversByContract.GetOrAdd(contract, _ => new List<IResolver>()).Add(binding.Resolver);
                }
            }

            var container = new Container(Name, resolversByContract, disposables);
            container.SetParent(Parent);

            OnContainerBuilt?.Invoke(container);
            return container;
        }

        public ContainerBuilder SetName(string name)
        {
            Name = name;
            return this;
        }
        
        public ContainerBuilder SetParent(Container parent)
        {
            Parent = parent;
            return this;
        }

        public ContainerBuilder AddSingleton(Type concrete, params Type[] contracts)
        {
            return Add(concrete, contracts, new SingletonTypeResolver(concrete));
        }

        public ContainerBuilder AddSingleton(Type concrete)
        {
            return AddSingleton(concrete, concrete);
        }

        public ContainerBuilder AddSingleton(object instance, params Type[] contracts)
        {
            return Add(instance.GetType(), contracts, new SingletonValueResolver(instance));
        }

        public ContainerBuilder AddSingleton(object instance)
        {
            return AddSingleton(instance, instance.GetType());
        }

        public ContainerBuilder AddSingleton<T>(Func<Container, T> factory, params Type[] contracts)
        {
            var resolver = new SingletonFactoryResolver(Proxy);
            return Add(typeof(T), contracts, resolver);

            object Proxy(Container container)
            {
                return factory.Invoke(container);
            }
        }

        public ContainerBuilder AddSingleton<T>(Func<Container, T> factory)
        {
            return AddSingleton(factory, typeof(T));
        }

        public ContainerBuilder AddTransient(Type concrete, params Type[] contracts)
        {
            return Add(concrete, contracts, new TransientTypeResolver(concrete));
        }

        public ContainerBuilder AddTransient(Type concrete)
        {
            return AddTransient(concrete, concrete);
        }

        public ContainerBuilder AddTransient(object instance, params Type[] contracts)
        {
            return Add(instance.GetType(), contracts, new TransientValueResolver(instance));
        }

        public ContainerBuilder AddTransient(object instance)
        {
            return AddTransient(instance, instance.GetType());
        }

        public ContainerBuilder AddTransient<T>(Func<Container, T> factory, params Type[] contracts)
        {
            var resolver = new TransientFactoryResolver(Proxy);
            return Add(typeof(T), contracts, resolver);

            object Proxy(Container container)
            {
                return factory.Invoke(container);
            }
        }

        public ContainerBuilder AddTransient<T>(Func<Container, T> factory)
        {
            return AddTransient(factory, typeof(T));
        }

        public bool HasBinding(Type type)
        {
            return _bindings.Any(binding => binding.Contracts.Contains(type));
        }

        private ContainerBuilder Add(Type concrete, Type[] contracts, IResolver resolver)
        {
            var binding = Binding.Validated(resolver, concrete, contracts);
            _bindings.Add(binding);
            return this;
        }
    }
}