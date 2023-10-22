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

    [SerializeField]
    private ParticleSystem buttonHighlight;

    [HideInInspector]
    public bool clickable = true;
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
        SetButtonInteractable(false);

        if (Screen.height > 1080)
            buttonHighlight.transform.localScale = new Vector3(90f, 90f, 90f);
        else if (Screen.height < 1080)
            buttonHighlight.transform.localScale = new Vector3(110f, 110f, 110f);

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
        resourceAmountText.text = val.ToString();
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
        resourceAmountText.text = val.ToString();
        resourceAmount = val;
    }

    public void FlashResource()
    {
        buttonHighlight.transform.localPosition = Vector3.zero;
        buttonHighlight.Play();
    }

    public void OnPointerClick()
    {
        if (clickable)
        {
            buttonHandler.PrepareResource(resourceType);
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (clickable)
        {
            buttonHandler.PrepareResource(resourceType);
            buttonHandler.HandleButtonClick();
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!clickable)
        {
            buttonHandler.dragging = true;
            originalParent = transform.parent;
            transform.SetParent(tempParent);
            transform.SetAsLastSibling();
            background.raycastTarget = false;
            buttonHandler.world.cityBuilderManager.PlayPickUpAudio();
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!clickable)
        {
            Vector3 p = Input.mousePosition;
            p.z = 935;
            Vector3 pos = Camera.main.ScreenToWorldPoint(p);
            transform.position = pos;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!clickable)
        {
            buttonHandler.dragging = false;
            transform.SetParent(originalParent);
            transform.localPosition = Vector3.zero;
            background.raycastTarget = true;
			buttonHandler.world.cityBuilderManager.PlayPutDownAudio();
		}
    }

    public void SetPriceText(int price)
    {
        priceHolder.SetActive(true);
        string str = price.ToString();
        priceText.text = str;
        this.price = price;
        priceText.rectTransform.sizeDelta = new Vector2(15 + 10 * str.Length, 30);
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
