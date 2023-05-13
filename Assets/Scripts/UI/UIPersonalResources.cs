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
    private int resourceAmount;

    [SerializeField] //for tweening
    private RectTransform allContents;

    [HideInInspector]
    public ResourceType resourceType;

    [SerializeField]
    private CanvasGroup canvasGroup;

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

    private void Awake()
    {
        SetButtonInteractable(false);

        if (Screen.height > 1080)
            buttonHighlight.transform.localScale = new Vector3(90f, 90f, 90f);
        else if (Screen.height < 1080)
            buttonHighlight.transform.localScale = new Vector3(110f, 110f, 110f);

        buttonHandler = tempParent.GetComponent<UIPersonalResourceInfoPanel>();
    }

    //public void SetButtonHandler(UIPersonalResourceInfoPanel infoPanel)
    //{
    //    buttonHandler = infoPanel;
    //}

    //public void CheckVisibility()
    //{
    //    //buttonHandler.showingCount = 0;

    //    if (resourceAmount > 0) //convert string to integer
    //    {
    //        activeStatus = true;
    //        allContents.localScale = scale; //if selecting trader while it's loading
    //        gameObject.SetActive(true);
    //    }
    //    else
    //    {
    //        activeStatus = false;
    //        gameObject.SetActive(false);
    //    }
    //}

    public void SetButtonInteractable(bool v)
    {
        clickable = v;
        //canvasGroup.interactable = v;
    }

    public void SetValue(int val, bool active = false)
    {
        //if (active)
        //{
        //    if (val <= 0)
        //    {
        //        SetButtonInteractable(false);
        //        LeanTween.scale(gameObject, Vector3.zero, 0.2f).setEase(LeanTweenType.easeOutSine).setOnComplete(SetActiveStatusFalse);
        //        buttonHandler.ReshuffleGrid();
        //    }
        //    //else if (!activeStatus && val > 0)
        //    //{
        //    //    activeStatus = true;
        //    //    gameObject.SetActive(true);
        //    //    allContents.localScale = Vector3.zero;
        //    //    LeanTween.scale(allContents, Vector3.one, 0.2f).setEase(LeanTweenType.easeOutBack);
        //    //}
        //}

        resourceAmountText.text = val.ToString();
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
        //int currentAmount = int.Parse(resourceAmountText.text);
        //currentAmount += val;
        //if (val <= 0)
        //{
        //    SetButtonInteractable(false);
        //    LeanTween.scale(gameObject, Vector3.zero, 0.2f).setEase(LeanTweenType.easeOutSine).setOnComplete(SetActiveStatusFalse);
        //    buttonHandler.ReshuffleGrid();
        //}
        //else if (!activeStatus && val > 0)
        //{
        //    SetButtonInteractable(true);
        //    gameObject.SetActive(true);
        //    allContents.localScale = Vector3.zero;
        //    LeanTween.scale(allContents, Vector3.one, 0.2f).setEase(LeanTweenType.easeOutBack);
        //}
        if (positive)
        {
            buttonHighlight.transform.localPosition = Vector3.zero;// transform.position;
            //buttonHighlight.Stop();
            //if (buttonHighlight.isPlaying)
            //    buttonHighlight.Stop();
            buttonHighlight.Play();
        }
        resourceAmountText.text = val.ToString();
        resourceAmount = val;
    }

    //private void SetActiveStatusFalse()
    //{
    //    gameObject.SetActive(false);
    //}

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
            originalParent = transform.parent;
            transform.SetParent(tempParent);
            transform.SetAsLastSibling();
            background.raycastTarget = false;
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
            transform.SetParent(originalParent);
            transform.localPosition = Vector3.zero;
            background.raycastTarget = true;
        }
    }
}
