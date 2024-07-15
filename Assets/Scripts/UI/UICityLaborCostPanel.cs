using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class UICityLaborCostPanel : MonoBehaviour
{
    [SerializeField]
    private Transform laborCostScrollRect;

    //[SerializeField]
    //private Image openCostsImage;
    //[SerializeField]
    //private Sprite buttonRight;
    //private Sprite buttonLeft;
    private Color originalColor;

    [SerializeField]
    private GameObject uiResourceInfoPanel;
    private List<UIResourceInfoPanel> resourceOptions = new();
    private Dictionary<ResourceType, UIResourceInfoPanel> resourceOptionsDict = new();

    [SerializeField] //for tweening
    private RectTransform allContents;
    [HideInInspector]
    public bool activeStatus, isOpen;
    private Vector3 originalLoc;

    private void Awake()
    {
        originalLoc = allContents.anchoredPosition3D;
        gameObject.SetActive(false);
        //buttonLeft = openCostsImage.sprite;
        //originalColor = openCostsImage.color;
        isOpen = true;
		CreateLaborCostResource(ResourceType.Food);
        CreateLaborCostResource(ResourceType.Gold);

   //     foreach (ResourceIndividualSO resource in ResourceHolder.Instance.allStorableResources.Concat(ResourceHolder.Instance.allWorldResources))
   //     {
   //         if (resource.resourceType == ResourceType.Research)
   //             continue;

   //         GameObject resourceOptionGO = Instantiate(uiResourceInfoPanel, laborCostScrollRect);

   //         if (resource.resourceType == ResourceType.Gold)
   //             resourceOptionGO.transform.SetSiblingIndex(0);
   //         else if (resource.resourceType == ResourceType.Food)
			//	resourceOptionGO.transform.SetSiblingIndex(0);

			//UIResourceInfoPanel resourceOption = resourceOptionGO.GetComponent<UIResourceInfoPanel>();
   //         RectTransform goSizing = resourceOption.GetComponent<RectTransform>();
   //         goSizing.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 80);
   //         goSizing.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 80);
   //         RectTransform imageSizing = resourceOption.image.GetComponent<RectTransform>();
   //         imageSizing.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 70);
   //         imageSizing.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 70);
   //         RectTransform resourceImageSizing = resourceOption.resourceImage.GetComponent<RectTransform>();
   //         resourceImageSizing.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 55);
   //         resourceImageSizing.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 55);

   //         resourceOption.resourceImage.sprite = resource.resourceIcon;
   //         resourceOption.SetResourceType(resource.resourceType);
   //         resourceOption.resourceAmountText.color = Color.red;
   //         resourceOption.gameObject.SetActive(false);

   //         //resourceOptions.Add(resourceOption);
   //         resourceOptionsDict[resourceOption.resourceType] = resourceOption;
   //     }
    }

    public void CreateLaborCostResource(ResourceType type)
    {
		if (!resourceOptionsDict.ContainsKey(type))
        {
            GameObject resourceOptionGO = Instantiate(uiResourceInfoPanel, laborCostScrollRect);

		    UIResourceInfoPanel resourceOption = resourceOptionGO.GetComponent<UIResourceInfoPanel>();
		    //RectTransform goSizing = resourceOption.GetComponent<RectTransform>();
		    //goSizing.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 80);
		    //goSizing.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 80);
		    //RectTransform imageSizing = resourceOption.image.GetComponent<RectTransform>();
		    //imageSizing.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 70);
		    //imageSizing.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 70);
		    //RectTransform resourceImageSizing = resourceOption.resourceImage.GetComponent<RectTransform>();
		    //resourceImageSizing.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 55);
		    //resourceImageSizing.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 55);

            resourceOption.resourceImage.sprite = ResourceHolder.Instance.GetIcon(type);
		    resourceOption.SetResourceType(type);
		    resourceOption.resourceAmountText.color = Color.red;
		    resourceOption.gameObject.SetActive(false);

		    resourceOptionsDict[resourceOption.resourceType] = resourceOption;
        }
	}

    public void ToggleVisibility(bool v, bool suddenly)
    {
        if (activeStatus == v)
            return;

        LeanTween.cancel(gameObject);

        if (v)
        {
            gameObject.SetActive(true);
            activeStatus = true;

            if (suddenly)
            {
                allContents.anchoredPosition3D = originalLoc + new Vector3(600f, 0, 0);
                LeanTween.moveX(allContents, allContents.anchoredPosition3D.x - 600f, 0.3f).setEaseOutSine();
            }
            else
            {
                //openCostsImage.sprite = buttonRight;
                //openCostsImage.color = Color.green;
                isOpen = true;

                allContents.anchoredPosition3D = originalLoc + new Vector3(150f, 0, 0);
                LeanTween.moveX(allContents, allContents.anchoredPosition3D.x - 150f, 0.3f).setEaseOutSine();
            }

            //LeanTween.alpha(allContents, 1f, 0.3f).setFrom(0f).setEaseLinear();
        }
        else
        {
            activeStatus = false;
            //when closing the entire labor menu
            if (suddenly)
            {
                LeanTween.moveX(allContents, allContents.anchoredPosition3D.x + 600f, 0.2f).setOnComplete(SetActiveStatusFalse);
            }
            else
            {
                //openCostsImage.sprite = buttonLeft;
                //openCostsImage.color = originalColor;
                isOpen = false;
                LeanTween.moveX(allContents, allContents.anchoredPosition3D.x + 150f, 0.2f).setOnComplete(SetActiveStatusFalse);
            }
        }
    }

    private void SetActiveStatusFalse()
    {
        gameObject.SetActive(false);
    }

    public void ResetUI()
    {
        foreach (UIResourceInfoPanel resource in resourceOptions)
        {
            resource.gameObject.SetActive(false);
        }

        resourceOptions.Clear();
    }

    public void SetConsumedResourcesInfo(Dictionary<ResourceType, float> consumedResourcesDict)
    {
        Dictionary<ResourceType, float> costsDict = new();
        
        foreach (ResourceType resourceType in consumedResourcesDict.Keys)
        {
            if (consumedResourcesDict[resourceType] == 0)
            {
				resourceOptionsDict[resourceType].gameObject.SetActive(false);
				continue;
            }
            
            resourceOptionsDict[resourceType].gameObject.SetActive(true);
            resourceOptions.Add(resourceOptionsDict[resourceType]);
            resourceOptionsDict[resourceType].SetNegativeAmount(consumedResourcesDict[resourceType]);
            costsDict[resourceType] = consumedResourcesDict[resourceType];
        }

        SortConsumedResourcesInfo(costsDict);
    }

    public void SortConsumedResourcesInfo(Dictionary<ResourceType, float> costsDict)
    {
		int listCount = resourceOptions.Count;

		for (int i = 0; i < listCount; i++)
		{
			for (int j = i + 1; j < listCount; j++)
			{
				if ((resourceOptions[j].gameObject.activeSelf && costsDict[resourceOptions[j].resourceType] > costsDict[resourceOptions[i].resourceType]) || 
                    (resourceOptions[j].gameObject.activeSelf && !resourceOptions[i].gameObject.activeSelf))
				{
					UIResourceInfoPanel oldPanel = resourceOptions[j];
					resourceOptions.RemoveAt(j);
					resourceOptions.Insert(i, oldPanel);
				}
			}
		}

		for (int i = 0; i < resourceOptions.Count; i++)
			resourceOptions[i].transform.SetSiblingIndex(i);

		if (resourceOptionsDict[ResourceType.Gold].gameObject.activeSelf)
			resourceOptionsDict[ResourceType.Gold].transform.SetSiblingIndex(0);

		if (resourceOptionsDict[ResourceType.Food].gameObject.activeSelf)
            resourceOptionsDict[ResourceType.Food].transform.SetSiblingIndex(0);
	}

    public void UpdateConsumedResources(List<ResourceType> consumedResourceTypes, Dictionary<ResourceType, float> consumedResourcesDict)
    {
        foreach (ResourceType resourceType in consumedResourceTypes)
        {
            if (consumedResourcesDict[resourceType] > 0)
            {
                if (!resourceOptions.Contains(resourceOptionsDict[resourceType]))
                {
                    resourceOptionsDict[resourceType].gameObject.SetActive(true);
                    resourceOptions.Add(resourceOptionsDict[resourceType]);
                }

                resourceOptionsDict[resourceType].SetNegativeAmount(consumedResourcesDict[resourceType]);
            }
            else if (consumedResourcesDict[resourceType] == 0 && resourceOptions.Contains(resourceOptionsDict[resourceType]))
            {
                resourceOptionsDict[resourceType].gameObject.SetActive(false);
                resourceOptions.Remove(resourceOptionsDict[resourceType]);
            }
        }
    }
}
