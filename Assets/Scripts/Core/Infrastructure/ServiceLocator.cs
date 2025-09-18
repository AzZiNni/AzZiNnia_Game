using System;
using System.Collections.Generic;

namespace Azurin.Core
{
    /// <summary>
    /// Extremely small and explicit service locator to decouple runtime lookups.
    /// Register services at bootstrap and resolve via Get<T>().
    /// </summary>
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, object> typeToService = new Dictionary<Type, object>();

        public static void Register<TService>(TService instance) where TService : class
        {
            if (instance == null) throw new ArgumentNullException(nameof(instance));
            typeToService[typeof(TService)] = instance;
        }

        public static bool TryGet<TService>(out TService service) where TService : class
        {
            if (typeToService.TryGetValue(typeof(TService), out var obj))
            {
                service = obj as TService;
                return service != null;
            }
            service = null;
            return false;
        }

        public static TService Get<TService>() where TService : class
        {
            if (TryGet<TService>(out var s)) return s;
            throw new InvalidOperationException($"Service of type {typeof(TService).Name} is not registered");
        }

        public static void Unregister<TService>() where TService : class
        {
            typeToService.Remove(typeof(TService));
        }

        public static void Clear()
        {
            typeToService.Clear();
        }
    }
}


