using UnityEngine;

namespace Obvious.Soap
{
    /// <summary>
    /// a component that caches a reference to a component
    /// </summary>
    /// <typeparam name="T">Type</typeparam>
    public class CacheComponent<T> : MonoBehaviour
    {
        protected T _component;

        protected virtual void Awake()
        {
            if (!enabled) return; // Restored legacy handlers stay inert while disabled.
            GetReference();
        }

        private void Reset()
        {
            GetReference();
        }

        private void GetReference()
        {
            if (_component != null)
                return;
            _component = GetComponent<T>();
        }
    }
}