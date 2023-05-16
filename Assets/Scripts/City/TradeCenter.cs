using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TradeCenter : MonoBehaviour
{
    private MapWorld world;
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

    //initial resources
    public List<ResourceValue> sellResources = new(); //resources to sell

    private void Awake()
    {
        highlight = GetComponent<SelectionHighlight>();

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

    public void EnableHighlight(Color highlightColor)
    {
        if (highlight.isGlowing)
            return;

        highlight.EnableHighlight(highlightColor, true);
    }

    public void DisableHighlight()
    {
        if (!highlight.isGlowing)
            return;

        highlight.DisableHighlight();
    }
}
