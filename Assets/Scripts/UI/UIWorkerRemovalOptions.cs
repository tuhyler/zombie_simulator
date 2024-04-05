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

            if (world.upgradeableUtilityMaxLevelDict[UtilityType.Power] > 0)
                howMuchToShow = 210;
            else if (world.upgradeableUtilityMaxLevelDict[UtilityType.Water] > 0)
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

                if (world.upgradeableUtilityMaxLevelDict[UtilityType.Power] > 0)
                    howMuchToMove = 220;
                else if (world.upgradeableUtilityMaxLevelDict[UtilityType.Water] > 0)
					howMuchToMove = 150;

				LeanTween.moveY(allContents, allContents.anchoredPosition3D.y - howMuchToMove, 0.2f).setOnComplete(SetActiveStatusFalse);
            }
        }
    }

    private void SetActiveStatusFalse()
    {
        gameObject.SetActive(false);
    }

    public void BuildRoad()
    {
		world.unitMovement.workerTaskManager.BuildRoadButton();
    }

    public void BuildWater()
    {
		world.unitMovement.workerTaskManager.BuildWaterButton();
    }

    public void BuildPower()
    {
		world.unitMovement.workerTaskManager.BuildPowerButton();
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
