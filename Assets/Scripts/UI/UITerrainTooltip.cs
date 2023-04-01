using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UITerrainTooltip : MonoBehaviour
{
    [SerializeField]
    public TMP_Text title, resourceNone, resourceCount, resourceCountTitle;

    [SerializeField]
    private Image resourceImage;

    List<ResourceIndividualSO> resourceInfo = new();

    //cached TerrainData for turning off highlight
    private TerrainData td;
    private bool fourK;

    //for tweening
    [SerializeField]
    private RectTransform allContents;
    private bool activeStatus;

    private void Awake()
    {
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

        resourceInfo = ResourceHolder.Instance.allStorableResources.Concat(ResourceHolder.Instance.allWorldResources).ToList();
        gameObject.SetActive(false);
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

            //isn't showing up at mouse position at 4k for some reason
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

            int index = resourceInfo.FindIndex(a => a.resourceType == td.GetTerrainData().resourceType);
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
