using UnityEngine;

public class InfoManager : MonoBehaviour
{
    [SerializeField]
    private UIInfoPanelUnit infoPanel;

    public void ShowInfoPanel(string name, UnitBuildDataSO data, int currentHealth, bool isTrader, bool isLaborer) //toggles it on, gets the info
    {
        //HideInfoPanel();
        infoPanel.ToggleVisibility(true, isTrader, isLaborer);
        infoPanel.SetData(name, data.unitLevel, data.unitType.ToString(), currentHealth, data.health, data.movementSpeed, data.baseAttackStrength, data.cargoCapacity);
    }

    public void SetHealth(int currentHealth, int maxHealth)
    {
        infoPanel.SetHealth(currentHealth, maxHealth);
    }

    public void HideInfoPanel()
    {
        infoPanel.ToggleVisibility(false);
    }

    public void UpdateName(string newName)
    {
        infoPanel.unitName.text = newName;
    }
}
