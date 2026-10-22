using UnityEngine;
using UnityEngine.Events;

namespace TutorialFramework
{
    public abstract class BaseInteraction : MonoBehaviour
    {
        [Header("Events")]
        public UnityEvent onCompleted;

        public UnityEvent onUndone;


        [Header("Wrong Interaction Message")]
        public string wrongInteractionMessage;


        [SerializeField]
        protected bool completed;


        public bool IsCompleted => completed;



        public abstract void Begin();



        public virtual void ResetInteraction()
        {
            completed = false;
        }



        protected void Complete()
        {
            if (completed)
                return;


            completed = true;

            onCompleted?.Invoke();
        }



        protected void Undo()
        {
            if (!completed)
                return;


            completed = false;

            onUndone?.Invoke();
        }



        public virtual void ForceComplete()
        {
            Complete();
        }



        public virtual void ForceUndo()
        {
            Undo();
        }
    }
}