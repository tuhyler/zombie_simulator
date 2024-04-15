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
    public TMP_Text title, level, resourceCount, waitingForText;
    private TMP_Text /*producesText, */consumesNone;

    [SerializeField]
    private GameObject /*waitingForText, */resourceCountGO;
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
    [HideInInspector]
    public bool activeStatus;

    private void Awake()
    {
        transform.localScale = Vector3.zero;
        gameObject.SetActive(false);

        foreach (Transform selection in producesRect)
        {
            producesInfo.Add(selection.GetComponent<UIResourceInfoPanel>());
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

    //public void HandleEsc()
    //{
    //    if (activeStatus)
    //        world.CloseImprovementTooltipCloseButton();
    //}

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
            ResourceProducer producer = improvement.resourceProducer;

			if (producer.isWaitingforResources)
			{
				waiting = true;
				waitingForText.gameObject.SetActive(true);
                waitingForText.text = "Waiting for Resources";
			}
            else if (improvement.GetImprovementData.isResearch)
            {
				if (producer.isWaitingForResearch || producer.isWaitingToUnload)
                {
                    waiting = true;
				    waitingForText.gameObject.SetActive(true);
				    waitingForText.text = "Waiting for Assignment";
                }
                else
                {
					waiting = false;
					waitingForText.gameObject.SetActive(false);
				}
			}
            else if (producer.isWaitingForStorageRoom || producer.isWaitingToUnload)
            {
				waiting = true;
				waitingForText.gameObject.SetActive(true);
				waitingForText.text = "Waiting for Storage";
			}
			else
			{
				waiting = false;
				waitingForText.gameObject.SetActive(false);
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

            p.z = 935;
            //p.z = 1;
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
        ResourceProducer producer = improvement.resourceProducer;
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

        float workEthic;
        if (improvement.city == null)
            workEthic = 1;
        else
            workEthic = improvement.city.workEthic;

        SetResourcePanelInfo(producesInfo, producer.producedResources, producedTime, true, producer.isProducing, workEthic);
        SetResourcePanelInfo(consumesInfo, improvement.allConsumedResources[highlightIndex], producedTime, false);

        if (data.getTerrainResource)
            highlightIndex = 0;

        highlightList[highlightIndex].gameObject.SetActive(true);

        bool showCount;

        if (data.rawMaterials && improvement.td.resourceAmount >= 0)
        {
            showCount = true;
            resourceCountGO.SetActive(true);
            SetResourceCount(improvement.td.resourceAmount);
        }
        else
        {
            showCount = false;
            resourceCountGO.SetActive(false);
        }

        int multiple = Mathf.Max(maxCount - 2, 0) * 90; //allowing one extra for production time ResourceValue
        int panelWidth = 310 + multiple;
        int panelHeight = showCount ? 535 : 460;
        int lineWidth = 280 + multiple;

        allContents.sizeDelta = new Vector2(panelWidth, panelHeight);
        lineImage.sizeDelta = new Vector2(lineWidth, 4);
    }

    private void SetResourcePanelInfo(List<UIResourceInfoPanel> panelList, List<ResourceValue> resourceList, int producedTime, bool produces, bool producing = false, float workEthic = 1)
    {
        int resourcesCount = resourceList.Count;
        //bool showText = false;
        //if (workEthic > 0)
        //    showText = true;

        //show text for produces section
        if (produces)
        {
            producesCount = resourcesCount;
            


            //if (showText)
            //{
            //    //produceHighlight.gameObject.SetActive(false);
                
            //    producesText.gameObject.SetActive(true);

            //    foreach (UIResourceInfoPanel panel in panelList)
            //        panel.gameObject.SetActive(false);

            //    return;
            //}
            //else
            //{
            //    producesText.gameObject.SetActive(false);
            //}
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
                    panelList[i].SetResourceAmount(producedTime);
                    panelList[i].SetResourceType(ResourceType.Time);
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
                panelList[i].SetResourceType(resourceList[i].resourceType);
                panelList[i].resourceImage.sprite = ResourceHolder.Instance.GetIcon(resourceList[i].resourceType);

                if (produces)
                {
                    workEthic = producing ? workEthic : 1;
                    int amount = resourceList[i].resourceAmount;
                    int newAmount = Mathf.RoundToInt(amount * (workEthic + world.GetResourceTypeBonus(resourceList[i].resourceType)));

					panelList[i].SetResourceAmount(newAmount);

                    if (amount == newAmount)
						panelList[i].resourceAmountText.color = Color.white;
                    else if (newAmount > amount)
						panelList[i].resourceAmountText.color = Color.green;
                    else
						panelList[i].resourceAmountText.color = Color.red;
				}
                else
                {
					panelList[i].SetResourceAmount(resourceList[i].resourceAmount);

                    if (waiting)
                    {
                        if (resourceList[i].resourceType == ResourceType.Gold)
                        {
                            if (world.CheckWorldGold(resourceList[i].resourceAmount))
								panelList[i].resourceAmountText.color = Color.white;
                            else
                                panelList[i].resourceAmountText.color = Color.red;
						}
                        else if (resourceList[i].resourceAmount > improvement.meshCity.ResourceManager.ResourceDict[resourceList[i].resourceType])
                        {
                            panelList[i].resourceAmountText.color = Color.red;
                        }
                        else
                        {
							panelList[i].resourceAmountText.color = Color.white;
                        }
					}
                    else
                    {
						panelList[i].resourceAmountText.color = Color.white;
					}
                }
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
        
        ResourceProducer producer = improvement.resourceProducer;
        highlightList[producer.producedResourceIndex].gameObject.SetActive(false);
        if (producer.isProducing || producer.isWaitingForStorageRoom || producer.isWaitingforResources || producer.isWaitingToUnload)
            producer.StopProducing(true);

        improvement.producedResource = producesInfo[a].resourceType;
        improvement.producedResourceIndex = a;
        //improvement.CalculateWorkCycleLimit();
        producer.producedResourceIndex = a;
        highlightIndex = a;
        producer.SetNewProgressTime();
        producer.producedResource = producer.producedResources[a];
        producer.consumedResources = improvement.allConsumedResources[a];
        producer.SetConsumedResourceTypes();
        if (producer.currentLabor > 0)
        {
			producer.UpdateResourceGenerationData();
            producer.cityImprovement.exclamationPoint.SetActive(false);
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
            world.cityBuilderManager.UpdateLaborNumbers(world.cityBuilderManager.SelectedCity);
    }

    public void UpdateProduceNumbers()
    {
        if (improvement == null)
            return;

		ResourceProducer producer = improvement.resourceProducer;
        float workEthic = producer.isProducing ? improvement.city.workEthic : 1;

		for (int i = 0; i < producer.producedResources.Count; i++)
        {
            int amount = producer.producedResources[i].resourceAmount;
		    int newAmount = Mathf.RoundToInt(amount * (workEthic + world.GetResourceTypeBonus(producer.producedResources[i].resourceType)));

		    producesInfo[i].SetResourceAmount(newAmount);

		    if (amount == newAmount)
			    producesInfo[i].resourceAmountText.color = Color.white;
		    else if (newAmount > amount)
			    producesInfo[i].resourceAmountText.color = Color.green;
		    else
			    producesInfo[i].resourceAmountText.color = Color.red;
        }
	}

    public void ToggleWaiting(bool v, CityImprovement improvement, bool resource = false, bool storage = false, bool research = false)
    {
        if (activeStatus && this.improvement == improvement)
        {
            waiting = v;
            waitingForText.gameObject.SetActive(v);

            if (v)
            {
                if (resource)
                    waitingForText.text = "Waiting for Resources";
                else if (storage)
                    waitingForText.text = "Waiting for Storage";
                else if (research)
                    waitingForText.text = "Waiting for Assignment";
            }

			for (int i = 0; i < improvement.allConsumedResources[highlightIndex].Count; i++)
            {
                if (v)
                {
                    if (consumesInfo[i].resourceType == ResourceType.Gold)
                    {
                        if (world.CheckWorldGold(improvement.allConsumedResources[highlightIndex][i].resourceAmount))
							consumesInfo[i].resourceAmountText.color = Color.white;
                        else
							consumesInfo[i].resourceAmountText.color = Color.red;
					}
                    else if (improvement.allConsumedResources[highlightIndex][i].resourceAmount > improvement.meshCity.ResourceManager.ResourceDict[consumesInfo[i].resourceType])
                    {
						consumesInfo[i].resourceAmountText.color = Color.red;
					}
                    else
                    {
						consumesInfo[i].resourceAmountText.color = Color.white;
					}
                }
                else
                {
				    consumesInfo[i].resourceAmountText.color = Color.white;
                }
		    }
        }
    }

    private void SetResourceCount(int amount)
    {
		if (amount < 100000)
		{
			resourceCount.text = $"{amount:n0}";
		}
		else if (amount < 1000000)
		{
			resourceCount.text = Math.Round(amount * 0.001f, 1) + " k";
		}
		else if (amount < 1000000000)
		{
			resourceCount.text = Math.Round(amount * 0.000001f, 1) + " M";
		}
	}

    public void UpdateResourceAmount(CityImprovement improvement)
    {
        if (activeStatus && this.improvement == improvement && improvement.GetImprovementData.rawMaterials && improvement.td.resourceAmount >= 0)
            SetResourceCount(improvement.td.resourceAmount);
    }

	internal void CloseCheck(CityImprovement improvement)
	{
        if (activeStatus && this.improvement == improvement)
            ToggleVisibility(false);
	}
}
