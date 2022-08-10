using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CanvasHandler : MonoBehaviour
{
    [SerializeField]
    private UINextButton nextButtonUI;
    [SerializeField]
    private UIWorkerHandler workerBuildUI;
    //[SerializeField]
    //private UIUnitTurnHandler turnHandler; //doesn't work here, only ends up turning it on after turning it off. 

    public void IsInteractable(bool v)
    {
        nextButtonUI.ToggleInteractable(v); //'1' for alpha (changing enabled doesn't work for me)

        //turnHandler.ToggleInteractable(v); 

        foreach (UIWorkerOptions wo in workerBuildUI.buildOptions)
        {
            wo.ToggleInteractable(v);
        }
    }
}
