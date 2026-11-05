/// <summary>Free Mode component counterpart for battery-system calculations.</summary>
public sealed class FreeLabPVBatteryBinder : PVBatterySystemBinder
{
    /// <summary>Keeps the Free Mode display tied to the physical panel switch.</summary>
    public void SetSolarPanelState(bool isOn)
    {
        if (isOn)
            TurnPanelOn();
        else
            TurnPanelOff();
    }
}
