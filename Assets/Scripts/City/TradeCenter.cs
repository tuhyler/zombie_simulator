using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TradeCenter : MonoBehaviour
{
    [HideInInspector]
    public MapWorld world;
    private SelectionHighlight highlight;
    [SerializeField]
    private CityNameField nameField;

    //basic info
    [HideInInspector]
    public string tradeCenterName;
    [HideInInspector]
    public Vector3Int harborLoc, mainLoc;
    
    private Dictionary<ResourceType, int> resourceSellDict = new();
    public Dictionary<ResourceType, int> ResourceSellDict { get { return resourceSellDict; } set { resourceSellDict = value; } }

    private Dictionary<ResourceType, int> resourceBuyDict = new();
    public Dictionary<ResourceType, int> ResourceBuyDict { get { return resourceBuyDict; } set { resourceBuyDict = value; } }
    private Dictionary<ResourceType, int> resourceBuyGridDict = new();
    public Dictionary<ResourceType, int> ResourceBuyGridDict { get { return resourceBuyGridDict; } }

    //initial resources to buy & sell
    public List<ResourceValue> buyResources = new();
    public List<ResourceValue> sellResources = new();

    //for queuing unloading
    private Queue<Unit> waitList = new();
    private TradeRouteManager tradeRouteWaiter;
    private int waitingAmount;

    private void Awake()
    {
        highlight = GetComponent<SelectionHighlight>();

        int i = 0;
        foreach (ResourceValue value in buyResources)
        {
            resourceBuyDict[value.resourceType] = value.resourceAmount;
            resourceBuyGridDict[value.resourceType] = i;
            i++;
        }

        foreach (ResourceValue value in sellResources)
            resourceSellDict[value.resourceType] = value.resourceAmount;
    }

    public void SetWorld(MapWorld world)
    {
        this.world = world;
    }

    public void SetName(string name)
    {
        tradeCenterName = name;
        nameField.cityName.text = name;
    }

    public void ClaimSpotInWorld(int increment)
    {
        mainLoc = world.RoundToInt(transform.position);
        harborLoc = mainLoc;
        if (transform.rotation.eulerAngles.y == 0)
            harborLoc.z += -increment;
        else if (transform.rotation.eulerAngles.y == 90)
            harborLoc.x += -increment;
        else if (transform.rotation.eulerAngles.y == 180)
            harborLoc.z += increment;
        else if (transform.rotation.eulerAngles.y == 270)
            harborLoc.x += increment;

        world.AddToCityLabor(mainLoc, gameObject);
        world.AddStructure(mainLoc, gameObject);
    }

    public void SetWaiter(TradeRouteManager tradeRouteManager, int amount)
    {
        tradeRouteWaiter = tradeRouteManager;
        waitingAmount = amount;
        world.AddToGoldTradeCenterWaitList(this);
    }

    private void CheckGoldWaiter()
    {
        if (tradeRouteWaiter)
        {
            tradeRouteWaiter.resourceCheck = false;
            tradeRouteWaiter = null;
        }
    }

    public void AddToWaitList(Unit unit)
    {
        if (!waitList.Contains(unit))
            waitList.Enqueue(unit);
    }

    public void CheckQueue()
    {
        if (waitList.Count > 0)
        {
            waitList.Dequeue().MoveUpInLine();
        }

        if (waitList.Count > 0)
        {
            foreach (Unit unit in waitList)
            {
                unit.MoveUpInLine();
            }
        }
    }

    public void GoldCheck()
    {
        if (world.CheckWorldGold(waitingAmount))
        {
            CheckGoldWaiter();
        }
        else
        {
            world.AddToGoldTradeCenterWaitList(this);
        }
    }

    public void RemoveFromWaitList()
    {
        world.RemoveTradeCenterFromWaitList(this);
    }

    public void EnableHighlight(Color highlightColor)
    {
        if (highlight.isGlowing)
            return;

        highlight.EnableHighlight(highlightColor, false);
    }

    public void DisableHighlight()
    {
        if (!highlight.isGlowing)
            return;

        highlight.DisableHighlight();
    }
}
