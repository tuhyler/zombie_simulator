using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UITerrainTooltip : MonoBehaviour
{
    [SerializeField]
    public TMP_Text title, resourceNone, resourceCount, resourceCountTitle;

    [SerializeField]
    private Image resourceImage;

    private ResourceType resourceHere;
    public ResourceType ResourceHere { set { resourceHere = value; } }

    List<ResourceIndividualSO> resourceInfo = new();

    private int screenHeightHalf, screenWidthHalf;

    //cached TerrainData for turning off highlight
    private TerrainData td;

    //for tweening
    [SerializeField]
    private RectTransform allContents;
    private bool activeStatus;

    private void Awake()
    {
        resourceInfo = ResourceHolder.Instance.allStorableResources.Concat(ResourceHolder.Instance.allWorldResources).ToList();
        gameObject.SetActive(false);
        screenHeightHalf = Screen.height / 2;
        screenWidthHalf = Screen.width / 2;
    }

    public void ToggleVisibility(bool val, TerrainData td = null)
    {
        if (activeStatus == val)
            return;

        LeanTween.cancel(gameObject);

        if (val)
        {
            this.td = td;
            SetData(this.td);
            this.td.EnableHighlight(Color.white);
            gameObject.SetActive(val);
            activeStatus = true;
            Vector3 position = Input.mousePosition;
            position.x -= screenWidthHalf;
            position.y -= screenHeightHalf;
            transform.localPosition = position;
            allContents.anchoredPosition = position;
            //allContents.anchoredPosition = pos;
            allContents.localScale = Vector3.zero;
            LeanTween.scale(allContents, Vector3.one, 0.25f).setEaseLinear();
        }
        else
        {
            this.td.DisableHighlight();
            this.td = null;
            activeStatus = false;
            LeanTween.scale(allContents, Vector3.zero, 0.25f).setOnComplete(SetActiveStatusFalse);
        }
    }

    private void SetActiveStatusFalse()
    {
        gameObject.SetActive(false);
    }

    private void SetData(TerrainData td)
    {
        title.text = td.GetTerrainData().title;
        ResourceType type = td.GetTerrainData().resourceType;

        if (type == ResourceType.None)
        {
            resourceNone.gameObject.SetActive(true);
            resourceImage.gameObject.SetActive(false);
            resourceCountTitle.gameObject.SetActive(false);
            resourceCount.gameObject.SetActive(false);
        }
        else
        {
            resourceNone.gameObject.SetActive(false);
            resourceImage.gameObject.SetActive(true);
            resourceCountTitle.gameObject.SetActive(true);
            resourceCount.gameObject.SetActive(true);

            resourceHere = td.GetTerrainData().resourceType;
            int index = resourceInfo.FindIndex(a => a.resourceType == resourceHere);
            resourceImage.sprite = resourceInfo[index].resourceIcon;    

            if (td.resourceAmount < 0)
            {
                resourceCount.text = "\u221E";
                resourceCount.fontSize = 50;
            }
            else
            {
                resourceCount.text = td.resourceAmount.ToString();
                resourceCount.fontSize = 30;
            }
        }

    }
}
