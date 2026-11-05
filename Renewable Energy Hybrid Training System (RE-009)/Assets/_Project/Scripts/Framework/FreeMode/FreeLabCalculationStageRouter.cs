using System.Collections.Generic;
using TutorialFramework;
using UnityEngine;

/// <summary>
/// Free Mode-only calculation selector. It reads the Main Board wiring configurations
/// and enables exactly the binder appropriate to the resulting Free Lab stage.
/// Series/parallel use authored FreeLabWireDefinition objects, never endpoint pairs.
/// </summary>
[DisallowMultipleComponent]
public sealed class FreeLabCalculationStageRouter : MonoBehaviour
{
    [Header("Shared Stage Requirements")]
    [SerializeField] private FreeLabControl mainBoardOnOff;
    [Tooltip("Zero-based Main Board Enable Configurations elements.")]
    [SerializeField] private List<int> stageOneConfigurations = new List<int> { 0, 1 };
    [SerializeField] private List<int> stageTwoConfigurations = new List<int> { 2, 3 };
    [SerializeField] private List<int> stageThreeConfigurations = new List<int> { 5 };

    [Header("Calculation Binders")]
    [SerializeField] private FreeLabPVCharacteristicBinder pvCharacteristicBinder;
    [Tooltip("Used only when Main Board Enable Configurations element 2 matches.")]
    [SerializeField] private FreeLabPVBatteryBinder batteryElementTwoBinder;
    [Tooltip("Used only when Main Board Enable Configurations element 3 matches.")]
    [SerializeField] private FreeLabPVBatteryBinder batteryElementThreeBinder;
    [SerializeField] private FreeLabPVOffGridBinder offGridBinder;

    [Header("Stage UI Objects")]
    [Tooltip("Shown when no later-stage requirements match, including the initial Free Lab state.")]
    [SerializeField] private GameObject stageOneObject;
    [SerializeField] private GameObject stageTwoObject;
    [SerializeField] private GameObject stageThreeObject;

    [Header("PV Characteristic Array Wiring")]
    [Tooltip("Assign the authored Free Mode wires equivalent to Guided Exp 2 (series). Every listed wire is required.")]
    [SerializeField] private List<FreeLabWireDefinition> seriesRequiredWires = new List<FreeLabWireDefinition>();
    [Tooltip("Assign the authored Free Mode wires equivalent to Guided Exp 3 (parallel). Every listed wire is required.")]
    [SerializeField] private List<FreeLabWireDefinition> parallelRequiredWires = new List<FreeLabWireDefinition>();

    [SerializeField, Tooltip("Play Mode only.")] private int activeStage;
    private int lastLoggedStage = -1;

    /// <summary>The currently selected Free Mode calculation stage.</summary>
    public int ActiveStage => activeStage;

    public void Evaluate(FreeLabManager lab)
    {
        if (lab == null || mainBoardOnOff == null) return;
        int stage = MatchesAny(lab, stageThreeConfigurations) ? 3 :
            MatchesAny(lab, stageTwoConfigurations) ? 2 :
            1;
        activeStage = stage;
        if (activeStage != lastLoggedStage)
        {
            lastLoggedStage = activeStage;
            Debug.Log($"PV Characteristic Binder state changed: State {activeStage} ({GetStateName(activeStage)}).", this);
        }
        SetActive(stageOneObject, stage == 1);
        SetActive(stageTwoObject, stage == 2);
        SetActive(stageThreeObject, stage == 3);
        SetEnabled(pvCharacteristicBinder, stage == 1);
        bool elementTwoMatches = stage == 2 &&
            mainBoardOnOff.MatchesEnableConfiguration(lab, 2);
        bool elementThreeMatches = stage == 2 &&
            mainBoardOnOff.MatchesEnableConfiguration(lab, 3);
        SetEnabled(batteryElementTwoBinder, elementTwoMatches);
        SetEnabled(batteryElementThreeBinder, elementThreeMatches);
        SetEnabled(offGridBinder, stage == 3);

        // The solar-panel control is the single source of truth for Free
        // Mode's PV output. Synchronising each Free Mode binder here means a
        // panel switch-off clears its solar readings immediately, regardless
        // of which calculation stage is active.
        bool solarPanelOn = lab.IsSolarPanelSourceOn();
        if (batteryElementTwoBinder != null)
            batteryElementTwoBinder.SetSolarPanelState(solarPanelOn);
        if (batteryElementThreeBinder != null)
            batteryElementThreeBinder.SetSolarPanelState(solarPanelOn);
        if (offGridBinder != null)
            offGridBinder.SetSolarPanelState(solarPanelOn);

        if (pvCharacteristicBinder != null && stage == 1)
        {
            bool series = MatchesWires(lab, seriesRequiredWires);
            bool parallel = MatchesWires(lab, parallelRequiredWires);
            pvCharacteristicBinder.SetArrayConfiguration(series, parallel);
            pvCharacteristicBinder.UseActiveFreeLabRheostat();
            pvCharacteristicBinder.RefreshDisplay();
        }
    }

    private bool MatchesAny(FreeLabManager lab, List<int> indices)
    {
        foreach (int index in indices)
            if (mainBoardOnOff.MatchesEnableConfiguration(lab, index)) return true;
        return false;
    }

    private static string GetStateName(int stage)
    {
        switch (stage)
        {
            case 1: return "PV Characteristic";
            case 2: return "Battery Charging";
            case 3: return "Off-Grid Inverter";
            default: return "Unknown";
        }
    }

    private static bool MatchesWires(FreeLabManager lab, List<FreeLabWireDefinition> wires)
    {
        if (wires == null || wires.Count == 0) return false;
        foreach (var wire in wires)
            if (wire == null || wire.endpointA == null || wire.endpointB == null ||
                !lab.HasConnection(wire.endpointA, wire.endpointB)) return false;
        return true;
    }

    private static void SetEnabled(Behaviour binder, bool enabled)
    {
        if (binder != null) binder.enabled = enabled;
    }

    private static void SetActive(GameObject target, bool active)
    {
        if (target != null) target.SetActive(active);
    }
}
