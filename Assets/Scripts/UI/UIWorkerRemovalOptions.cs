using UnityEngine;

public class UIWorkerRemovalOptions : MonoBehaviour
{
    [SerializeField]
    public MapWorld world;
    
    [SerializeField]
    private GameObject /*allButton, */roadButton, liquidButton, powerButton;

    //for tweening
    [SerializeField]
    private RectTransform allContents;
    private Vector3 originalLoc;
    private bool activeStatus; //set this up so we don't have to wait for tween to set inactive

    private void Awake()
    {
        //allButton.SetActive(false);
        //liquidButton.SetActive(false);
        //powerButton.SetActive(false);

        gameObject.SetActive(false);
        originalLoc = allContents.anchoredPosition3D;
    }

    public void ToggleVisibility(bool val, bool suddenly) //pass resources to know if affordable in the UI (optional)
    {
        if (activeStatus == val)
            return;

        LeanTween.cancel(gameObject);

        if (val)
        {
            gameObject.SetActive(val);
            activeStatus = true;

            int howMuchToShow = 70;

            if (world.powerResearched)
                howMuchToShow = 210;
            else if (world.waterResearched)
                howMuchToShow = 140;

            //allContents.anchoredPosition3D = originalLoc + new Vector3(0, -howMuchToShow, 0);
            allContents.anchoredPosition3D = originalLoc;
            LeanTween.moveY(allContents, allContents.anchoredPosition3D.y + howMuchToShow, 0.4f).setEaseOutSine();
        }
        else
        {
            activeStatus = false;
            if (suddenly)
            {
                LeanTween.moveY(allContents, allContents.anchoredPosition3D.y - 240f, 0.2f).setOnComplete(SetActiveStatusFalse);
            }
            else
            {
                int howMuchToMove = 80;

				if (world.powerResearched)
					howMuchToMove = 220;
				else if (world.waterResearched)
					howMuchToMove = 150;

				LeanTween.moveY(allContents, allContents.anchoredPosition3D.y - howMuchToMove, 0.2f).setOnComplete(SetActiveStatusFalse);
            }
        }
    }

    private void SetActiveStatusFalse()
    {
        gameObject.SetActive(false);
    }

    public void RemoveAll()
    {

    }

    public void RemoveRoad()
    {
        world.unitMovement.workerTaskManager.RemoveRoadPrep();
    }

    public void RemoveLiquid()
    {
		world.unitMovement.workerTaskManager.RemoveLiquidPrep();
	} 

    public void RemovePower()
    {
		world.unitMovement.workerTaskManager.RemovePowerPrep();
	}
}
