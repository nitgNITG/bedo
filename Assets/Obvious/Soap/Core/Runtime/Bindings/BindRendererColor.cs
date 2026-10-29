using UnityEngine;

namespace Obvious.Soap
{
    /// <summary>
    /// Binds a color variable to a renderer
    /// </summary>
    [HelpURL("https://obvious-game.gitbook.io/soap/scene-documentation/2_bindings/bindrenderercolor")]
    [AddComponentMenu("Soap/Bindings/BindRendererColor")]
    [RequireComponent(typeof(Renderer))]
    public class BindRendererColor : CacheComponent<Renderer>
    {
        [SerializeField] private ColorVariable _colorVariable = null;

        protected override void Awake()
        {
            if (!enabled) return; // Restored legacy handlers stay inert while disabled.
            base.Awake();
            Refresh(_colorVariable);
            _colorVariable.OnValueChanged += Refresh;
        }

        private void OnDestroy()
        {
            if (!enabled) return; // Restored legacy handlers stay inert while disabled.
            _colorVariable.OnValueChanged -= Refresh;
        }

        private void Refresh(Color color)
        {
            _component.material.color = color;
        }
    }
}