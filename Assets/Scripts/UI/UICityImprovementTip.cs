using Mono.Cecil;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UICityImprovementTip : MonoBehaviour, ITooltip
{
    [SerializeField]
    private MapWorld world;
    
    [SerializeField]
    public TMP_Text title, level, resourceCount, waitingForText, consumesText, workEthicAmount, specialtyAmount;
    private TMP_Text /*producesText, */consumesNone;

    [SerializeField]
    private GameObject /*waitingForText, */resourceCountGO, producesHolder, workEthicHolder, specialtyHolder;
    private bool waiting;

    [SerializeField]
    private Image improvementImage, specialtyImage;//, produceHighlight;

    [SerializeField]
    private Sprite housingSprite, waterSprite, powerSprite, purchaseAmountSprite;

    [SerializeField]
    private List<Image> highlightList = new();

    [SerializeField]
    private Transform producesRect, consumesRect;
    private int producesCount, maxCount, highlightIndex;

    private List<UIResourceInfoPanel> producesInfo = new(), consumesInfo = new();
    private List<int> produceTimeList = new();

    //cached improvement for turning off highlight
    [HideInInspector]
    public CityImprovement improvement;
    //private float xChange, yChange; //work around for produce highlight

    //for tweening
    [SerializeField]
    private RectTransform allContents, lineImage, infoHolder;
    [HideInInspector]
    public bool activeStatus;

	private void Awake()
    {
        transform.localScale = Vector3.zero;
        gameObject.SetActive(false);

        foreach (Transform selection in producesRect)
        {
            if (selection.TryGetComponent(out UIResourceInfoPanel panel))
                producesInfo.Add(panel);
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
            if (improvement.GetImprovementData.audio != null)
                world.cityBuilderManager.PlaySelectAudio(improvement.GetImprovementData.audio);
            world.iTooltip = this;
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
				if (producer.isWaitingForResearch || producer.isWaitingForStorageRoom)
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
            else if (producer.isWaitingForStorageRoom /*|| producer.isWaitingToUnload*/)
            {
				waiting = true;
				waitingForText.gameObject.SetActive(true);
				waitingForText.text = "Waiting for Storage";
			}
            else if (producer.hitResourceMax)
            {
				waiting = true;
				waitingForText.gameObject.SetActive(true);
				waitingForText.text = "At Resource Max Level";
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

            p.z = 935;
            if (p.y + allContents.rect.height * 0.5f > Screen.height)
                y = 1f;
            else if (p.y - allContents.rect.height * 0.5f < 0)
                y = 0f;

            if (p.x + allContents.rect.width * 0.5f > Screen.width)
                x = 1f;
            else if (p.x - allContents.rect.width * 0.5f < 0)
                x = 0f;

            allContents.pivot = new Vector2(x, y);
            Vector3 pos = Camera.main.ScreenToWorldPoint(p);
            allContents.transform.position = pos;

            LeanTween.scale(allContents, Vector3.one, 0.25f).setEaseLinear();
        }
        else
        {
            world.iTooltip = null;
            this.improvement.DisableHighlight();
            this.improvement = null;
            activeStatus = false;
            LeanTween.scale(allContents, Vector3.zero, 0.25f).setOnComplete(SetActiveStatusFalse);
        }
    }

    private void SetActiveStatusFalse()
    {
        gameObject.SetActive(false);
        if (world.iTooltip == null)
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
        specialtyHolder.SetActive(false);
        workEthicHolder.SetActive(false);

        if (improvement.building)
        {
            consumesText.text = "Cost Per Cycle";
    
            if (improvement.GetImprovementData.cityBonus)
            {
				SetResourcePanelInfo(producesInfo, producer.producedResources, 0, true, producer.isProducing, 1);
				SetResourcePanelInfo(consumesInfo, improvement.allConsumedResources[0], 0, false, false, 1, true);
				producesHolder.SetActive(true);

			    if (data.workEthicChange != 0)
			    {
				    workEthicHolder.SetActive(true);
				    string prefix = "+";
				    workEthicAmount.color = Color.black;

				    if (data.workEthicChange < 0)
				    {
					    prefix = "-";
					    workEthicAmount.color = Color.red;
				    }

				    workEthicAmount.text = prefix + Mathf.RoundToInt(data.workEthicChange * 100) + "%";
			    }
			    else if (data.housingIncrease > 0)
			    {
				    specialtyHolder.SetActive(true);
				    specialtyAmount.text = "+" + data.housingIncrease;
				    specialtyImage.sprite = housingSprite;
			    }
			    else if (data.waterIncrease > 0)
			    {
				    specialtyHolder.SetActive(true);
				    specialtyAmount.text = "+" + data.waterIncrease;
				    specialtyImage.sprite = waterSprite;
			    }
			    else if (data.powerIncrease > 0)
			    {
				    specialtyHolder.SetActive(true);
				    specialtyAmount.text = "+" + data.powerIncrease;
				    specialtyImage.sprite = powerSprite;
			    }
			    else if (data.purchaseAmountChange > 0)
			    {
				    specialtyHolder.SetActive(true);
				    specialtyAmount.text = "+" + data.purchaseAmountChange;
				    specialtyImage.sprite = purchaseAmountSprite;
			    }
			}
			else
            {
                producesHolder.SetActive(false);
                SetResourcePanelInfo(consumesInfo, improvement.GetCycleCost(), 0, false, false, 1, true);
            }
		}
		else
        {
            producesHolder.SetActive(true);
            consumesText.text = "Requires";
            produceTimeList = data.producedResourceTime;
            highlightIndex = improvement.resourceProducer.producedResourceIndex;
            int producedTime = produceTimeList[highlightIndex];

            float workEthic;
            if (improvement.city == null)
                workEthic = 1;
            else
                workEthic = improvement.city.workEthic;

            if (producer.producedResources.Count > 0)
            {
                SetResourcePanelInfo(producesInfo, producer.producedResources, producedTime, true, producer.isProducing, workEthic);
            }
            
            SetResourcePanelInfo(consumesInfo, improvement.allConsumedResources[highlightIndex], producedTime, false);

            if (data.getTerrainResource)
                highlightIndex = 0;

            highlightList[highlightIndex].gameObject.SetActive(true);
        }

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
        int panelHeight = 340;
        int lineWidth = 280 + multiple;

        if (!improvement.building || improvement.GetImprovementData.cityBonus)
            panelHeight += 140;

        if (showCount)
            panelHeight += 75;
        
        if (waiting)
            panelHeight += 40;

        infoHolder.sizeDelta = new Vector2(panelWidth, 190);
        allContents.sizeDelta = new Vector2(panelWidth, panelHeight);
        lineImage.sizeDelta = new Vector2(lineWidth, 4);
    }

    private void SetResourcePanelInfo(List<UIResourceInfoPanel> panelList, List<ResourceValue> resourceList, int producedTime, bool produces, bool producing = false, float workEthic = 1, bool building = false)
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
                if (!produces && !building)
                {
                    panelList[i].gameObject.SetActive(true);
                    panelList[i].SetResourceAmount(producedTime);
                    panelList[i].SetResourceType(ResourceType.Time);
                    panelList[i].resourceImage.sprite = ResourceHolder.Instance.GetIcon(ResourceType.Time);
					panelList[i].resourceAmountText.color = Color.white;
				}
                else
                {
                    panelList[i].gameObject.SetActive(false);
                }
            }
            else if (resourceList[i].resourceType == ResourceType.None)
            {
                panelList[i].gameObject.SetActive(false);
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
                    int newAmount = (int)Math.Round(amount * (workEthic + world.GetResourceTypeBonus(resourceList[i].resourceType)), MidpointRounding.AwayFromZero);

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
                        else if (resourceList[i].resourceAmount > improvement.meshCity.resourceManager.resourceDict[resourceList[i].resourceType])
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
        int currentLabor = 0;
        if (producer.isProducing || producer.isWaitingForStorageRoom || producer.isWaitingforResources || producer.hitResourceMax)
        {
            producer.StopProducing(true);
            currentLabor = producer.currentLabor;
            producer.UpdateCurrentLaborData(0);

            ResourceType type = producer.producedResource.resourceType;
			//if (type == ResourceType.Fish)
			//	type = ResourceType.Food;

			int totalResourceLabor = producer.cityImprovement.city.ChangeResourcesWorked(type, -currentLabor);
			if (totalResourceLabor == 0)
				producer.cityImprovement.city.RemoveFromResourcesWorked(type);
		}

        //improvement.producedResource = producesInfo[a].resourceType;
        //improvement.producedResourceIndex = a;
        //improvement.CalculateWorkCycleLimit();
        producer.producedResourceIndex = a;
        highlightIndex = a;
        producer.SetNewProgressTime();

        if (producer.producedResources[a].resourceType == ResourceType.Fish)
        {
            ResourceValue newValue;
            newValue.resourceType = ResourceType.Food;
            newValue.resourceAmount = producer.producedResources[a].resourceAmount;
            producer.producedResource = newValue;
        }
        else
        {
            producer.producedResource = producer.producedResources[a];
        }
        producer.consumedResources = improvement.allConsumedResources[a];
        producer.SetConsumedResourceTypes();
        SetResourcePanelInfo(consumesInfo, improvement.allConsumedResources[a], produceTimeList[a], false);

        if (currentLabor > 0)
        {
            producer.UpdateCurrentLaborData(currentLabor);

            ResourceType type = producer.producedResource.resourceType;
            //if (type == ResourceType.Fish)
            //    type = ResourceType.Food;

			producer.cityImprovement.city.ChangeResourcesWorked(type, currentLabor);
			producer.cityImprovement.exclamationPoint.SetActive(false);
			producer.StartProducing(true);
        }

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

        if (producer.cityImprovement.city != null && producer.cityImprovement.city.activeCity)
            producer.UpdateCityImprovementStats();

        //if (world.cityBuilderManager.SelectedCity != null)
        //    world.cityBuilderManager.UpdateLaborNumbers(world.cityBuilderManager.SelectedCity);
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
		    int newAmount = (int)Math.Round(amount * (workEthic + world.GetResourceTypeBonus(producer.producedResources[i].resourceType)), MidpointRounding.AwayFromZero);

		    producesInfo[i].SetResourceAmount(newAmount);

		    if (amount == newAmount)
			    producesInfo[i].resourceAmountText.color = Color.white;
		    else if (newAmount > amount)
			    producesInfo[i].resourceAmountText.color = Color.green;
		    else
			    producesInfo[i].resourceAmountText.color = Color.red;
        }
	}

	public void UpdateConsumeNumbers()
    {
        if (improvement == null)
            return;

        SetResourcePanelInfo(consumesInfo, improvement.GetCycleCost(), 0, false, false, 1, true);
	}

	public void ToggleWaiting(bool v, CityImprovement improvement, bool resource = false, bool storage = false, bool research = false, bool resourceMax = false)
    {
        if (activeStatus && this.improvement == improvement)
        {
            if (waiting != v)
            {
                waiting = v;
                waitingForText.gameObject.SetActive(v);

                Vector2 allContentsSize = allContents.sizeDelta;
                allContentsSize.y += v ? 40 : -40;
                allContents.sizeDelta = allContentsSize;
			}

            if (v)
            {
                if (resource)
                    waitingForText.text = "Waiting for Resources";
                else if (storage)
                    waitingForText.text = "Waiting for Storage";
                else if (research)
                    waitingForText.text = "Waiting for Assignment";
                else if (resourceMax)
                    waitingForText.text = "At Resource Max Level";
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
                    else if (improvement.allConsumedResources[highlightIndex][i].resourceAmount > improvement.meshCity.resourceManager.resourceDict[consumesInfo[i].resourceType])
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

	public void CheckResource(City city, int amount, ResourceType type)
	{
		
	}
}
