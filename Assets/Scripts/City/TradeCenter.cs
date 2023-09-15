using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class TradeCenter : MonoBehaviour
{
    [HideInInspector]
    public MapWorld world;
    private SelectionHighlight highlight;
    [SerializeField]
    private CityNameField nameField;

    [SerializeField]
    private GameObject nameMap;
    [SerializeField]
    private List<Light> nightLights = new();

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
        highlight = GetComponentInChildren<SelectionHighlight>();

        int i = 0;
        foreach (ResourceValue value in buyResources)
        {
            resourceBuyDict[value.resourceType] = value.resourceAmount;
            resourceBuyGridDict[value.resourceType] = i;
            i++;
        }

        foreach (ResourceValue value in sellResources)
            resourceSellDict[value.resourceType] = value.resourceAmount;

        nameMap.GetComponentInChildren<TMP_Text>().outlineWidth = 0.35f;
        nameMap.GetComponentInChildren<TMP_Text>().outlineColor = Color.black;
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public void Reveal()
    {
        gameObject.SetActive(true);
    }

    public void SetWorld(MapWorld world)
    {
        this.world = world;
    }

    public void SetName(string name)
    {
        tradeCenterName = name;
        nameField.cityName.text = name;
        nameField.SetCityNameFieldSize(name);
        nameField.SetNeutralBackground();
        nameMap.GetComponentInChildren<TMP_Text>().text = name;
        world.AddTradeCenterName(nameMap);
        nameMap.gameObject.SetActive(false);
    }

    public void SetPop(int pop)
    {
        nameField.SetCityPop(pop);
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

    public void ToggleLights(bool v)
    {
        foreach (Light light in nightLights)
        {
            light.gameObject.SetActive(v);
        }
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

    public void RemoveFromWaitList(Unit unit)
    {
        List<Unit> waitListList = waitList.ToList();

        if (!waitListList.Contains(unit))
        {
            CheckQueue();
            return;
        }

        int index = waitListList.IndexOf(unit);
        waitListList.Remove(unit);

        int j = 0;
        for (int i = index; i < waitListList.Count; i++)
        {
            j++;
            waitListList[i].waitingCo = StartCoroutine(waitListList[i].MoveUpInLine(j));
        }

        waitList = new Queue<Unit>(waitListList);
        //waitList = new Queue<Unit>(waitList.Where(x => x != unit));
    }

    public void CheckQueue()
    {
        if (waitList.Count > 0)
        {
            waitList.Dequeue().ExitLine();
        }

        if (waitList.Count > 0)
        {
            int i = 0;
            foreach (Unit unit in waitList)
            {
                i++;
                unit.waitingCo = StartCoroutine(unit.MoveUpInLine(i));
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

        highlight.EnableHighlight(highlightColor);
    }

    public void DisableHighlight()
    {
        if (!highlight.isGlowing)
            return;

        highlight.DisableHighlight();
    }
}
