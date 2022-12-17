using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InfoManager : MonoBehaviour
{
    [SerializeField]
    private UIInfoPanelUnit infoPanel;

    public void ShowInfoPanel(InfoProvider infoProvider) //toggles it on, gets the info
    {
        //HideInfoPanel();
        infoPanel.ToggleVisibility(true);
        infoPanel.SetData(/*infoProvider.Image, */infoProvider.NameToDisplay);
    }

    public void HideInfoPanel()
    {
        infoPanel.ToggleVisibility(false);
    }
}
