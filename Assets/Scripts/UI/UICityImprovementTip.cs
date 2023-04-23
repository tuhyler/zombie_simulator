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
    public TMP_Text title, level;
    private TMP_Text producesText, consumesNone;

    [SerializeField]
    private Image improvementImage, produceHighlight;

    [SerializeField]
    private Transform producesRect, consumesRect;
    private int producesCount;

    private List<UIResourceInfoPanel> producesInfo = new(), consumesInfo = new();
    private List<ResourceIndividualSO> resourceInfo = new();
    private List<int> produceTimeList = new();

    //cached improvement for turning off highlight
    private CityImprovement improvement;

    private bool fourK;

    //for tweening
    [SerializeField]
    private RectTransform allContents;
    private bool activeStatus;

    private void Awake()
    {
        resourceInfo = ResourceHolder.Instance.allStorableResources.Concat(ResourceHolder.Instance.allWorldResources).ToList();

        if (Screen.height <= 1080)
        {
            allContents.anchorMin = new Vector2(0.1f, 0.1f);
            allContents.anchorMax = new Vector2(0.1f, 0.1f);
        }
        else if (Screen.height <= 1440)
        {
            allContents.anchorMin = new Vector2(0.05f, 0.05f);
            allContents.anchorMax = new Vector2(0.05f, 0.05f);
        }
        else if (Screen.height <= 2160)
        {
            allContents.anchorMin = new Vector2(-0.2f, -0.2f);
            allContents.anchorMax = new Vector2(-0.2f, -0.2f);
            fourK = true;
        }

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

        gameObject.SetActive(false);
    }

    public void ToggleVisibility(bool val, CityImprovement improvement = null)
    {
        if (activeStatus == val)
            return;

        LeanTween.cancel(gameObject);

        if (val)
        {
            this.improvement = improvement;
            SetData(this.improvement);
            this.improvement.EnableHighlight(Color.white);
            gameObject.SetActive(val);
            activeStatus = true;
            Vector3 position = Input.mousePosition;

            //trick to make sure entire window always showns on screen
            if (fourK)
            {
                position.x -= 1920;
                position.y -= 1080;
                position *= .6f;
                position.x += 1920;
                position.y += 1080;
            }

            position.z = 0;
            allContents.anchoredPosition = position;
            allContents.localScale = Vector3.zero;
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
    }

    private void SetData(CityImprovement improvement)
    {
        ImprovementDataSO data = improvement.GetImprovementData;
        title.text = data.improvementDisplayName;
        if (data.improvementName == data.improvementDisplayName)
            level.text = "Level " + data.improvementLevel.ToString();
        else
            level.text = "Level " + data.improvementLevel.ToString() + " " + data.improvementName;
        improvementImage.sprite = data.image;
        produceTimeList = data.producedResourceTime;
        int index = improvement.producedResourceIndex;
        int producedTime = produceTimeList[index];

        SetResourcePanelInfo(producesInfo, data.producedResources, producedTime, true, data.workEthicChange);
        SetResourcePanelInfo(consumesInfo, improvement.allConsumedResources[index], producedTime, false);
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
                produceHighlight.gameObject.SetActive(false);
                
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

        int indexSelect = 0;

        for (int i = 0; i < panelList.Count; i++)
        {
            if (i >= resourcesCount)
            {
                panelList[i].gameObject.SetActive(false);
            }
            else
            {
                panelList[i].gameObject.SetActive(true);
                panelList[i].resourceAmount.text = Mathf.RoundToInt(resourceList[i].resourceAmount * (60f / producedTime)).ToString();
                panelList[i].resourceType = resourceList[i].resourceType;
                int index = resourceInfo.FindIndex(a => a.resourceType == resourceList[i].resourceType);
                panelList[i].resourceImage.sprite = resourceInfo[index].resourceIcon;

                if (produces)
                {
                    if (resourceList[i].resourceType == improvement.producedResource)
                        indexSelect = i;
                }
            }
        }

        if (produces)
        {
            float xShiftLeft = (resourcesCount-1) * 45;
            float xShiftRight = indexSelect * 90;
            float xShift = (resourcesCount - 1) * 90;
            //xShiftRight -= 1.5f;

            //Vector2 loc = panelList[0].transform.localPosition;
            Vector2 loc = Vector2.zero;
            loc.x -= xShiftLeft;
            loc.x += xShiftRight;
            //loc.x += 45;
            loc.y -= 40;
            produceHighlight.transform.localPosition = loc;
        }
    }

    public void ChangeResourceProduced(int a)
    {
        ResourceProducer producer = world.GetResourceProducer(improvement.loc);
        if (producer.isProducing || producer.isWaitingForStorageRoom || producer.isWaitingforResources || producer.isWaitingToUnload)
            producer.StopProducing(true);

        improvement.producedResource = producesInfo[a].resourceType;
        improvement.producedResourceIndex = a;
        producer.producedResourceIndex = a;
        producer.SetNewProgressTime();
        producer.producedResource = improvement.GetImprovementData.producedResources[a];
        producer.consumedResources = improvement.allConsumedResources[a];
        producer.SetConsumedResourceTypes();
        if (producer.currentLabor > 0)
            producer.StartProducing();

        SetResourcePanelInfo(consumesInfo, improvement.allConsumedResources[a], produceTimeList[a], false);

        float xShiftLeft = (producesCount - 1) * 45;
        float xShiftRight = a * 90;
        //xShiftRight -= 1.5f;

        //Vector2 loc = producesInfo[a].transform.localPosition;
        Vector2 loc = Vector2.zero;
        loc.x -= xShiftLeft;
        loc.x += xShiftRight;
        loc.y -= 40f;
        produceHighlight.transform.localPosition = loc;

        if (world.cityBuilderManager.SelectedCity != null)
            world.cityBuilderManager.UpdateLaborNumbers();
    }
}
