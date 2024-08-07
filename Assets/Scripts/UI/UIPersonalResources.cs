using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIPersonalResources : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField]
    private TMP_Text resourceAmountText;
    [SerializeField]
    public Image resourceImage, background;
    [HideInInspector]
    public int resourceAmount;

    [SerializeField] //for tweening
    private RectTransform allContents;

    [HideInInspector]
    public ResourceType resourceType;

    private UIPersonalResourceInfoPanel buttonHandler;
    [HideInInspector]
    public UIResourceGivingPanel givingPanel;

    [SerializeField]
    private ParticleSystem buttonHighlight;

    [HideInInspector]
    public bool clickable = false;
    //for moving panels on resource grid
    [HideInInspector]
    public int resourceValue, loc; 
    [HideInInspector]
    public Transform originalParent;
    public Transform tempParent;

    //for showing trade center pricing
    [SerializeField]
    private GameObject priceHolder;
    [SerializeField]
    private TMP_Text priceText;
    [HideInInspector]
    public int price;

    private void Awake()
    {
        //SetButtonInteractable(false);

		resourceAmountText.outlineColor = Color.black;
		resourceAmountText.outlineWidth = .2f;

		if (tempParent != null)
            buttonHandler = tempParent.GetComponent<UIPersonalResourceInfoPanel>();

        priceHolder.SetActive(false);
    }

    public void SetButtonInteractable(bool v)
    {
        clickable = v;
    }

    public void SetValue(int val)
    {
        SetNumberText(val);
        resourceAmount = val;
    }

    public void SetText(string text, int val)
    {
        resourceAmountText.text = text;
        resourceAmount = val;
    }

    public void Activate(bool instant)
    {
        SetButtonInteractable(true);
        allContents.localScale = Vector3.zero;
        float speed = instant ? 0 : 0.2f;
        LeanTween.scale(allContents, Vector3.one, speed).setEase(LeanTweenType.easeOutBack);
    }
        
    public void UpdateValue(int val, bool positive)
    {
        if (positive)
        {
            buttonHighlight.transform.localPosition = Vector3.zero;
            buttonHighlight.Play();
        }
        SetNumberText(val);
        resourceAmount = val;
    }

    private void SetNumberText(int amount)
    {
		if (amount < 1000)
		{
			resourceAmountText.text = amount.ToString();
		}
		else if (amount < 1000000)
		{
			resourceAmountText.text = Math.Round(amount * 0.001f, 1) + "k";
		}
		else if (amount < 1000000000)
		{
			resourceAmountText.text = Math.Round(amount * 0.000001f, 1) + "M";
		}
	}

    public void FlashResource()
    {
        buttonHighlight.transform.localPosition = Vector3.zero;
        buttonHighlight.Play();
    }

    //public void OnPointerClick()
    //{
    //    //if (clickable)
    //    //{
    //    //    buttonHandler.PrepareResource(resourceType);
    //    //}
    //}

    public void OnPointerDown(PointerEventData eventData)
    {
		if (eventData.button == PointerEventData.InputButton.Left)
        {
		    if (clickable)
            {
                if (buttonHandler != null)
                {
                    buttonHandler.PrepareResource(resourceType);
                    buttonHandler.HandleButtonClick();
                }
                else
                {
                    givingPanel.HandleButtonClick();
                }
            }
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
		if (eventData.button == PointerEventData.InputButton.Left)
        {
		    if (!clickable)
            {
                buttonHandler.dragging = true;
                originalParent = transform.parent;
                transform.SetParent(tempParent);
                transform.SetAsLastSibling();
                background.raycastTarget = false;
                buttonHandler.world.cityBuilderManager.PlaySelectAudio(buttonHandler.world.cityBuilderManager.pickUpClip);
            }
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
		if (eventData.button == PointerEventData.InputButton.Left)
        {
		    if (!clickable)
            {
                Vector3 p = Input.mousePosition;
                p.z = 935;
                Vector3 pos = Camera.main.ScreenToWorldPoint(p);
                transform.position = pos;
            }
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
		if (eventData.button == PointerEventData.InputButton.Left)
        {
		    if (!clickable)
            {
                buttonHandler.dragging = false;
                transform.SetParent(originalParent);
                transform.localPosition = Vector3.zero;
                background.raycastTarget = true;
			    buttonHandler.world.cityBuilderManager.PlaySelectAudio(buttonHandler.world.cityBuilderManager.putDownClip);
		    }
        }
    }

    public void SetPriceText(int price)
    {
        priceHolder.SetActive(true);

		if (price < 1000)
		{
			priceText.text = price.ToString();
		}
		else if (price < 1000000)
		{
			priceText.text = Math.Round(price * 0.001f, 1) + "k";
		}
		else if (price < 1000000000)
		{
			priceText.text = Math.Round(price * 0.000001f, 1) + "M";
		}

        this.price = price;
        priceText.rectTransform.sizeDelta = new Vector2(15 + 10 * priceText.text.Length, 30);
    }

    public void SetPriceColor(Color color)
    {
        priceText.color = color;
    }

    public void HidePricing()
    {
        priceHolder.SetActive(false);
    }
}
