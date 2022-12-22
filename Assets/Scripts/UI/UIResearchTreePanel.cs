using System.Collections;
using System.Collections.Generic;
using UnityEditor.Tilemaps;
using UnityEngine;

public class UIResearchTreePanel : MonoBehaviour
{
    [SerializeField]
    private MapWorld world;

    [SerializeField]
    private Transform uiElementsParent;

    [SerializeField]
    private UnitMovement unitMovement;

    [SerializeField]
    private CityBuilderManager cityBuilderManager;

    private UIResearchItem chosenResearchItem;
    private List<UIResearchItem> researchItemList = new();
    private int extraResearch;

    [SerializeField] //for tweening
    private RectTransform allContents;
    [HideInInspector]
    public bool activeStatus;
    private Vector3 originalLoc;

    private void Awake()
    {
        originalLoc = allContents.anchoredPosition3D;
        gameObject.SetActive(false);
        
        foreach (Transform transform in uiElementsParent)
        {
            UIResearchItem researchItem = transform.GetComponent<UIResearchItem>();
            researchItem.SetResearchTree(this);
            researchItemList.Add(researchItem);
        }    
    }

    public void ToggleVisibility(bool v)
    {
        if (activeStatus == v)
            return;

        LeanTween.cancel(gameObject);

        if (v)
        {
            cityBuilderManager.ResetCityUI();
            unitMovement.ClearSelection();
            
            gameObject.SetActive(v);

            foreach (UIResearchItem researchItem in researchItemList)
            {
                if (researchItem.ResearchReceived > 0 || researchItem == chosenResearchItem)
                    researchItem.UpdateProgressBar();
            }

            activeStatus = true;

            allContents.anchoredPosition3D = originalLoc + new Vector3(0, 1200f, 0);

            LeanTween.moveY(allContents, allContents.anchoredPosition3D.y + -1200f, 0.5f).setEaseOutSine();
            //LeanTween.alpha(allContents, 1f, 0.3f).setFrom(0f).setEaseLinear();
        }
        else
        {
            activeStatus = false;
            LeanTween.moveY(allContents, allContents.anchoredPosition3D.y + 1200f, 0.3f).setOnComplete(SetActiveStatusFalse);
        }
    }

    private void SetActiveStatusFalse()
    {
        gameObject.SetActive(false);
    }

    public void CloseResearchTree()
    {
        ToggleVisibility(false);
    }

    public void SetResearchItem(UIResearchItem researchItem)
    {
        if (chosenResearchItem != null)
        {
            chosenResearchItem.ChangeColor();
            if (chosenResearchItem.ResearchReceived == 0)
                chosenResearchItem.HideProgressBar();

            if (chosenResearchItem == researchItem)
            {
                world.researching = false;
                world.SetResearchName("No Current Research");
                return;
            }
        }

        world.researching = true;
        if (world.CitiesResearchWaitingCheck())
            world.RestartResearch();

        researchItem.ChangeColor();
        world.SetResearchName(researchItem.ResearchName);
        chosenResearchItem = researchItem;
        chosenResearchItem.UpdateProgressBar();
        if (extraResearch > 0)
            AddResearch(extraResearch);

        world.SetWorldResearchUI(chosenResearchItem.ResearchReceived, chosenResearchItem.totalResearchNeeded);
    }

    public void AddResearch(int amount)
    {
        int diff = chosenResearchItem.totalResearchNeeded - chosenResearchItem.ResearchReceived;
        extraResearch = 0;

        if (amount > diff)
        {
            extraResearch = amount - diff;
            chosenResearchItem.ResearchReceived = chosenResearchItem.totalResearchNeeded;
        }
        else
        {
            chosenResearchItem.ResearchReceived += amount;
        }

        if (activeStatus)
            chosenResearchItem.UpdateProgressBar();

        if (chosenResearchItem.ResearchReceived == chosenResearchItem.totalResearchNeeded)
        {
            chosenResearchItem.ResearchComplete(world);
            researchItemList.Remove(chosenResearchItem);
            chosenResearchItem = null;
        }
    }

    public string GetChosenResearchName()
    {
        return chosenResearchItem.ResearchName;
    }
}
