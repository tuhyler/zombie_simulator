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
    [SerializeField]
    private GameObject tradeCenterPrefab;

    //basic info
    public string tradeCenterName;
    public string tradeCenterDisplayName;
    public int cityPop;
    [HideInInspector]
    public Vector3Int harborLoc, mainLoc;
    [HideInInspector]
    public bool isDiscovered;
    
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
    private Queue<Unit> waitList = new(), seaWaitList = new();
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
        isDiscovered = false;
    }

    public void Reveal()
    {
        gameObject.SetActive(true);
        isDiscovered = true;
        world.GetTerrainDataAt(harborLoc).Reveal();
    }

    public void SetWorld(MapWorld world)
    {
        this.world = world;
    }

    public void SetName()
    {
        nameField.cityName.text = tradeCenterDisplayName;
        nameField.SetCityNameFieldSize(tradeCenterDisplayName);
        nameField.SetNeutralBackground();
        nameMap.GetComponentInChildren<TMP_Text>().text = tradeCenterDisplayName;
        world.AddTradeCenterName(nameMap);
        nameMap.gameObject.SetActive(false);
    }

    public void SetPop(int pop)
    {
        cityPop = pop;
        nameField.SetCityPop(pop);
    }

    public void ClaimSpotInWorld(int increment, bool loading)
    {
        if (!loading)
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
        }

        world.AddToCityLabor(mainLoc, null);
        world.AddStructure(mainLoc, gameObject);

        foreach (Vector3Int loc in world.GetNeighborsFor(mainLoc, MapWorld.State.EIGHTWAYINCREMENT))
            world.AddToCityLabor(loc, null);
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
        if (unit.bySea)
        {
			if (!seaWaitList.Contains(unit))
				seaWaitList.Enqueue(unit);
		}
        else
        {
            if (!waitList.Contains(unit))
                waitList.Enqueue(unit);
        }
    }

    public void RemoveFromWaitList(Unit unit)
    {
        List<Unit> waitListList = unit.bySea ? seaWaitList.ToList() : waitList.ToList();

        if (!waitListList.Contains(unit))
        {
            if (unit.bySea)
                CheckSeaQueue();
            else
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

        if (unit.bySea)
            seaWaitList = new Queue<Unit>(waitListList);
        else
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

    public void CheckSeaQueue()
    {
		if (seaWaitList.Count > 0)
		{
			seaWaitList.Dequeue().ExitLine();
		}

		if (seaWaitList.Count > 0)
		{
			int i = 0;
			foreach (Unit unit in seaWaitList)
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

    public void EnableHighlight(Color highlightColor, bool newGlow)
    {
        if (highlight.isGlowing)
            return;

        highlight.EnableHighlight(highlightColor, newGlow);
    }

    public void DisableHighlight()
    {
        if (!highlight.isGlowing)
            return;

        highlight.DisableHighlight();
    }

    public void SetData()
    {

    }

    public TradeCenterData SaveData()
    {
        TradeCenterData data = new();
        
        data.name = tradeCenterName;
        data.mainLoc = mainLoc;
        data.harborLoc = harborLoc;
        data.rotation = transform.rotation;
        data.cityPop = cityPop;
        data.isDiscovered = isDiscovered;

		return data;
    }

    public List<int> SaveWaitListData(bool bySea)
    {
		List<Unit> tempWaitList = bySea ? seaWaitList.ToList() : waitList.ToList();
        List<int> waitListOrder = new();

		for (int i = 0; i < tempWaitList.Count; i++)
			waitListOrder.Add(tempWaitList[i].id);

        return waitListOrder;
	}

	public void LoadData(TradeCenterData data)
    {
		mainLoc = data.mainLoc;
		harborLoc = data.harborLoc;
		transform.rotation = data.rotation;
		//tradeCenterName = data.name; //done elsewhere
  //      cityPop = data.cityPop;
		isDiscovered = data.isDiscovered;
	}

	public void SetWaitList(List<int> waitList)
	{
		for (int i = 0; i < waitList.Count; i++)
		{
			for (int j = 0; j < world.traderList.Count; j++)
			{
				if (world.traderList[j].id == waitList[i])
				{
					this.waitList.Enqueue(world.traderList[j]);
					break;
				}
			}
		}
	}

	public void SetSeaWaitList(List<int> seaWaitList)
	{
		for (int i = 0; i < seaWaitList.Count; i++)
		{
			for (int j = 0; j < world.traderList.Count; j++)
			{
				if (world.traderList[j].id == seaWaitList[i])
				{
					this.seaWaitList.Enqueue(world.traderList[j]);
					break;
				}
			}
		}
	}
}
