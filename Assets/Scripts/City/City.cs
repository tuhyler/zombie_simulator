using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class City : MonoBehaviour
{
    private GameObject unitToProduce;

    //[SerializeField]
    //private MeshRenderer selectionCircle;

    [SerializeField]
    private GameObject housingPrefab;
    private GameObject currentHouse;

    [SerializeField]
    private Material cityNameMaterial;

    private Material originalCityNameMaterial;//, originalCityStatMaterial;

    [SerializeField]
    private SpriteRenderer cityNameField;//, citySizeField, unusedLaborField;

    [SerializeField]
    private TMP_Text cityName;
    public string CityName { get { return cityName.text; } }

    [HideInInspector]
    public Vector3Int cityLoc;
    [HideInInspector]
    public bool activeCity;
    
    private MapWorld world;

    private ResourceManager resourceManager;
    public ResourceManager ResourceManager { get { return resourceManager; } }

    //private ResourceProducer resourceProducer;

    [HideInInspector]
    public CityPopulation cityPop;

    //foodConsumed info
    public int unitFoodConsumptionPerTurn = 1, secondsTillGrowthCheck = 60; //how much foodConsumed one unit eats per turn
    private int foodConsumptionPerTurn; //total 
    public int FoodConsumptionPerMinute { get { return foodConsumptionPerTurn; } set { foodConsumptionPerTurn = value; } }
    private string minutesTillGrowth; //string in case there's no growth
    public string GetMinutesTillGrowth { get { return minutesTillGrowth; } }
    private Coroutine foodConsumedCo;
    private int countDownTimer;
    public int CountDownTimer { get { return countDownTimer; } }

    //resource info
    private float workEthic = 1.0f;
    public float GetSetWorkEthic { get { return workEthic; } set { workEthic = value; } }
    public int warehouseStorageLimit = 200;
    private TradeRouteManager tradeRouteWaiter;
    private ResourceType resourceWaiter = ResourceType.None;
    private Queue<Unit> waitList = new();
    
    //world resource info
    //private int goldPerMinute;
    //public int GetGoldPerMinute { get { return goldPerMinute; } }
    private int researchPerMinute;
    public int GetResearchPerMinute { get { return researchPerMinute; } }

    //stored queue items
    [HideInInspector]
    public List<UIQueueItem> savedQueueItems = new();

    //private SelectionHighlight highlight; //Highlight doesn't work on city name text

    private void Awake()
    {
        //selectionCircle.GetComponent<MeshRenderer>().enabled = false;
        world = FindObjectOfType<MapWorld>();
        cityPop = GetComponent<CityPopulation>();
        resourceManager = GetComponent<ResourceManager>();
        resourceManager.ResourceStorageLimit = warehouseStorageLimit;
        //resourceProducer = GetComponent<ResourceProducer>();
        resourceManager.SetCity(this);
        //resourceProducer.SetResourceManager(resourceManager);

        cityPop.IncreasePopulation();
        
        cityLoc = Vector3Int.RoundToInt(transform.position);
        //highlight = GetComponent<SelectionHighlight>();

        originalCityNameMaterial = cityNameField.material;
        //originalCityStatMaterial = citySizeField.material;
    }

    private void Start()
    {
        UpdateCityPopInfo();
        if (cityPop.GetPop >= 1)
            StartCoroutine(FoodConsumptionCoroutine());
    }


    private void EnableHighlight()
    {
        cityNameField.material = cityNameMaterial;
        //citySizeField.material = cityNameMaterial;
        //unusedLaborField.material = cityNameMaterial;
        //Debug.Log("Size is " + cityNameField.size);
    }

    private void DisableHighlight()
    {
        cityNameField.material = originalCityNameMaterial;
        //citySizeField.material = originalCityStatMaterial;
        //unusedLaborField.material = originalCityStatMaterial;
    }

    public bool CheckCityName(string cityName)
    {
        return world.IsCityNameTaken(cityName);
    }

    public void SetNewCityName()
    {
        bool approvedName = false;
        int cityCount = world.CityCount();
        string cityName = "";

        while (!approvedName)
        {
            cityCount++;
            cityName = "City_" + cityCount.ToString();

            if (!world.CheckCityName(cityName))
            {
                approvedName = true;
            }
        }

        SetCityNameFieldSize(cityName);
        SetCityName(cityName);
        AddCityNameToWorld();
    }

    private void SetCityNameFieldSize(string cityName)
    {
        float wordLength = cityName.Length;
        cityNameField.size = new Vector2(wordLength * 0.18f + 0.7f, 1.0f);
    }

    public void SetCityName(string cityName)
    {
        this.cityName.text = cityName;
    }

    public void AddCityNameToWorld()
    {
        world.AddCityName(cityName.text, cityLoc);
    }

    public void RemoveCityName()
    {
        world.RemoveCityName(cityLoc);
    }

    public void UpdateCityName(string newCityName)
    {
        SetCityNameFieldSize(newCityName);
        SetCityName(newCityName);
        AddCityNameToWorld();
    }

    //update city info after changing foodConsumed info
    public void UpdateCityPopInfo()
    {
        //cityPopText.text = cityPop.GetPop.ToString();
        //cityLaborText.text = cityPop.GetSetUnusedLabor.ToString();
        foodConsumptionPerTurn = cityPop.GetPop * unitFoodConsumptionPerTurn;
        resourceManager.ModifyResourceConsumptionPerMinute(ResourceType.Food, foodConsumptionPerTurn, true);
        float foodPerMinute = resourceManager.GetResourceGenerationValues(ResourceType.Food);
        int foodStorage = resourceManager.FoodGrowthLevel;

        if (foodPerMinute > 0)
        {
            minutesTillGrowth = Mathf.CeilToInt(((float)resourceManager.FoodGrowthLimit - foodStorage) / foodPerMinute).ToString();
        }
        if (foodPerMinute < 0) 
        {
            minutesTillGrowth = Mathf.FloorToInt(foodStorage / foodPerMinute).ToString(); //maybe take absolute value, change color to red?
        }
        if (foodPerMinute == 0)
        {
            minutesTillGrowth = "-";
        }
    }

    public void SetHouse()
    {
        if (currentHouse != null)
            Destroy(currentHouse);

        Instantiate(housingPrefab, transform.position, Quaternion.identity);
    }

    public void SelectUnitToProduce(GameObject unitToProduce)
    {
        this.unitToProduce = unitToProduce;
        //if (destroyedCity)
        CompleteProduction();
    }

    private void CompleteProduction()
    {
        if (unitToProduce == null)
            return;

        Vector3Int buildPosition = cityLoc;
        if (world.IsUnitLocationTaken(buildPosition)) //placing unit in world after building in city
        {
            //List<Vector3Int> newPositions = world.GetNeighborsFor(Vector3Int.FloorToInt(buildPosition));
            foreach (Vector3Int pos in world.GetNeighborsFor(buildPosition, MapWorld.State.EIGHTWAYTWODEEP))
            {
                if (!world.IsUnitLocationTaken(pos) && world.GetTerrainDataAt(pos).GetTerrainData().walkable)
                {
                    buildPosition = pos;
                    break;
                }
            }

            if (buildPosition == Vector3Int.RoundToInt(transform.position))
            {
                Debug.Log("No suitable locations to build unit");
                return;
            }
        }

        Vector3 buildPositionFinal = buildPosition;
        buildPositionFinal.y += .5f;
        GameObject unitGO = Instantiate(unitToProduce, buildPositionFinal, Quaternion.identity); //produce unit at specified position
        unitGO.name = unitGO.name.Replace("(Clone)", ""); //getting rid of the clone part in name 
        Unit unit = unitGO.GetComponent<Unit>();

        unit.CurrentLocation = world.AddUnitPosition(buildPosition, unit);
    }

    public void PopulationGrowthCheck()
    {
        cityPop.IncreasePopulationAndLabor();
        resourceManager.IncreaseFoodConsumptionPerTurn(true);
        UpdateCityPopInfo();

        if (cityPop.GetPop == 1)
            StartCoroutine(FoodConsumptionCoroutine());
    }

    public void PopulationDeclineCheck()
    {
        cityPop.DecreasePopulation();

        if (cityPop.GetSetUnusedLabor > 0) //if unused labor, get rid of first
            cityPop.DecreaseUnusedLabor();
        else
        {
            StopFoodConsumptionCoroutine();
            System.Random random = new();
            int randomLabor = random.Next(cityPop.GetSetUsedLabor); //randomly choosing by weight between field and city labor

            if (randomLabor < cityPop.GetSetFieldLaborers)
                RemoveRandomFieldLaborer(random);
            else
                RemoveRandomCityLaborer(random);
        }

        resourceManager.IncreaseFoodConsumptionPerTurn(false);
        UpdateCityPopInfo();
    }

    private void RemoveRandomFieldLaborer(System.Random random)
    {
        List<Vector3Int> workedTiles = world.GetWorkedCityRadiusFor(world.GetClosestTile(transform.position), gameObject);

        //below is giving every labor in any tile equal chance of being chosen
        int currentLabor = 0;
        Dictionary<int, Vector3Int> laborByTile = new();
        foreach (Vector3Int tile in workedTiles)
        {
            int prevLabor = currentLabor;
            currentLabor += world.GetCurrentLaborForTile(tile);
            for (int i = prevLabor; i < currentLabor; i++)
            {
                laborByTile[currentLabor] = tile;
            }
        }

        Vector3Int chosenTile = laborByTile[random.Next(currentLabor)];
        //above is giving labor in any tile equal chance of being chosen
        //Vector3Int chosenTile = workedTiles[random.Next(workedTiles.Count)]; //equal chance of being chosen, regardless of labor size

        int labor = world.GetCurrentLaborForTile(chosenTile);
        labor--;

        if (labor == 0) //removing from world dicts when zeroed out
        {
            world.RemoveFromCurrentWorked(chosenTile);
            world.RemoveFromCityLabor(chosenTile);
            //resourceManager.RemoveKeyFromGenerationDict(chosenTile);
        }
        else
        {
            world.AddToCurrentFieldLabor(chosenTile, labor);
        }
    }

    private void RemoveRandomCityLaborer(System.Random random)
    {
        List<string> buildingNames = world.GetBuildingListForCity(cityLoc);

        //below is giving every labor in any building equal chance of being chosen
        int currentLabor = 0;
        Dictionary<int, string> laborByBuilding = new();
        foreach (string buildingName in buildingNames)
        {
            int prevLabor = currentLabor;
            currentLabor += world.GetCurrentLaborForBuilding(cityLoc, buildingName);
            for (int i = prevLabor; i < currentLabor; i++)
            {
                laborByBuilding[currentLabor] = buildingName;
            }
        }

        string chosenBuildingName = laborByBuilding[random.Next(currentLabor)];
        //above is giving labor in any building equal chance of being chosen

        //string chosenBuildingName = buildingNames[random.Next(buildingNames.Count)]; //equal chance of being chosen, regardless of labor size
        
        int labor = world.GetCurrentLaborForBuilding(cityLoc, chosenBuildingName);
        labor--;

        if (labor == 0) //removing from world dicts when zeroed out
        {
            world.RemoveFromBuildingCurrentWorked(cityLoc, chosenBuildingName);
            //resourceManager.RemoveKeyFromBuildingGenerationDict(chosenBuildingName);
        }
        else
        {
            world.AddToCurrentBuildingLabor(cityLoc, chosenBuildingName, labor);
        }
    }

    public void SetWaiter(TradeRouteManager tradeRouteManager, ResourceType resourceType = ResourceType.None)
    {
        tradeRouteWaiter = tradeRouteManager;
        resourceWaiter = resourceType;
    }

    public void CheckResourceWaiter(ResourceType resourceType)
    {
        if (tradeRouteWaiter != null && resourceWaiter == resourceType)
        {
            tradeRouteWaiter.resourceCheck = false;
            tradeRouteWaiter = null;
            resourceWaiter = ResourceType.None;
        }
    }

    public void CheckLimitWaiter()
    {
        if (tradeRouteWaiter != null && resourceWaiter == ResourceType.None)
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
            foreach(Unit unit in waitList)
            {
                unit.MoveUpInLine();
            }
        }
    }

    public void ChangeWorkEthic(float change)
    {
        workEthic += change;
    }


    //world resource manager
    public bool CheckIfWorldResource(ResourceType resourceType) //seeing if its world resource
    {
        return world.WorldResourcePrep().Contains(resourceType);
    }

    public void UpdateWorldResourceGeneration(ResourceType resourceType, float diffAmount, bool add)
    {
        world.UpdateWorldResourceGeneration(resourceType, diffAmount, add);

        if (resourceType == ResourceType.Research)
        {
            if (add)
                researchPerMinute += Mathf.RoundToInt(diffAmount);
            else
                researchPerMinute += Mathf.RoundToInt(diffAmount);
        }
    }

    //private void UpdateInfoPanel(ResourceType resourceType, float diffAmount)
    //{
    //    if (resourceType == ResourceType.Gold)
    //    {
    //        goldPerMinute += Mathf.RoundToInt(diffAmount);
    //    }
    //    if (resourceType == ResourceType.Research)
    //    {
    //        researchPerMinute += Mathf.RoundToInt(diffAmount);
    //    }
    //}


    //Time generator to consume food
    private IEnumerator FoodConsumptionCoroutine()
    {
        countDownTimer = secondsTillGrowthCheck;
        
        while (countDownTimer > 0)
        {
            yield return new WaitForSeconds(1);
            countDownTimer--;
            if (activeCity)
                resourceManager.uiInfoPanelCity.SetTimer(countDownTimer);
        }

        ResourceValue foodConsumed;
        foodConsumed.resourceType = ResourceType.Food;
        foodConsumed.resourceAmount = foodConsumptionPerTurn;

        //consume before checking for growth
        resourceManager.ConsumeResources(new List<ResourceValue> { foodConsumed }, 1);
        resourceManager.CheckForPopGrowth();

        Debug.Log(cityName + " is checking for growth");
        StartCoroutine(FoodConsumptionCoroutine());
    }

    private void StopFoodConsumptionCoroutine()
    {
        if (foodConsumedCo != null)
        {
            StopCoroutine(foodConsumedCo);
        }
    }

    //for queued build items
    public void RemoveFirstFromQueue(CityBuilderManager cityBuilderManager)
    {
        UIQueueItem item = savedQueueItems[0];
        savedQueueItems.Remove(item);
        Destroy(item);

        if (savedQueueItems.Count > 0)
        {
            UIQueueItem nextItem = savedQueueItems[0];
            
            List<ResourceValue> resourceCosts = new();

            if (nextItem.unitBuildData != null)
                resourceCosts = new(nextItem.unitBuildData.unitCost);
            if (nextItem.improvementData != null)
                resourceCosts = new(nextItem.improvementData.improvementCost);

            resourceManager.SetQueueResources(resourceCosts, cityBuilderManager);
        }
    }

    public UIQueueItem GetBuildInfo()
    {
        return savedQueueItems[0];
    }


    //public void CheckIfBuiltItemIsQueued(Vector3Int loc, ImprovementDataSO improvementData)
    //{
    //    foreach (UIQueueItem item in savedQueueItems)
    //    {
    //        if (item.itemName == improvementData.improvementName && item.buildLoc == loc)
    //        {
    //            savedQueueItems.Remove(item);
    //            return;
    //        }
    //    }
    //}


    internal void Select()
    {
        EnableHighlight();
        //selectionCircle.enabled = true;
        //highlight.ToggleGlow(true);
    }

    public void Deselect()
    {
        DisableHighlight();
        //selectionCircle.enabled = false;
        //highlight.ToggleGlow(false);
    }
}
