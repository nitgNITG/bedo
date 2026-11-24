using UnityEngine;
using TutorialFramework;

[RequireComponent(typeof(MeshRenderer))]
public sealed class PowerIndicatorFakeGlow : MonoBehaviour
{
    [SerializeField] private Renderer offStateCover;
    [SerializeField] private bool freeModeControlsObject;

    private MeshRenderer glow;

    private void Awake()
    {
        glow = GetComponent<MeshRenderer>();
    }

    private void OnEnable() => RefreshVisibility();

    private void LateUpdate() => RefreshVisibility();

    private void RefreshVisibility()
    {
        var manager = InteractionManager.Instance;
        glow.enabled = (freeModeControlsObject && manager != null && manager.Free) ||
            (offStateCover && offStateCover.gameObject.activeInHierarchy && !offStateCover.enabled);
    }
}
