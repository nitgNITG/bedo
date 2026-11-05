using System.Text;
using UnityEngine;
using TMPro;
using Obvious.Soap;

namespace Bedo.Integrations.Soap
{
    /// <summary>
    /// Binds an IntVariable to a TextMeshPro component, with:
    /// • optional multiplication
    /// • clamping
    /// • prefix and suffix
    /// </summary>
    [HelpURL("https://obvious-game.gitbook.io/soap/scene-documentation/2_bindings/bindtext-textmeshpro")]
    [AddComponentMenu("Soap/Bindings/BindIntTextMeshPro")]
    [RequireComponent(typeof(TMP_Text))]
    public class BindIntTextMeshPro : CacheComponent<TMP_Text>
    {
        [SerializeField] private IntVariable _intVariable = null;
        [SerializeField] private IntReference MultiplyBy = new IntReference(1, true);

        [Tooltip("Text to display before the value")] public string Prefix = string.Empty;
        [Tooltip("Text to display after the value")] public string Suffix = string.Empty;

        [Tooltip("Clamp the integer value between MinValue and MaxValue before display")] public bool UseClamp = false;
        [Tooltip("Minimum value when clamping")] public int MinValue = int.MinValue;
        [Tooltip("Maximum value when clamping")] public int MaxValue = int.MaxValue;

        private readonly StringBuilder _builder = new StringBuilder();
        private System.Action<int> _onValueChanged;

        protected override void Awake()
        {
            if (!enabled) return; // Restored legacy handlers stay inert while disabled.
            base.Awake();

            if (_intVariable == null)
            {
                Debug.LogError($"[{nameof(BindIntTextMeshPro)}] No IntVariable assigned on {gameObject.name}", gameObject);
                enabled = false;
                return;
            }

            UpdateText(_intVariable.Value);
            Subscribe();
        }

        private void Subscribe()
        {
            _onValueChanged = newValue => UpdateText(newValue);
            _intVariable.OnValueChanged += _onValueChanged;
        }

        private void UpdateText(int rawValue)
        {
            // 1) Multiply
            long multiplied = (long)rawValue * MultiplyBy.Value;

            // 2) Clamp if requested
            long clamped = UseClamp
                ? Mathf.Clamp((int)multiplied, MinValue, MaxValue)
                : multiplied;

            // 3) Build final string
            _builder.Clear()
                    .Append(Prefix)
                    .Append(clamped)
                    .Append(Suffix);

            _component.SetText(_builder);
        }

        private void OnDestroy()
        {
            if (!enabled) return; // Restored legacy handlers stay inert while disabled.
            if (_intVariable != null && _onValueChanged != null)
                _intVariable.OnValueChanged -= _onValueChanged;
        }
    }
}
