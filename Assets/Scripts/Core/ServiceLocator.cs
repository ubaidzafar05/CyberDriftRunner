using System;
using System.Collections.Generic;

public static class ServiceLocator
{
    private static readonly Dictionary<Type, object> Services = new Dictionary<Type, object>();

    public static void Register<T>(T service) where T : class
    {
        if (service == null)
        {
            return;
        }

        Services[typeof(T)] = service;
    }

    public static bool TryResolve<T>(out T service) where T : class
    {
        if (Services.TryGetValue(typeof(T), out object resolved) && resolved is T typed)
        {
            service = typed;
            return true;
        }

        service = null;
        return false;
    }

    public static T Resolve<T>() where T : class
    {
        return TryResolve(out T service) ? service : null;
    }

    public static void Clear()
    {
        Services.Clear();
    }
}
