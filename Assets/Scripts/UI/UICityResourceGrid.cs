using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class UICityResourceGrid : MonoBehaviour
{
    //public Transform gridHolder;
    //private Dictionary<int, UIDropLocation> gridCellDict = new();
    //private int activeCells;

    //private List<ResourceIndividualSO> resourceInfo;

    //private City city;

    ////for tweening
    //[SerializeField]
    //private RectTransform allContents;
    //private Vector3 originalLoc;
    //[HideInInspector]
    //public bool activeStatus;


    //private void Awake()
    //{
    //    gameObject.SetActive(false);

    //    int total = 0;
    //    foreach (Transform selection in gridHolder)
    //    {
    //        UIDropLocation loc = selection.GetComponent<UIDropLocation>();
    //        loc.gameObject.SetActive(false);
    //        loc.SetUICityResourceGrid(this);
    //        loc.gridLocation = total;
    //        loc.resource.loc = total;
    //        gridCellDict.Add(loc.gridLocation, loc);
    //        total++;
    //    }

    //    resourceInfo = ResourceHolder.Instance.allStorableResources.Concat(ResourceHolder.Instance.allWorldResources).ToList();
    //}

    //public void ToggleVisibility(bool val, City city = null) 
    //{
    //    if (activeStatus == val)
    //        return;

    //    LeanTween.cancel(gameObject);

    //    if (val)
    //    {
    //        this.city = city;
    //        activeCells = 0;

    //        gameObject.SetActive(val);
    //        activeStatus = true;
    //        allContents.anchoredPosition3D = originalLoc + new Vector3(0, 200f, 0);

    //        LeanTween.moveY(allContents, allContents.anchoredPosition3D.y - 200f, 0.3f).setEaseOutSine();

    //        foreach (ResourceType type in city.resourceGridDict.Keys)
    //        {
    //            int index = resourceInfo.FindIndex(a => a.resourceType == type);
    //            int loc = city.resourceGridDict[type];
    //            gridCellDict[loc].gameObject.SetActive(true);
    //            activeCells++;

    //            gridCellDict[loc].resource.resourceType = type;
    //            gridCellDict[loc].resource.SetValue(city.ResourceManager.ResourceDict[type]);
    //            gridCellDict[loc].resource.resourceImage.sprite = resourceInfo[index].resourceIcon;
    //        }
    //    }
    //    else
    //    {
    //        for (int i = 0; i < activeCells; i++)
    //        {
    //            gridCellDict[i].gameObject.SetActive(false);
    //        }

    //        city = null;
            
    //        activeStatus = false;
    //        LeanTween.moveY(allContents, allContents.anchoredPosition3D.y + 200f, 0.2f).setOnComplete(SetActiveStatusFalse);
    //    }
    //}

    //private void SetActiveStatusFalse()
    //{
    //    gameObject.SetActive(false);
    //}

    //public void UpdateDict(int oldNum, int newNum, ResourceType type)
    //{
    //    //shifting all the other resources
    //    if (oldNum > newNum)
    //    {
    //        for (int i = oldNum; i > newNum; i--)
    //        {
    //            int next = i - 1;
    //            gridCellDict[next].MoveResource(gridCellDict[i]);
    //            city.resourceGridDict[gridCellDict[i].resource.resourceType] = i;
    //        }
    //    }
    //    else
    //    {
    //        for (int i = oldNum; i < newNum; i++)
    //        {
    //            int next = i + 1;
    //            gridCellDict[next].MoveResource(gridCellDict[i]);
    //            city.resourceGridDict[gridCellDict[i].resource.resourceType] = i;
    //        }
    //    }

    //    city.resourceGridDict[type] = newNum;
    //}
}
