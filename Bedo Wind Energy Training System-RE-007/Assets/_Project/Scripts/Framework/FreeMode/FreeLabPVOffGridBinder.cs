/// <summary>Free Mode component counterpart for Off Grid calculations.</summary>
public sealed class FreeLabPVOffGridBinder : PVOffGridSystemBinder
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
