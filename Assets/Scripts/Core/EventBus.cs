using System;
using System.Collections.Generic;

public static class EventBus
{
    private static readonly Dictionary<Type, Delegate> Subscribers = new Dictionary<Type, Delegate>();

    public static void Subscribe<T>(Action<T> callback)
    {
        if (callback == null)
        {
            return;
        }

        Type eventType = typeof(T);
        if (Subscribers.TryGetValue(eventType, out Delegate existing))
        {
            Subscribers[eventType] = Delegate.Combine(existing, callback);
            return;
        }

        Subscribers[eventType] = callback;
    }

    public static void Unsubscribe<T>(Action<T> callback)
    {
        if (callback == null)
        {
            return;
        }

        Type eventType = typeof(T);
        if (!Subscribers.TryGetValue(eventType, out Delegate existing))
        {
            return;
        }

        Delegate updated = Delegate.Remove(existing, callback);
        if (updated == null)
        {
            Subscribers.Remove(eventType);
            return;
        }

        Subscribers[eventType] = updated;
    }

    public static void Publish<T>(T payload)
    {
        Type eventType = typeof(T);
        if (!Subscribers.TryGetValue(eventType, out Delegate existing))
        {
            return;
        }

        if (existing is Action<T> callback)
        {
            callback.Invoke(payload);
        }
    }

    public static void Clear()
    {
        Subscribers.Clear();
    }
}
