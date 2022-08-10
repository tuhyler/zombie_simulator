using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InfoManager : MonoBehaviour
{
    [SerializeField]
    private UIInfoPanelUnit infoPanel;

    //public void HandleSelection(GameObject selectedObject) //no longer used in playerinput, moved to UnitMovement
    //{
    //    HideInfoPanel(); //make sure its hidden first, to make a new one
    //    if (selectedObject == null)
    //        return;

    //    InfoProvider infoProvider = selectedObject.GetComponent<InfoProvider>(); //this is how we get the information associated with each unit
    //    if (infoProvider == null)
    //        return;
    //    ShowInfoPanel(infoProvider);
    //}

    public void ShowInfoPanel(InfoProvider infoProvider) //toggles it on, gets the info
    {
        //HideInfoPanel();
        infoProvider.UpdateInfo();
        infoPanel.ToggleVisibility(true);
        infoPanel.SetData(/*infoProvider.Image, */infoProvider.NameToDisplay, infoProvider.CurrentMovementPoints, infoProvider.RegMovementPoints);
    }

    public void HideInfoPanel()
    {
        infoPanel.ToggleVisibility(false);
    }

    //public void WaitTurn() //Use this interface to end turns, turning off everything
    //{
    //    //HideInfoPanel();
    //}
}
