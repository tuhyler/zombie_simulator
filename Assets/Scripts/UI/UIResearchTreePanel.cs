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

    private UIResearchItem chosenResearchItem;

    [SerializeField] //for tweening
    private RectTransform allContents;
    [HideInInspector]
    public bool activeStatus;
    private Vector3 originalLoc;

    private void Awake()
    {
        gameObject.SetActive(false);
        
        foreach (Transform transform in uiElementsParent)
        {
            transform.GetComponent<UIResearchItem>().SetResearchTree(this);
        }    
    }

    public void ToggleVisibility(bool v)
    {
        if (activeStatus == v)
            return;

        LeanTween.cancel(gameObject);

        if (v)
        {
            gameObject.SetActive(v);

            activeStatus = true;

            allContents.anchoredPosition3D = originalLoc + new Vector3(0, 1200f, 0);

            LeanTween.moveY(allContents, allContents.anchoredPosition3D.y + -1200f, 0.5f).setEaseOutSine();
            LeanTween.alpha(allContents, 1f, 0.3f).setFrom(0f).setEaseLinear();
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
            chosenResearchItem.ChangeColor();

        researchItem.ChangeColor();
        world.SetResearchName(researchItem.ResearchName);
        chosenResearchItem = researchItem;
    }
}
