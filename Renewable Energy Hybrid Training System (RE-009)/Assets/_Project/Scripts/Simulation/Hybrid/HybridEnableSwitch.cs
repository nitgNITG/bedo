using UnityEngine;
using TutorialFramework;

public sealed class HybridEnableSwitch : MonoBehaviour
{
    public HybridTrainingController system;
    public StoryboardSteps storyboard;
    public SingleClickInteraction instruction;
    public int instructionIndex=-1;
    public Interaction sound;
    private Vector3 disabledPosition, enabledOffset;
    private void Awake(){disabledPosition=transform.localPosition;var renderer=GetComponent<Renderer>();float travel=renderer?renderer.bounds.size.x*.85f:1.45f;enabledOffset=transform.parent.InverseTransformVector(Vector3.left*travel);}
    private void LateUpdate(){if(system)transform.localPosition=disabledPosition+(system.SolarEnabled?enabledOffset:Vector3.zero);}
    private void OnMouseDown()=>Click();
    public void Click()
    {
        var manager=InteractionManager.Instance;
        if(!system||!manager||!system.MainPowered)return;
        if(manager.Free){sound?.PlaySound();system.ToggleSolarEnable();return;}
        if(instructionIndex<0||!manager.Guided)return;
        if(manager.Guided&&(!storyboard||!manager.IsExperimentActive(storyboard)||storyboard.CurrentStep<instructionIndex))return;
        sound?.PlaySound();system.ToggleSolarEnable();
        if(manager.Guided&&storyboard.CurrentStep==instructionIndex&&instruction)
        {
            if(instruction.marker)instruction.marker.SetActive(false);
            instruction.ForceComplete();
        }
    }
}
