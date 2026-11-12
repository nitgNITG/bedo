using UnityEngine;
using TutorialFramework;


public class OnMouseDownEvent : MonoBehaviour
{

    private Interaction interaction;
    [SerializeField] private WindMonitorTabs windMonitorTabs;
    [SerializeField] private bool systemTab;



    private void Awake()
    {
        interaction =
        GetComponent<Interaction>();
    }



    private void OnMouseDown()
    {
        if (windMonitorTabs != null)
        {
            if (systemTab) windMonitorTabs.ShowSystem();
            else windMonitorTabs.ShowGraph(interaction);
            return;
        }
        if (interaction != null)
        {
            interaction.Click();
        }
    }
}
