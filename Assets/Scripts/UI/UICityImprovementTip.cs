using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class UICityImprovementTip : MonoBehaviour
{
    [SerializeField]
    private MapWorld world;
    
    [SerializeField]
    public TMP_Text title, level, resourceCount;
    private TMP_Text producesText, consumesNone;

    [SerializeField]
    private GameObject waitingForText, resourceCountGO;
    private bool waiting;

    [SerializeField]
    private Image improvementImage;//, produceHighlight;

    [SerializeField]
    private List<Image> highlightList = new();

    [SerializeField]
    private Transform producesRect, consumesRect;
    private int producesCount, maxCount, highlightIndex;

    private List<UIResourceInfoPanel> producesInfo = new(), consumesInfo = new();
    private List<int> produceTimeList = new();

    //cached improvement for turning off highlight
    private CityImprovement improvement;
    //private float xChange, yChange; //work around for produce highlight

    //for tweening
    [SerializeField]
    private RectTransform allContents, lineImage;
    private bool activeStatus;

    private void Awake()
    {
        transform.localScale = Vector3.zero;
        gameObject.SetActive(false);

        foreach (Transform selection in producesRect)
        {
            if (selection.TryGetComponent(out TMP_Text text))
            {
                producesText = text;
            }
            else
            {
                producesInfo.Add(selection.GetComponent<UIResourceInfoPanel>());
            }
        }
        foreach (Transform selection in consumesRect)
        {
            if (selection.TryGetComponent(out TMP_Text text))
            {
                consumesNone = text;
            }
            else
            {
                consumesInfo.Add(selection.GetComponent<UIResourceInfoPanel>());
            }
        }
    }

    public void ToggleVisibility(bool val, CityImprovement improvement = null)
    {
        if (activeStatus == val)
            return;

        LeanTween.cancel(gameObject);

        if (val)
        {
            world.cityBuilderManager.PlayAudioClip(improvement.GetImprovementData.audio);
            
            highlightList[highlightIndex].gameObject.SetActive(false); // turn off previous one
            this.improvement = improvement;

			if (world.GetResourceProducer(improvement.loc).isWaitingforResources)
			{
				waiting = true;
				waitingForText.SetActive(true);
			}
			else
			{
				waiting = false;
				waitingForText.SetActive(false);
			}

			SetData(this.improvement);
            this.improvement.EnableHighlight(Color.white);
            gameObject.SetActive(val);
            activeStatus = true;

            //setting up pop up location
            Vector3 p = Input.mousePosition;
            float x = 0.5f;
            float y = 0.5f;
            //xChange = 0;
            //yChange = 0;

            //p.z = 935;
            p.z = 1;
            if (p.y + allContents.rect.height * 0.5f > Screen.height)
            {
                y = 1f;
                //yChange = -217.5f;
            }
            else if (p.y - allContents.rect.height * 0.5f < 0)
            {
                y = 0f;
                //yChange = 217.5f;
            }

            if (p.x + allContents.rect.width * 0.5f > Screen.width)
            {
                x = 1f;
                //xChange = 155f;
            }
            else if (p.x - allContents.rect.width * 0.5f < 0)
            {
                x = 0f;
                //xChange = -155f;
            }

            allContents.pivot = new Vector2(x, y);
            Vector3 pos = Camera.main.ScreenToWorldPoint(p);
            allContents.transform.position = pos;

            LeanTween.scale(allContents, Vector3.one, 0.25f).setEaseLinear();
        }
        else
        {
            this.improvement.DisableHighlight();
            this.improvement = null;
            activeStatus = false;
            LeanTween.scale(allContents, Vector3.zero, 0.25f).setOnComplete(SetActiveStatusFalse);
        }
    }

    private void SetActiveStatusFalse()
    {
        gameObject.SetActive(false);
        world.infoPopUpCanvas.gameObject.SetActive(false);
    }

    private void SetData(CityImprovement improvement)
    {
        ImprovementDataSO data = improvement.GetImprovementData;
        ResourceProducer producer = world.GetResourceProducer(improvement.loc);
        producesCount = data.producedResources.Count;
        maxCount = producesCount;

        for (int i = 0; i < improvement.allConsumedResources.Count; i++)
        {
            if (improvement.allConsumedResources[i].Count > maxCount)
                maxCount = improvement.allConsumedResources[i].Count;
        }

        title.text = data.improvementDisplayName;
        if (data.improvementName == data.improvementDisplayName)
            level.text = "Level " + data.improvementLevel.ToString();
        else
            level.text = "Level " + data.improvementLevel.ToString() + " " + data.improvementName;
        improvementImage.sprite = data.image;
        produceTimeList = data.producedResourceTime;
        highlightIndex = improvement.producedResourceIndex;
        int producedTime = produceTimeList[highlightIndex];

        SetResourcePanelInfo(producesInfo, producer.producedResources, producedTime, true, data.workEthicChange);
        SetResourcePanelInfo(consumesInfo, improvement.allConsumedResources[highlightIndex], producedTime, false);

        if (data.getTerrainResource)
            highlightIndex = 0;

        highlightList[highlightIndex].gameObject.SetActive(true);

        bool showCount;

        if (data.rawMaterials && improvement.td.resourceAmount >= 0)
        {
            showCount = true;
            resourceCountGO.SetActive(true);
            resourceCount.text = improvement.td.resourceAmount.ToString();
        }
        else
        {
            showCount = false;
            resourceCountGO.SetActive(false);
        }

        int multiple = Mathf.Max(maxCount - 2, 0) * 90; //allowing one extra for production time ResourceValue
        int panelWidth = 310 + multiple;
        int panelHeight = showCount ? 525 : 450;
        int lineWidth = 280 + multiple;

        allContents.sizeDelta = new Vector2(panelWidth, panelHeight);
        lineImage.sizeDelta = new Vector2(lineWidth, 4);
    }

    private void SetResourcePanelInfo(List<UIResourceInfoPanel> panelList, List<ResourceValue> resourceList, int producedTime, bool produces, float workEthic = 0)
    {
        int resourcesCount = resourceList.Count;
        bool showText = false;
        if (workEthic > 0)
            showText = true;

        //show text for produces section
        if (produces)
        {
            producesCount = resourcesCount;
            
            if (showText)
            {
                //produceHighlight.gameObject.SetActive(false);
                
                producesText.gameObject.SetActive(true);
                if (workEthic > 0)
                    producesText.text = "Work Ethic +" + (workEthic * 100).ToString() + '%';

                foreach (UIResourceInfoPanel panel in panelList)
                    panel.gameObject.SetActive(false);

                return;
            }
            else
            {
                producesText.gameObject.SetActive(false);
            }
        }
        //show text for consumes section
        else
        {
            if (resourcesCount == 0)
            {
                consumesNone.gameObject.SetActive(true);

                foreach (UIResourceInfoPanel panel in panelList)
                    panel.gameObject.SetActive(false);

                return;
            }
            else
            {
                consumesNone.gameObject.SetActive(false);
            }
        }

        //int indexSelect = 0;

        for (int i = 0; i < panelList.Count; i++)
        {
            if (i > resourcesCount)
            {
                panelList[i].gameObject.SetActive(false);
            }
            else if (i == resourcesCount) //for adding time
            {
                if (!produces)
                {
                    panelList[i].gameObject.SetActive(true);
                    panelList[i].resourceAmountText.text = producedTime.ToString();
                    panelList[i].resourceType = ResourceType.Time;
                    panelList[i].resourceImage.sprite = ResourceHolder.Instance.GetIcon(ResourceType.Time);
                }
                else
                {
                    panelList[i].gameObject.SetActive(false);
                }
            }
            else
            {
                panelList[i].gameObject.SetActive(true);
                //panelList[i].resourceAmount.text = Mathf.RoundToInt(resourceList[i].resourceAmount * (60f / producedTime)).ToString();
                //panelList[i].SetResourceAmount(resourceList[i].resourceAmount);
                panelList[i].resourceAmountText.text = resourceList[i].resourceAmount.ToString();
                panelList[i].resourceType = resourceList[i].resourceType;
                panelList[i].resourceImage.sprite = ResourceHolder.Instance.GetIcon(resourceList[i].resourceType);

				if (waiting && resourceList[i].resourceAmount > improvement.meshCity.ResourceManager.ResourceDict[resourceList[i].resourceType])
					panelList[i].resourceAmountText.color = Color.red;
				else
					panelList[i].resourceAmountText.color = Color.white;
			}
        }

        //if (produces)
        //{
        //    highlightList[highlightIndex].gameObject.SetActive(true);
        //    //float xShiftLeft = (resourcesCount-1) * 45;
        //    //float xShiftRight = improvement.producedResourceIndex * 90;
        //    //xShiftRight -= 1.5f;

        //    //Vector2 loc = Vector2.zero;
        //    //loc.x -= xShiftLeft + xChange;
        //    //loc.x += xShiftRight;
        //    //loc.y = -40 + yChange;
        //    //produceHighlight.transform.localPosition = loc;
        //}
    }

    public void ChangeResourceProduced(int a)
    {
        if (highlightIndex == a)
            return;
        
        ResourceProducer producer = world.GetResourceProducer(improvement.loc);
        highlightList[producer.producedResourceIndex].gameObject.SetActive(false);
        if (producer.isProducing || producer.isWaitingForStorageRoom || producer.isWaitingforResources || producer.isWaitingToUnload)
            producer.StopProducing(true);

        improvement.producedResource = producesInfo[a].resourceType;
        improvement.producedResourceIndex = a;
        improvement.CalculateWorkCycleLimit();
        producer.producedResourceIndex = a;
        highlightIndex = a;
        producer.SetNewProgressTime();
        producer.producedResource = improvement.GetImprovementData.producedResources[a];
        producer.consumedResources = improvement.allConsumedResources[a];
        producer.SetConsumedResourceTypes();
        if (producer.currentLabor > 0)
        {
			producer.UpdateResourceGenerationData();
			producer.StartProducing();
        }

        SetResourcePanelInfo(consumesInfo, improvement.allConsumedResources[a], produceTimeList[a], false);

        highlightList[a].gameObject.SetActive(true);
        //float xShiftLeft = (producesCount - 1) * 45;
        //float xShiftRight = a * 90;
        ////xShiftRight -= 1.5f;

        ////Vector2 loc = producesInfo[a].transform.localPosition;
        //Vector2 loc = Vector2.zero;
        //loc.x -= xShiftLeft + xChange;
        //loc.x += xShiftRight;
        //loc.y = -40f + yChange;
        //produceHighlight.transform.localPosition = loc;

        if (world.cityBuilderManager.SelectedCity != null)
            world.cityBuilderManager.UpdateLaborNumbers();
    }

    public void ToggleWaiting(bool v)
    {
        if (activeStatus)
        {
            waiting = v;
            waitingForText.SetActive(v);

			for (int i = 0; i < improvement.allConsumedResources[highlightIndex].Count; i++)
            {
                if (v && improvement.allConsumedResources[highlightIndex][i].resourceAmount > improvement.meshCity.ResourceManager.ResourceDict[consumesInfo[i].resourceType])
                    consumesInfo[i].resourceAmountText.color = Color.red;
                else
				    consumesInfo[i].resourceAmountText.color = Color.white;
		    }
        }
    }

    public void UpdateResourceAmount(CityImprovement improvement)
    {
        if (activeStatus && improvement.GetImprovementData.rawMaterials && this.improvement == improvement && improvement.td.resourceAmount >= 0)
            resourceCount.text = improvement.td.resourceAmount.ToString();
    }

	internal void CloseCheck(CityImprovement improvement)
	{
        if (activeStatus && this.improvement == improvement)
            ToggleVisibility(false);
	}
}
