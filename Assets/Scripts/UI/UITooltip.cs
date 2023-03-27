using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

public class UITooltip : MonoBehaviour
{
    public TMP_Text title, level, producesTitle;
    private TMP_Text producesText, consumesNone;
    private int screenHeightNegHalf = -750, listCount;
    private RectTransform rectTransform;
    public Transform producesRect, consumesRect, costsRect;
    private List<UIResourceInfoPanel> producesInfo = new(), consumesInfo = new(), costsInfo = new();
    List<ResourceIndividualSO> resourceInfo = new();

    private void Awake()
    {
        resourceInfo = ResourceHolder.Instance.allStorableResources.Concat(ResourceHolder.Instance.allWorldResources).ToList();
        //screenHeightNegHalf = Screen.height * -1 / 2;
        rectTransform = GetComponent<RectTransform>();
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
        foreach (Transform selection in costsRect)
        {
            costsInfo.Add(selection.GetComponent<UIResourceInfoPanel>());
        }
        listCount = producesInfo.Count;
    }

    public void SetInfo(Vector3 position, string title, int level, float workEthic, string description, List<ResourceValue> produces, List<ResourceValue> consumes, List<ResourceValue> costs)
    {
        transform.position = position;
        this.title.text = title;
        this.level.text = "Level " + level.ToString();
        SetResourcePanelInfo(producesInfo, produces, true, false, workEthic, description);
        SetResourcePanelInfo(consumesInfo, consumes, false, true);
        SetResourcePanelInfo(costsInfo, costs, false);

        PositionCheck();
    }

    private void SetResourcePanelInfo(List<UIResourceInfoPanel> panelList, List<ResourceValue> resourceList, bool produces, bool consumes = false, float workEthic = 0, string description = "")
    {
        int resourcesCount = resourceList.Count;
        bool showText = false;
        if (workEthic > 0 || description.Length > 0)
            showText = true;

        //show text for produces section
        if (produces)
        {
            producesTitle.text = "Produces";

            if (showText)
            {
                producesText.gameObject.SetActive(true);
                if (workEthic > 0)
                    producesText.text = "Work Ethic +" + (workEthic * 100).ToString() + '%';
                else
                {
                    producesTitle.text = "Unit Info";
                    producesText.text = description;
                }

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
        if (consumes)
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

        for (int i = 0; i < listCount; i++)
        {
            if (i >= resourcesCount)
            {
                panelList[i].gameObject.SetActive(false);
            }
            else
            {
                panelList[i].gameObject.SetActive(true);
                panelList[i].resourceAmount.text = resourceList[i].resourceAmount.ToString();
                int index = resourceInfo.FindIndex(a => a.resourceType == resourceList[i].resourceType);
                panelList[i].resourceImage.sprite = resourceInfo[index].resourceIcon;
            }
        }
    }

    private void PositionCheck()
    {
        if (transform.localPosition.y - rectTransform.rect.height < screenHeightNegHalf)
        {
            rectTransform.pivot = new Vector2(0.5f,0);
        }
        else
        {
            rectTransform.pivot = new Vector2(0.5f, 1);
        }
    }

}
