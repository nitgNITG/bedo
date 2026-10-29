using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

namespace Obvious.Soap
{
    /// <summary>
    /// A generic listener for scriptable variables that invokes a response whenever the variable's value changes.
    /// </summary>
    public abstract class ValueChangedListenerGeneric<T> : EventListenerBase
    {
        /// <summary>
        /// Override this with the list of variable + response pairs in your derived class.
        /// Example:
        ///   protected override VariableResponse<T>[] VariableResponses { get { return myVariableResponses; } }
        /// </summary>
        protected virtual VariableResponse<T>[] VariableResponses { get; }

        private readonly Dictionary<ScriptableVariable<T>, VariableResponse<T>> _dictionary =
            new Dictionary<ScriptableVariable<T>, VariableResponse<T>>();

        // Optionally keep a CancellationTokenSource if you want to cancel async tasks.
        protected readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        protected override void ToggleRegistration(bool toggle)
        {
            // Register or unregister each ScriptableVariable in our responses.
            foreach (var variableResponse in VariableResponses)
            {
                if (variableResponse?.ScriptableVariable == null)
                    continue;

                if (toggle)
                {
                    // Subscribe: pass the variable reference + newValue to our handler.
                    variableResponse.ScriptableVariable.OnValueChanged +=
                        (newValue) => OnValueChanged(variableResponse.ScriptableVariable, newValue);

                    if (!_dictionary.ContainsKey(variableResponse.ScriptableVariable))
                        _dictionary.Add(variableResponse.ScriptableVariable, variableResponse);
                }
                else
                {
                    // Unsubscribe
                    variableResponse.ScriptableVariable.OnValueChanged -=
                        (newValue) => OnValueChanged(variableResponse.ScriptableVariable, newValue);

                    if (_dictionary.ContainsKey(variableResponse.ScriptableVariable))
                        _dictionary.Remove(variableResponse.ScriptableVariable);
                }
            }
        }

        /// <summary>
        /// Called whenever a subscribed ScriptableVariable changes its value.
        /// We manually pass the scriptableVar reference from our subscription lambda.
        /// </summary>
        private void OnValueChanged(ScriptableVariable<T> scriptableVar, T newValue, bool debug = false)
        {
            // Make sure the variable is in our dictionary
            if (!_dictionary.TryGetValue(scriptableVar, out var variableResponse))
                return;

            float delay = variableResponse.Delay;
            if (delay > 0)
            {
                // If this GameObject is active in the scene, use a Coroutine; otherwise use async/await.
                if (gameObject.activeInHierarchy)
                {
                    StartCoroutine(Cr_DelayInvokeResponse(scriptableVar, variableResponse, newValue, debug));
                }
                else
                {
                    DelayInvokeResponseAsync(scriptableVar, variableResponse, newValue, debug, _cancellationTokenSource.Token);
                }
            }
            else
            {
                InvokeResponse(scriptableVar, variableResponse, newValue, debug);
            }
        }

        private IEnumerator Cr_DelayInvokeResponse(
            ScriptableVariable<T> scriptableVar,
            VariableResponse<T> variableResponse,
            T param,
            bool debug
        )
        {
            yield return new WaitForSeconds(variableResponse.Delay);
            InvokeResponse(scriptableVar, variableResponse, param, debug);
        }

        private async void DelayInvokeResponseAsync(
            ScriptableVariable<T> scriptableVar,
            VariableResponse<T> variableResponse,
            T param,
            bool debug,
            CancellationToken cancellationToken
        )
        {
            try
            {
                await Task.Delay((int)(variableResponse.Delay * 1000), cancellationToken);
                InvokeResponse(scriptableVar, variableResponse, param, debug);
            }
            catch (TaskCanceledException)
            {
                // Handle cancellation if needed
            }
        }

        private void InvokeResponse(
            ScriptableVariable<T> scriptableVar,
            VariableResponse<T> variableResponse,
            T param,
            bool debug
        )
        {
            variableResponse.Response?.Invoke(param);

            if (debug)
                DebugLogVariableChange(scriptableVar, variableResponse);
        }

        /// <summary>
        /// Example debug logging to see who is listening to the variable.
        /// </summary>
        protected virtual void DebugLogVariableChange(ScriptableVariable<T> variable, VariableResponse<T> variableResponse)
        {
            var response = variableResponse.Response;
            var registeredListenerCount = response.GetPersistentEventCount();

            for (var i = 0; i < registeredListenerCount; i++)
            {
                var sb = new StringBuilder();
                sb.Append("<color=#f75369>[Var Changed] </color>");
                sb.Append(variable.name);
                sb.Append(" => ");
                sb.Append(response.GetPersistentTarget(i).name);
                sb.Append(".");
                sb.Append(response.GetPersistentMethodName(i));
                sb.Append("()");
                UnityEngine.Debug.Log(sb.ToString(), gameObject);
            }
        }

        /// <summary>
        /// Utility method to see if a persistent call to a given method name exists.
        /// </summary>
        public override bool ContainsCallToMethod(string methodName)
        {
            var containsMethod = false;
            foreach (var variableResponse in VariableResponses)
            {
                var registeredListenerCount = variableResponse.Response.GetPersistentEventCount();

                for (var i = 0; i < registeredListenerCount; i++)
                {
                    if (variableResponse.Response.GetPersistentMethodName(i) == methodName)
                    {
                        var sb = new StringBuilder();
                        sb.Append($"<color=#f75369>{methodName}()</color>");
                        sb.Append(" is called by: <color=#f75369>[Variable Changed] </color>");
                        sb.Append(variableResponse.ScriptableVariable.name);
                        UnityEngine.Debug.Log(sb.ToString(), gameObject);
                        containsMethod = true;
                        break;
                    }
                }
            }

            return containsMethod;
        }

        /// <summary>
        /// Container for a single ScriptableVariable reference and its associated delay/response.
        /// </summary>
        [Serializable]
        public class VariableResponse<U> : ISerializationCallbackReceiver
        {
            [Tooltip("The scriptable variable to listen to for OnValueChanged.")]
            public virtual ScriptableVariable<U> ScriptableVariable { get; }

            [Tooltip("Delay in seconds before invoking the response.")]
            public FloatReference Delay = new FloatReference(0, true);

            [Tooltip("The UnityEvent response invoked when the variable changes.")]
            public virtual UnityEvent<U> Response { get; }

            [NonSerialized]
            private bool _isInitialized;

            public void OnBeforeSerialize() { }

            public void OnAfterDeserialize()
            {
                // Set Delay to use local only if it hasn't been initialized yet
                if (!_isInitialized && Delay.Variable == null)
                {
                    Delay.UseLocal = true;
                    _isInitialized = true;
                }
            }
        }
    }
}