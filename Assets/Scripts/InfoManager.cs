using UnityEngine;

public class InfoManager : MonoBehaviour
{
    [SerializeField]
    private UIInfoPanelUnit infoPanel;

    public void ShowInfoPanel(UnitBuildDataSO data, int currentHealth) //toggles it on, gets the info
    {
        //HideInfoPanel();
        infoPanel.ToggleVisibility(true);
        infoPanel.SetData(data.unitName, data.unitLevel, data.unitType.ToString(), currentHealth, data.health, data.movementSpeed, data.attackStrength, data.cargoCapacity);
    }

    public void HideInfoPanel()
    {
        infoPanel.ToggleVisibility(false);
    }
}
