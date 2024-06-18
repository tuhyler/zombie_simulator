using UnityEngine;

public class InfoManager : MonoBehaviour
{
    [SerializeField]
    public UIInfoPanelUnit infoPanel;

    public void ShowInfoPanel(string name, UnitBuildDataSO data, int currentHealth, bool isTrader, int bonus, bool leader, bool player = false)//toggles it on, gets the info
    {
        //HideInfoPanel();
        infoPanel.ToggleVisibility(true, isTrader/*, isLaborer*/);
        string unitType; 
        if (leader)
            unitType = "Enemy Leader";
        else if (player)
            unitType = "Player";
        else
            unitType = data.unitType.ToString();
        infoPanel.SetData(name, data.unitLevel, unitType, currentHealth, data.health, data.movementSpeed, data.baseAttackStrength, data.cargoCapacity);
        infoPanel.SetStrengthBonus(bonus);
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

    public void UpdateStrengthBonus(int bonus)
    {
        infoPanel.SetStrengthBonus(bonus);
    }
}
