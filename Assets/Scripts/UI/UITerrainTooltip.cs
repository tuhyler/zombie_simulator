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
    private MapWorld world;
    
    [SerializeField]
    public TMP_Text title, resourceNone, resourceCount, resourceCountTitle, requiresText;

    [SerializeField]
    private Image resourceImage;
    private UITooltipTrigger tooltipTrigger;

    //cached TerrainData for turning off highlight
    [HideInInspector]
    public TerrainData td;

    //for tweening
    [SerializeField]
    private RectTransform allContents;
    [HideInInspector]
    public bool activeStatus;

    private void Awake()
    {
        transform.localScale = Vector3.zero;
        gameObject.SetActive(false);
        tooltipTrigger = GetComponentInChildren<UITooltipTrigger>();
    }

    public void HandleEsc()
    {
        if (activeStatus)
            world.CloseTerrainTooltipCloseButton();
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
            Vector3 p = Input.mousePosition;
            float x = 0.5f;
            float y = 0f;

            p.z = 935;
            //p.z = 1f;
            if (p.y + allContents.rect.height > Screen.height)
                y = 1f;

            if (p.x + allContents.rect.width * 0.5f > Screen.width)
                x = 1f;
            else if (p.x - allContents.rect.width * 0.5 < 0)
                x = 0f;

            allContents.pivot = new Vector2(x, y);
            Vector3 pos = Camera.main.ScreenToWorldPoint(p);
            allContents.transform.position = pos;
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
        world.infoPopUpCanvas.gameObject.SetActive(false);
        gameObject.SetActive(false);
    }

    private void SetData(TerrainData td)
    {
        title.text = td.terrainData.title;
        ResourceType type = td.resourceType;

        if (type == ResourceType.None)
        {
            resourceNone.gameObject.SetActive(true);
            resourceImage.gameObject.SetActive(false);
            resourceCountTitle.gameObject.SetActive(false);
            resourceCount.gameObject.SetActive(false);
            requiresText.gameObject.SetActive(false);
            allContents.sizeDelta = new Vector2(290,250);
        }
        else
        {
            resourceNone.gameObject.SetActive(false);
            resourceImage.gameObject.SetActive(true);
            resourceCountTitle.gameObject.SetActive(true);
            resourceCount.gameObject.SetActive(true);
            requiresText.gameObject.SetActive(true);
            requiresText.text = "Requires " + ResourceHolder.Instance.GetRequirement(type);
            allContents.sizeDelta = new Vector2(290, 300);
            resourceImage.sprite = ResourceHolder.Instance.GetIcon(type);
            tooltipTrigger.SetMessage(ResourceHolder.Instance.GetName(type));

            if (td.resourceAmount < 0)
            {
                resourceCount.text = "\u221E"; //infinity symbol
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
