using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class DelayedEventTrigger : MonoBehaviour
{
    [System.Serializable]
    public class DelayedEvent
    {
        public float delay = 1f;
        public UnityEvent onDelayedEvent;
    }

    [Header("Single Delay Trigger")]
    public float delay = 1f;
    public UnityEvent onTriggeredAfterDelay;

    private Coroutine running;

    // ==============================
    // CALL THIS FROM YOUR EVENT
    // ==============================
    public void TriggerDelay()
    {
        if (running != null)
            StopCoroutine(running);

        running = StartCoroutine(DelayRoutine());
    }

    IEnumerator DelayRoutine()
    {
        yield return new WaitForSeconds(delay);
        onTriggeredAfterDelay?.Invoke();
    }

  
}