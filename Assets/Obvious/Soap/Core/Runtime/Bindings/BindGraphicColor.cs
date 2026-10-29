using UnityEngine;
using UnityEngine.UI;

namespace Obvious.Soap
{
    /// <summary>
    /// Binds a color variable to a graphic (works with UI and SpriteRenderer)
    /// </summary>
    [HelpURL("https://obvious-game.gitbook.io/soap/scene-documentation/2_bindings/bindgraphiccolor")]
    [AddComponentMenu("Soap/Bindings/BindGraphicColor")]
    [RequireComponent(typeof(Graphic))]
    public class BindGraphicColor : CacheComponent<Graphic>
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
            _component.color = color;
        }
    }
}