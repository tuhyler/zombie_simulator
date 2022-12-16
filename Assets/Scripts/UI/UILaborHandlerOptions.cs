using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UILaborHandlerOptions : MonoBehaviour
{
    [SerializeField]
    private TMP_Text resourceGeneration;

    [SerializeField]
    public Image resourceImage;

    [HideInInspector]
    public ResourceType resourceType;

    //for managing the labor icons
    [SerializeField]
    private Transform laborIconHolder;
    private UICityLaborIcon laborIcon10;
    private UICityLaborIcon laborIcon5;
    private List<UICityLaborIcon> laborIcons = new();
    private List<UICityLaborIcon> laborIconsOneList = new(); //to turn on the one icons individually (for speed)

    private bool isShowing;

    private void Awake()
    {
        foreach (Transform selection in laborIconHolder)
        {
            UICityLaborIcon laborIcon = selection.GetComponent<UICityLaborIcon>();
            laborIcon.ToggleVisibility(false);
            int size = laborIcon.size;
            laborIcons.Add(laborIcon);

            if (size == 10)
            {
                laborIcon10 = laborIcon;
                laborIcon10.infinite = true;
            }
            else if (size == 5)
                laborIcon5 = laborIcon;
            else
            {
                laborIcon.HideNumber();
                laborIconsOneList.Add(laborIcon);
            }
        }

        resourceGeneration.text = "+0";
        //goldLostIcon.gameObject.SetActive(false);
    }

    public void ToggleVisibility(bool v)
    {
        //if (!v)
        //    HideLaborIcons();
        
        gameObject.SetActive(v);
        isShowing = v;
    }

    //setting up which icons to show
    public void SetUICount(int count, float resourceGenerationNum)
    {
        resourceGeneration.text = $"+{Mathf.RoundToInt(resourceGenerationNum)}";

        if (count == 0)
            return;
        
        foreach (UICityLaborIcon laborIcon in laborIcons)
        {
            int size = laborIcon.size;

            if (count >= size)
            {
                laborIcon.ToggleVisibility(true);
                
                if (laborIcon.infinite)
                {
                    int total = (count / size) * size;
                    count -= total;
                    laborIcon.SetNumber(total);
                }
                else
                {
                    count -= size;
                }
            }
        }
    }

    public void AddSubtractUICount(int count, int laborChange, float resourceGenerationNum)
    {
        resourceGeneration.text = $"+{Mathf.RoundToInt(resourceGenerationNum)}";
        
        if (laborChange > 0)
        {
            if (count % 10 == 0)
            {
                laborIcon5.ToggleVisibility(false);
                foreach (UICityLaborIcon laborIcons in laborIconsOneList)
                    laborIcons.ToggleVisibility(false);
                laborIcon10.SetNumber((count / 10) * 10);
                laborIcon10.ToggleVisibility(true);
            }
            else if (count % 5 == 0)
            {
                foreach (UICityLaborIcon laborIcons in laborIconsOneList)
                    laborIcons.ToggleVisibility(false);
                laborIcon5.ToggleVisibility(true);
            }
            else
            {
                foreach (UICityLaborIcon laborIcons in laborIconsOneList)
                {
                    if (!laborIcons.isActive)
                    {
                        laborIcons.ToggleVisibility(true);
                        break; //just turn on one
                    }
                }
            }
        }
        else if (laborChange < 0)
        {
            if (count % 10 == 9)
            {
                if (count / 10 == 0)
                    laborIcon10.ToggleVisibility(false);
                else
                    laborIcon10.SetNumber((count / 10) * 10);

                laborIcon5.ToggleVisibility(true);
                foreach (UICityLaborIcon laborIcons in laborIconsOneList)
                    laborIcons.ToggleVisibility(true);
            }
            else if (count % 5 == 4)
            {
                laborIcon5.ToggleVisibility(false);

                foreach (UICityLaborIcon laborIcons in laborIconsOneList)
                    laborIcons.ToggleVisibility(true);
            }
            else
            {
                foreach (UICityLaborIcon laborIcons in laborIconsOneList)
                {
                    if (laborIcons.isActive)
                    {
                        laborIcons.ToggleVisibility(false);
                        break; //just turn off one
                    }
                }
            }
        }
    }

    public void HideLaborIcons()
    {
        if (isShowing)
        {
            foreach (UICityLaborIcon laborIcon in laborIcons)
            {
                if (laborIcon.isActive)
                    laborIcon.ToggleVisibility(false);
            }
        }
    }
}
