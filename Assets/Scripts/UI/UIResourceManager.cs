using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using static UnityEngine.Rendering.DebugUI;

public class UIResourceManager : MonoBehaviour
{
    [SerializeField]
    public CityBuilderManager cityBuilderManager;
    
    [SerializeField]
    private TMP_Text cityStorageInfo, /*cityStoragePercent, */cityLevel, cityLimit, slash;

    [SerializeField]
    private Image progressBarMask, buttonDown;
    [SerializeField]
    private Sprite buttonUp;
    private Sprite originalDown;

    [SerializeField] //for tweening
    public RectTransform allContents;
    [HideInInspector]
    public Vector3 originalLoc, overflowOriginalLoc;
    private bool activeStatus, overflowActiveStatus;

    //city info
    [HideInInspector]
    public City city;
    private string cityName;
    private int cityStorageLimit;
    private float cityStorageLevel;

    //resource grid
    public RectTransform gridHolder, overflowGridHolder;
    private Dictionary<int, UIDropLocation> gridCellDict = new();
    private int activeCells;
    private Dictionary<ResourceType, int> resourceUIDict = new();
    public int gridWidth = 10;
    [HideInInspector]
    public bool dragging;

    private void Awake()
    {
        gameObject.SetActive(false);
        overflowGridHolder.gameObject.SetActive(false);
        originalLoc = allContents.anchoredPosition3D;
        overflowOriginalLoc = overflowGridHolder.anchoredPosition3D;
        originalDown = buttonDown.sprite;

        overflowGridHolder.sizeDelta = new Vector2(gridWidth * 90, 90);
        buttonDown.transform.localPosition = new Vector3(gridWidth * 45 + 20, -80, 0);

        cityLevel.outlineColor = Color.black;
        cityLevel.outlineWidth = 0.4f;
		slash.outlineColor = Color.black;
		slash.outlineWidth = 0.4f;
		cityLimit.outlineColor = Color.black;
		cityLimit.outlineWidth = 0.4f;

		int total = 0;
        foreach (Transform selection in gridHolder)
        {
            GridCellPrep(selection, total);
            total++;
        }

        foreach (Transform selection in overflowGridHolder)
        {
            GridCellPrep(selection, total);
            total++;
        }
    }

    private void GridCellPrep(Transform selection, int total)
    {
        UIDropLocation loc = selection.GetComponent<UIDropLocation>();
        loc.gameObject.SetActive(false);
        loc.SetUIResourceManager(this);
        loc.gridLocation = total;
        loc.resource.loc = total;
        gridCellDict.Add(loc.gridLocation, loc);
    }

    public void ToggleVisibility(bool v, City city = null)
    {
        if (activeStatus == v)
            return;

        LeanTween.cancel(gameObject);

        if (v)
        {
            this.city = city;
            SetCityInfo(city.cityName, city.warehouseStorageLimit, city.ResourceManager.ResourceStorageLevel);
            activeCells = 0;
            buttonDown.gameObject.SetActive(false);

            foreach (ResourceType type in city.resourceGridDict.Keys)
                ActivateCell(type);

            if (activeCells > gridWidth)
            {
                gridHolder.sizeDelta = new Vector2(90 * gridWidth, 90);
                buttonDown.gameObject.SetActive(true);
            }
            else
            {
                gridHolder.sizeDelta = new Vector2(activeCells * 90, 90);
            }

            gameObject.SetActive(v);
            activeStatus = true;

            allContents.anchoredPosition3D = originalLoc + new Vector3(0, 200f, 0);

            LeanTween.moveY(allContents, allContents.anchoredPosition3D.y + -200f, 0.3f).setEaseOutSine();
        }
        else
        {
            for (int i = 0; i < activeCells; i++)
            {
                gridCellDict[i].gameObject.SetActive(false);
            }

            this.city.ReshuffleGrid(); //gets rid of zeroed out resources
            this.city = null;
            resourceUIDict.Clear();

            activeStatus = false;
            ToggleOverflowVisibility(false);
            LeanTween.moveY(allContents, allContents.anchoredPosition3D.y + 200f, 0.2f).setOnComplete(SetActiveStatusFalse);
        }
    }

    private void SetActiveStatusFalse()
    {
        gameObject.SetActive(false);
    }

    public void ToggleOverflow()
    {
        cityBuilderManager.PlaySelectAudio();
        
        if (overflowActiveStatus)
        {
            ToggleOverflowVisibility(false);
        }
        else
        {
            cityBuilderManager.ResetCityUIToBase();
            ToggleOverflowVisibility(true);
        }
    }

    public void ToggleOverflowVisibility(bool v)
    {
        if (overflowActiveStatus == v)
            return;

        LeanTween.cancel(overflowGridHolder.gameObject);

        if (v)
        {
            buttonDown.sprite = buttonUp;
            overflowGridHolder.gameObject.SetActive(v);
            overflowActiveStatus = true;
            float size = Mathf.CeilToInt((activeCells - gridWidth) / (float)gridWidth) * 90;

            overflowGridHolder.anchoredPosition3D = overflowOriginalLoc + new Vector3(0, size, 0);

            LeanTween.moveY(overflowGridHolder, overflowGridHolder.anchoredPosition3D.y + -size, 0.3f).setEaseOutSine();
        }
        else
        {
            buttonDown.sprite = originalDown;
            overflowActiveStatus = false;
            LeanTween.moveY(overflowGridHolder, overflowGridHolder.anchoredPosition3D.y + Mathf.CeilToInt((activeCells - gridWidth) / (float)gridWidth) * 90, 0.2f).setOnComplete(SetOverflowStatusFalse);
        }
    }

    private void SetOverflowStatusFalse()
    {
        overflowGridHolder.gameObject.SetActive(false);
    }
    
    private void ActivateCell(ResourceType type)
    {
        int loc = city.resourceGridDict[type];
        gridCellDict[loc].gameObject.SetActive(true);
        activeCells++;

        gridCellDict[loc].resource.resourceType = type;
        gridCellDict[loc].resource.SetValue(city.ResourceManager.ResourceDict[type]);
        gridCellDict[loc].resource.resourceImage.sprite = ResourceHolder.Instance.GetIcon(type);
        resourceUIDict[type] = loc;
    }


    private void SetCityInfo(string cityName, int cityStorageLimit, float cityStorageLevel)
    {
        this.cityStorageLevel = cityStorageLevel;
        this.cityName = cityName;
        this.cityStorageLimit = cityStorageLimit;
        progressBarMask.fillAmount = cityStorageLevel / cityStorageLimit;

		SetCityWarehouseInfo();
    }

    private void SetCityWarehouseInfo()
    {
        cityStorageInfo.text = $"{cityName} Storage";
        //cityStoragePercent.text = $"{Mathf.RoundToInt(100 * (cityStorageLevel / cityStorageLimit))}%";
        cityLevel.text = SetStringValue(cityStorageLevel);
        cityLimit.text = SetStringValue(cityStorageLimit);
        //cityLevelAndLimit.text = $"{cityStorageLevel}/{cityStorageLimit}";
    }

    private void UpdateCityWarehouseInfo()
    {
		cityLevel.text = SetStringValue(cityStorageLevel);
	}

    public void SetCityCurrentStorage(float cityStorageLevel)
    {
        this.cityStorageLevel = cityStorageLevel;
        progressBarMask.fillAmount = cityStorageLevel / cityStorageLimit;
		//UpdateStorage(cityStorageLevel);
		UpdateCityWarehouseInfo();
    }

	private string SetStringValue(float amount)
	{
		string amountStr = "-";

		if (amount < 10000)
		{
			amountStr = $"{amount:n0}";
		}
		else if (amount < 1000000)
		{
			amountStr = Math.Round(amount * 0.001f, 1) + " k";
		}
		else if (amount < 1000000000)
		{
			amountStr = Math.Round(amount * 0.000001f, 1) + " M";
		}

		return amountStr;
	}

	private void UpdateStorage(float cityStorageLevel)
    {
        LeanTween.value(progressBarMask.gameObject, progressBarMask.fillAmount, cityStorageLevel / cityStorageLimit, 0.2f)
            .setEase(LeanTweenType.easeOutSine)
            .setOnUpdate((value) =>
            {
                progressBarMask.fillAmount = value;
            });
    }

    public void SetResource(ResourceType type, int val)
    {
        if (!resourceUIDict.ContainsKey(type))
        {
            if (!city.resourceGridDict.ContainsKey(type))
                city.AddToGrid(type);

            ActivateCell(type);
            if (activeCells > gridWidth)
                buttonDown.gameObject.SetActive(true);
            else
                gridHolder.sizeDelta = new Vector2(activeCells * 90, 90);
        }

        gridCellDict[resourceUIDict[type]].resource.SetValue(val);
    }

    public void MoveResources(int oldNum, int newNum, ResourceType type)
    {
        //shifting all the other resources
        if (oldNum > newNum)
        {
            for (int i = oldNum; i > newNum; i--)
            {
                int next = i - 1;
                gridCellDict[next].MoveResource(gridCellDict[i], true);
                city.resourceGridDict[gridCellDict[i].resource.resourceType] = i;
                resourceUIDict[gridCellDict[i].resource.resourceType] = i;
            }
        }
        else
        {
            for (int i = oldNum; i < newNum; i++)
            {
                int next = i + 1;
                gridCellDict[next].MoveResource(gridCellDict[i], false);
                city.resourceGridDict[gridCellDict[i].resource.resourceType] = i;
                resourceUIDict[gridCellDict[i].resource.resourceType] = i;
            }
        }

        city.resourceGridDict[type] = newNum;
        resourceUIDict[type] = newNum;
    }
}
