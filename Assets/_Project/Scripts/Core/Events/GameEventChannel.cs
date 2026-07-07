using System;
using System.Collections.Generic;
using UnityEngine;

namespace CarMatchClone.Core.Events
{
    public abstract class GameEventChannel<T> : ScriptableObject
    {
        private readonly List<Action<T>> _listeners = new List<Action<T>>();

        public void Raise(T payload)
        {
            for (int i = _listeners.Count - 1; i >= 0; i--)
                _listeners[i]?.Invoke(payload);
        }

        public void Subscribe(Action<T> listener) => _listeners.Add(listener);
        public void Unsubscribe(Action<T> listener) => _listeners.Remove(listener);
    }
}
