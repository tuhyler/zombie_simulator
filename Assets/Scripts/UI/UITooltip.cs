using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

public class UITooltip : MonoBehaviour
{
    public TMP_Text title;
    private int screenHeightNegHalf, listCount;
    private RectTransform rectTransform;
    public Transform producesRect, consumesRect, costsRect;
    private List<UIResourceInfoPanel> producesInfo = new(), consumesInfo = new(), costsInfo = new();
    List<ResourceIndividualSO> resourceInfo = new();

    private void Awake()
    {
        resourceInfo = ResourceHolder.Instance.allStorableResources.Concat(ResourceHolder.Instance.allWorldResources).ToList();
        screenHeightNegHalf = Screen.height * -1 / 2;
        rectTransform = GetComponent<RectTransform>();
        gameObject.SetActive(false);

        foreach (Transform selection in producesRect)
        {
            producesInfo.Add(selection.GetComponent<UIResourceInfoPanel>());
        }
        foreach (Transform selection in consumesRect)
        {
            consumesInfo.Add(selection.GetComponent<UIResourceInfoPanel>());
        }
        foreach (Transform selection in costsRect)
        {
            costsInfo.Add(selection.GetComponent<UIResourceInfoPanel>());
        }
        listCount = producesInfo.Count;
    }

    public void SetInfo(Vector3 position, string title, List<ResourceValue> produces, List<ResourceValue> consumes, List<ResourceValue> costs)
    {
        transform.position = position;
        this.title.text = title;
        SetResourcePanelInfo(producesInfo, produces);
        SetResourcePanelInfo(consumesInfo, consumes);
        SetResourcePanelInfo(costsInfo, costs);

        PositionCheck();
    }

    private void SetResourcePanelInfo(List<UIResourceInfoPanel> panelList, List<ResourceValue> resourceList)
    {
        int resourcesCount = resourceList.Count;
        
        for (int i = 0; i < listCount; i++)
        {
            if (i >= resourcesCount)
            {
                panelList[i].gameObject.SetActive(false);
            }
            else
            {
                if (!panelList[i].isActiveAndEnabled)
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
    }

}
