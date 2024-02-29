using TMPro;
using UnityEngine;

public class UITraderOrderHandler : MonoBehaviour
{
    [SerializeField]
    private MapWorld world;
    
    [SerializeField]
    private UISingleConditionalButtonHandler uiTradeRoute; //button to set up trade routes to cities

    [SerializeField]
    public UISingleConditionalButtonHandler uiBeginTradeRoute; //public to show button in other class

    //[SerializeField]
    //public UISingleConditionalButtonHandler uiLoadUnload; //button to load unload cargo for traders

    [SerializeField]
    private Transform uiElementsParent;

    [SerializeField]
    private RectTransform allContents;

    [SerializeField]
    private CanvasGroup allContents2;

    [SerializeField]
    private TMP_Text beginText; 

    [SerializeField]
    private Sprite beginRoute, stopRoute;

    private Vector3 originalLoc;

    private bool activeStatus; //set this up so we don't have to wait for tween to set inactive

    private void Awake()
    {
        uiTradeRoute.ToggleInteractable(true);
        uiBeginTradeRoute.ToggleInteractable(false);
        //uiLoadUnload.ToggleInteractable(false);

        gameObject.SetActive(false);
        originalLoc = allContents.anchoredPosition3D;
    }

    private void Start()
    {
        foreach (Transform selection in uiElementsParent)
        {
            //set visible as default is invisible
            selection.GetComponent<UISingleConditionalButtonHandler>().SetActiveStatusTrue();
        }
    }

    public void HandleR()
    {
        if (activeStatus)
            world.unitMovement.SetUpTradeRoute();
    }

    public void HandleF()
    {
        if (activeStatus)
            world.unitMovement.ShowTradeRouteCost();
    }

    public void ToggleVisibility(bool val, MapWorld world = null) //pass in world for canvas
    {
        if (activeStatus == val)
            return;

        LeanTween.cancel(gameObject);

        if (val)
        {
            SetActiveStatusTrue();
            activeStatus = true; 
            allContents.anchoredPosition3D = originalLoc + new Vector3(0, -600f, 0);

            LeanTween.moveY(allContents, allContents.anchoredPosition3D.y + 600f, 0.4f).setEaseOutBack();
            //LeanTween.alpha(allContents, 1f, 0.2f).setFrom(0f).setEaseLinear();
        }
        else
        {
            activeStatus = false;
            LeanTween.moveY(allContents, allContents.anchoredPosition3D.y - 600f, 0.2f).setOnComplete(() => SetActiveStatusFalse(world));
        }
    }

    private void SetActiveStatusTrue()
    {
        gameObject.SetActive(true);
    }

    private void SetActiveStatusFalse(MapWorld world)
    {
        gameObject.SetActive(false);
        world.traderCanvas.gameObject.SetActive(false);
        world.personalResourceCanvas.gameObject.SetActive(false);
    }

    public void SwitchRouteIcons(bool v)
    {
        uiBeginTradeRoute.ToggleButtonColor(v);
        beginText.text = v ? "Stop" : "Begin";

        if (v)
            uiBeginTradeRoute.buttonImage.sprite = stopRoute;
        else
            uiBeginTradeRoute.buttonImage.sprite = beginRoute;
    }
}
