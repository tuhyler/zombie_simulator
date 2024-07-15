using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UILaborHandlerOptions : MonoBehaviour
{
    [SerializeField]
    public UIResourceInfoPanel resourcePanel;
    
    [SerializeField]
    private TMP_Text laborCount;

    //[SerializeField]
    //public Image resourceImage;

    [HideInInspector]
    public ResourceType resourceType;

    [HideInInspector]
    public float generation;

    //for managing the labor icons
    //[SerializeField]
    //private Transform laborIconHolder;
    //private UICityLaborIcon laborIcon10;
    ////private UICityLaborIcon laborIcon5;
    //private List<UICityLaborIcon> laborIcons = new();
    //private List<UICityLaborIcon> laborIconsOneList = new(); //to turn on the one icons individually (for speed)

    [HideInInspector]
    public bool isShowing;

    private void Awake()
    {
        //foreach (Transform selection in laborIconHolder)
        //{
        //    UICityLaborIcon laborIcon = selection.GetComponent<UICityLaborIcon>();
        //    laborIcon.ToggleVisibility(false);
        //    laborIcons.Add(laborIcon);

        //    if (laborIcon.infinite)
        //    {
        //        laborIcon10 = laborIcon;
        //        laborIcon10.infinite = true;
        //    }
        //    else
        //    {
        //        laborIcon.HideNumber();
        //        laborIconsOneList.Add(laborIcon);
        //    }
        //}

  //      resourceGeneration.text = "+0";
		//resourceGeneration.outlineColor = Color.black;
		//resourceGeneration.outlineWidth = .3f;

		laborCount.outlineColor = Color.black;
		laborCount.outlineWidth = 0.3f;
	}

    public void ToggleVisibility(bool v)
    {
        gameObject.SetActive(v);
        isShowing = v;
    }

    //setting up which icons to show
    public void SetUICount(int count, float resourceGenerationNum)
    {
        if (count == 0)
            return;

        resourcePanel.resourceAmountText.text = $"+{Mathf.RoundToInt(resourceGenerationNum)}";
        generation = resourceGenerationNum;
        laborCount.text = count.ToString();
        
        //foreach (UICityLaborIcon laborIcon in laborIcons)
        //{
        //    int size = laborIcon.size;

        //    if (count >= size)
        //    {
        //        laborIcon.ToggleVisibility(true);
                
        //        if (laborIcon.infinite)
        //        {
        //            int total = (count / size) * size;
        //            count -= total;
        //            laborIcon.SetNumber(total);
        //        }
        //        else
        //        {
        //            count -= size;
        //        }
        //    }
        //}
    }

    public void AddSubtractUICount(int count, int laborChange, float resourceGenerationNum)
    {
        resourcePanel.resourceAmountText.text = $"+{Mathf.RoundToInt(resourceGenerationNum)}";
        generation = resourceGenerationNum;
        laborCount.text = count.ToString();
        
        //if (laborChange > 0)
        //{
        //    if (count % 5 == 0)
        //    {
        //        foreach (UICityLaborIcon laborIcons in laborIconsOneList)
        //            laborIcons.ToggleVisibility(false);
        //        laborIcon10.SetNumber(count);
        //        laborIcon10.ToggleVisibility(true);
        //    }
        //    else
        //    {
        //        foreach (UICityLaborIcon laborIcons in laborIconsOneList)
        //        {
        //            if (!laborIcons.isActive)
        //            {
        //                laborIcons.ToggleVisibility(true);
        //                break; //just turn on one
        //            }
        //        }
        //    }
        //}
        //else if (laborChange < 0)
        //{
        //    if (count % 5 == 4)
        //    {
        //        if (count < 5)
        //            laborIcon10.ToggleVisibility(false);
        //        else
        //            laborIcon10.SetNumber(count / 5 * 5);

        //        foreach (UICityLaborIcon laborIcons in laborIconsOneList)
        //            laborIcons.ToggleVisibility(true);
        //    }
        //    else
        //    {
        //        for (int i = laborIconsOneList.Count - 1; i >= 0; i--)
        //        {
        //            if (laborIconsOneList[i].isActive)
        //            {
        //                laborIconsOneList[i].ToggleVisibility(false);
        //                break;
        //            }
        //        }
        //    }
        //}
    }

    public void UpdateResourceGenerationNumbers(float resourceGenerationNum)
    {
		resourcePanel.resourceAmountText.text = $"+{Mathf.RoundToInt(resourceGenerationNum)}";
        generation = resourceGenerationNum;
	}

    //public void HideLaborIcons() //1 in uilaborhandler
    //{
    //    if (isShowing)
    //    {
    //        foreach (UICityLaborIcon laborIcon in laborIcons)
    //        {
    //            if (laborIcon.isActive)
    //                laborIcon.ToggleVisibility(false);
    //        }
    //    }
    //}
}
