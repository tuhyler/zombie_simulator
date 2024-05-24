using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TradeCenter : MonoBehaviour, ITradeStop, IGoldWaiter
{
    [HideInInspector]
    public MapWorld world;
    private SelectionHighlight highlight;
    [SerializeField]
    private CityNameField nameField;
    [SerializeField]
    public Transform main, lightHolder;
    [SerializeField]
    private List<MeshRenderer> tcMesh;
    private List<Material> originalMat = new();

    [SerializeField]
    private GameObject nameMap;
    [SerializeField]
    private List<Light> nightLights = new();
    [SerializeField]
    private GameObject tradeCenterPrefab;
    [SerializeField]
    public Transform minimapIcon;
    [SerializeField]
    public UnitBuildDataSO tradeCenterRep;
    [HideInInspector]
    public TradeRep tcRep;
    [SerializeField]
    public Vector3 tradeRepLoc;
    [HideInInspector]
    public float multiple = 1;

    //basic info
    public string tradeCenterName;
    public string tradeCenterDisplayName;
    public int cityPop;
    [HideInInspector]
    public Vector3Int mainLoc;
    [HideInInspector]
    public bool isDiscovered;
    
    public Dictionary<ResourceType, int> resourceSellDict = new();
    public Dictionary<ResourceType, int> resourceBuyDict = new();
    public Dictionary<ResourceType, int> resourceBuyGridDict = new();

    public Dictionary<SingleBuildType, Vector3Int> singleBuildDict = new();

    //initial resources to buy & sell
    public List<ResourceValue> buyResources = new();
    public List<ResourceValue> sellResources = new();

    int IGoldWaiter.goldNeeded => waitingAmount;
	Vector3Int IGoldWaiter.waiterLoc => mainLoc;

	//stop info
	ITradeStop stop;
	string ITradeStop.stopName => tradeCenterName;
    [HideInInspector]
    public List<Trader> waitList = new(), seaWaitList = new(), airWaitList = new();
	List<Trader> ITradeStop.waitList => waitList;
	List<Trader> ITradeStop.seaWaitList => seaWaitList;
	List<Trader> ITradeStop.airWaitList => airWaitList;
	Vector3Int ITradeStop.mainLoc => mainLoc;
	Dictionary<SingleBuildType, Vector3Int> ITradeStop.singleBuildLocDict => singleBuildDict;
	City ITradeStop.city => null;
	Wonder ITradeStop.wonder => null;
	TradeCenter ITradeStop.center => this;

    //for queuing unloading
    private List<Trader> goldWaiters = new();
    [HideInInspector]
    public int waitingAmount;

    private void Awake()
    {
        stop = this;
        highlight = GetComponentInChildren<SelectionHighlight>();

        for (int i = 0; i < tcMesh.Count; i++)
		    originalMat.Add(tcMesh[i].sharedMaterial);

        for (int i = 0; i < buyResources.Count; i++)
        {
            resourceBuyDict[buyResources[i].resourceType] = buyResources[i].resourceAmount;
            resourceBuyGridDict[buyResources[i].resourceType] = i;
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
        tcRep.gameObject.SetActive(true);
        tcRep.SetSomethingToSay(tcRep.tradeRepName + "_intro");

        foreach (Vector3Int tile in world.GetNeighborsFor(mainLoc, MapWorld.State.EIGHTWAYINCREMENT))
        {
            TerrainData td = world.GetTerrainDataAt(tile);

			if (!td.isDiscovered)
                td.Reveal();
        }

        foreach (ResourceType type in resourceSellDict.Keys)
        {
            if (!world.resourceDiscoveredList.Contains(type))
                world.DiscoverResource(type);
        }
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

    public void SetTradeCenterRep(bool load)
    {
        int rot = Mathf.RoundToInt(main.rotation.eulerAngles.y / 90);
        Vector3 tempTradeRepLoc = tradeRepLoc;

        switch (rot)
        {
            case 1:
                tempTradeRepLoc.x *= -1;
                break;
            case 2:
				tempTradeRepLoc.x *= -1;
				tempTradeRepLoc.z *= -1;
				break;
            case 3:
				tempTradeRepLoc.z *= -1;
				break;
        }
        
        Vector3 instantiateLoc = tempTradeRepLoc;
        Vector3 rotationDirection = Vector3Int.zero - instantiateLoc;
		Quaternion endRotation;
		if (rotationDirection == Vector3.zero)
			endRotation = Quaternion.identity;
		else
			endRotation = Quaternion.LookRotation(rotationDirection, Vector3.up);

		GameObject rep = Instantiate(Resources.Load<GameObject>("Prefabs/" + tradeCenterRep.prefabLoc), instantiateLoc, endRotation);
        rep.transform.SetParent(transform, false);
        tcRep = rep.GetComponent<TradeRep>();
        tcRep.SetUpTradeRep(world);
        tcRep.SetTradeCenter(this);
        
        if (!isDiscovered)
            tcRep.gameObject.SetActive(false);

        if (load)
            tcRep.LoadTradeRepData(GameLoader.Instance.gameData.allTCRepData[tcRep.tradeRepName]);
        CheckRapport();
    }

    public void CheckRapport()
    {
        if (tcRep.rapportScore == 5)
            multiple = 1 - tcRep.ecstaticDiscount * 0.01f;
        else if (tcRep.rapportScore > 2)
            multiple = 1 - tcRep.happyDiscount * 0.01f;
        else if (tcRep.rapportScore < -2)
            multiple = 1 + tcRep.angryIncrease * 0.01f;
        else
            multiple = 1;
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
            Vector3Int harborLoc = mainLoc;
            if (transform.rotation.eulerAngles.y == 0)
                harborLoc.z += -increment;
            else if (transform.rotation.eulerAngles.y == 90)
                harborLoc.x += -increment;
            else if (transform.rotation.eulerAngles.y == 180)
                harborLoc.z += increment;
            else if (transform.rotation.eulerAngles.y == 270)
                harborLoc.x += increment;

            singleBuildDict[SingleBuildType.TradeDepot] = mainLoc;
            singleBuildDict[SingleBuildType.Harbor] = harborLoc;
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

    public void ToggleClear(bool v)
    {
        //if (v)
        //{
        //    for (int i = 0; i < tcMesh.Count; i++)
        //        tcMesh[i].sharedMaterial = world.atlasSemiClear;
        //}
        //else
        //{
        //    for (int i = 0; i < tcMesh.Count; i++)
        //        tcMesh[i].sharedMaterial = originalMat[i];
        //}
    }

    public void SetWaiter(Trader trader, int amount, bool load)
    {
        if (goldWaiters.Count == 0)
            waitingAmount = amount;

        goldWaiters.Add(trader);

        if (!load)
            world.AddToGoldWaitList(this);
    }

    public bool RestartGold(int gold)
    {
        if (goldWaiters.Count > 0 && waitingAmount <= gold)
        {
			goldWaiters[0].tradeRouteManager.ContinueLoadingUnloading();
            goldWaiters.RemoveAt(0);

            if (goldWaiters.Count > 0)
                waitingAmount = goldWaiters[0].tradeRouteManager.goldAmount;

			return true;
        }
        else
        {
            return false;
        }
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

    public TradeCenterData SaveData()
    {
        TradeCenterData data = new();
        
        data.name = tradeCenterName;
        data.mainLoc = mainLoc;
        data.singleBuildDict = singleBuildDict;
        data.rotation = main.rotation;
        data.cityPop = cityPop;
        data.isDiscovered = isDiscovered;

		return data;
    }

    public List<int> SaveWaitListData(bool bySea, bool byAir, bool gold)
    {
        List<Trader> tempWaitList;
        if (gold)
            tempWaitList = goldWaiters;
        else if (bySea)
            tempWaitList = stop.seaWaitList;
        else if (byAir)
            tempWaitList = stop.airWaitList;
        else
            tempWaitList = stop.waitList;
        
        List<int> waitListOrder = new();
    
        for (int i = 0; i < tempWaitList.Count; i++)
	    	waitListOrder.Add(tempWaitList[i].id);

        return waitListOrder;
	}

	public void LoadData(TradeCenterData data)
    {
		mainLoc = data.mainLoc;
		singleBuildDict = data.singleBuildDict;
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
					stop.AddToWaitList(world.traderList[j], stop);
					break;
				}
			}
		}
	}
}
