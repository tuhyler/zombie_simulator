using UnityEngine;

public class InfoManager : MonoBehaviour
{
    [SerializeField]
    private UIInfoPanelUnit infoPanel;

    public void ShowInfoPanel(UnitBuildDataSO data, int currentHealth) //toggles it on, gets the info
    {
        //HideInfoPanel();
        infoPanel.ToggleVisibility(true);
        infoPanel.SetData(data.unitDisplayName, data.unitLevel, data.unitType.ToString(), currentHealth, data.health, data.movementSpeed, data.baseAttackStrength, data.cargoCapacity);
    }

    public void SetHealth(int currentHealth, int maxHealth)
    {
        infoPanel.SetHealth(currentHealth, maxHealth);
    }

    public void HideInfoPanel()
    {
        infoPanel.ToggleVisibility(false);
    }
}
