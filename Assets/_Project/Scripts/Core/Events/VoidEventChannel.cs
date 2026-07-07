using System;
using System.Collections.Generic;
using UnityEngine;

namespace CarMatchClone.Core.Events
{
    [CreateAssetMenu(menuName = "CarMatchClone/Events/VoidEventChannel")]
    public class VoidEventChannel : ScriptableObject
    {
        private readonly List<Action> _listeners = new List<Action>();

        public void Raise()
        {
            for (int i = _listeners.Count - 1; i >= 0; i--)
                _listeners[i]?.Invoke();
        }

        public void Subscribe(Action listener) => _listeners.Add(listener);
        public void Unsubscribe(Action listener) => _listeners.Remove(listener);
    }
}
