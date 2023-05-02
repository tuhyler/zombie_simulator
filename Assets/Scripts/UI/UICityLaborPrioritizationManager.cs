using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UICityLaborPrioritizationManager : MonoBehaviour
{
    [SerializeField]
    private CityBuilderManager cityBuilderManager;

    [SerializeField]
    private GameObject uiLaborResourcePriorityHolder, uiLaborResourcePriority;

    [SerializeField]
    private Transform resourceHolder;

    //for generating resource lists
    private List<TMP_Dropdown.OptionData> resources = new();

    [SerializeField] //for tweening
    private RectTransform allContents;
    [HideInInspector]
    public bool activeStatus;
    private Vector3 originalLoc;

    //List of all displayed resource priorities
    [HideInInspector]
    public List<UILaborResourcePriority> resourcePriorityList = new();
    private Dictionary<int, UILaborResourcePriorityHolder> resourcePriorityHolderDict = new();

    private City city;

    [SerializeField]
    private Image openPrioritizationImage;
    [SerializeField]
    private Sprite buttonLeft;
    private Sprite buttonRight;

    private void Awake()
    {
        originalLoc = allContents.anchoredPosition3D;
        AddResources();
        buttonRight = openPrioritizationImage.sprite;
        gameObject.SetActive(false);
    }

    public void LoadLaborPrioritizationInfo()
    {
        List<ResourceType> resourcePriorities = city.ResourcePriorities;

        foreach (ResourceType resourceType in resourcePriorities)
        {
            UILaborResourcePriority uiLaborResource = AddResourcePriority();
            uiLaborResource.SetCaptionResourceInfo(resourceType);
        }
    }

    private void AddResources()
    {
        foreach (ResourceIndividualSO resource in ResourceHolder.Instance.allStorableResources.Concat(ResourceHolder.Instance.allWorldResources).ToList())
        {
            resources.Add(new TMP_Dropdown.OptionData(resource.resourceName, resource.resourceIcon));
        }
    }

    public void ToggleVisibility(bool v, bool exitCity = false)
    {
        if (activeStatus == v)
            return;

        LeanTween.cancel(gameObject);

        if (v)
        {
            gameObject.SetActive(v);
            openPrioritizationImage.sprite = buttonLeft;
            activeStatus = true;

            allContents.anchoredPosition3D = originalLoc + new Vector3(-400f, 0, 0);

            LeanTween.moveX(allContents, allContents.anchoredPosition3D.x + 400f, 0.3f).setEaseOutSine();
            //LeanTween.alpha(allContents, 1f, 0.3f).setFrom(0f).setEaseLinear(); //don't like alpha fading here
        }
        else
        {
            activeStatus = false;
            city = null;
            openPrioritizationImage.sprite = buttonRight;
            
            if (!exitCity)
                LeanTween.moveX(allContents, allContents.anchoredPosition3D.x + -400f, 0.3f).setOnComplete(SetActiveStatusFalse);
            else
                LeanTween.moveY(allContents, allContents.anchoredPosition3D.y + -950f, 0.3f).setOnComplete(SetActiveStatusFalse);
        }
    }

    private void SetActiveStatusFalse()
    {
        foreach (UILaborResourcePriority uiLaborResourcePriority in resourcePriorityList)
        {
            Destroy(uiLaborResourcePriority.gameObject);//.RemoveWindow();
        }

        foreach (int i in resourcePriorityHolderDict.Keys)
        {
            Destroy(resourcePriorityHolderDict[i].gameObject);
        }

        resourcePriorityList.Clear();
        resourcePriorityHolderDict.Clear();

        gameObject.SetActive(false);
    }

    public void PrepareLaborPrioritizationMenu(City city)
    {
        this.city = city;
    }

    public void AddResourcePriorityButton() //added this as a method attached to button as it can't return anything
    {
        if (resourcePriorityList.Count >= resources.Count) //limit
        {
            InfoPopUpHandler.WarningMessage().Create(city.cityLoc, "Max resources");
            return;
        }

        AddResourcePriority();
    }

    private UILaborResourcePriority AddResourcePriority() //showing a new resource priority panel
    {
        GameObject newHolder = Instantiate(uiLaborResourcePriorityHolder);
        newHolder.transform.SetParent(resourceHolder, false);

        UILaborResourcePriorityHolder newResourceHolder = newHolder.GetComponent<UILaborResourcePriorityHolder>();
        newResourceHolder.loc = resourcePriorityList.Count;
        resourcePriorityHolderDict[resourcePriorityList.Count] = newResourceHolder;

        GameObject newPriority = Instantiate(uiLaborResourcePriority);
        newPriority.transform.SetParent(newHolder.transform, false);

        UILaborResourcePriority newResourcePriority = newPriority.GetComponent<UILaborResourcePriority>();
        newResourceHolder.resource = newResourcePriority;
        newResourcePriority.AddResources(resources);
        newResourcePriority.SetInitialPriority(resourcePriorityList.Count+1); //set priority before attaching to this
        newResourcePriority.SetCityLaborPrioritizationManager(this);

        return newResourcePriority;
    }

    public void AddToResourcePriorityList(int priority, UILaborResourcePriority uiLaborResourcePriority)
    {
        resourcePriorityList.Insert(priority-1, uiLaborResourcePriority);
    }

    public void MovePriorityUp(UILaborResourcePriority uiLaborResourcePriority, int priority)
    {
        UILaborResourcePriority priorityAbove = resourcePriorityList[priority - 1];
        priorityAbove.SetPriority(priorityAbove.currentPriorityNumber + 1);
        resourcePriorityList.Remove(uiLaborResourcePriority);
        resourcePriorityList.Insert(priority-1, uiLaborResourcePriority);
        resourcePriorityHolderDict[priority-1].MoveStop(resourcePriorityHolderDict[priority]);

        priorityAbove.transform.SetParent(resourcePriorityHolderDict[priority - 1].transform, false);
        Vector3 newLoc = resourcePriorityHolderDict[priority - 1].transform.position;
        resourcePriorityHolderDict[priority - 1].resource = priorityAbove;
        //priorityAbove.currentPriorityNumber = priority - 1;

        LeanTween.move(priorityAbove.gameObject, newLoc, 0.2f).setEaseOutSine().setOnComplete(() => { SetZeroLoc(priorityAbove); });
        priorityAbove.transform.localPosition = Vector3.zero;
    }


    public void MovePriorityDown(UILaborResourcePriority uiLaborResourcePriority, int priority)
    {
        UILaborResourcePriority priorityBelow = resourcePriorityList[priority - 1];
        priorityBelow.SetPriority(priorityBelow.currentPriorityNumber - 1);
        resourcePriorityList.Remove(uiLaborResourcePriority);
        resourcePriorityList.Insert(priority-1, uiLaborResourcePriority);
        resourcePriorityHolderDict[priority - 1].MoveStop(resourcePriorityHolderDict[priority-2]);

        priorityBelow.transform.SetParent(resourcePriorityHolderDict[priority - 1].transform, false);
        Vector3 newLoc = resourcePriorityHolderDict[priority - 1].transform.position;
        resourcePriorityHolderDict[priority - 1].resource = priorityBelow;
        //priorityAbove.currentPriorityNumber = priority - 1;

        LeanTween.move(priorityBelow.gameObject, newLoc, 0.2f).setEaseOutSine().setOnComplete(() => { SetZeroLoc(priorityBelow); });
        priorityBelow.transform.localPosition = Vector3.zero;
    }

    public void MovePriority(int loc, bool up)
    {
        UILaborResourcePriority priority = up ? resourcePriorityList[loc] : resourcePriorityList[loc - 2];
        resourcePriorityList.Remove(priority);
        resourcePriorityList.Insert(loc - 1, priority);

        if (up)
        {
            resourcePriorityList[loc].priorityNumber.text = (loc + 1).ToString();
            resourcePriorityList[loc].currentPriorityNumber = loc + 1;
            resourcePriorityHolderDict[loc - 1].MoveStop(resourcePriorityHolderDict[loc]);
        }
        else
        {
            resourcePriorityList[loc - 2].priorityNumber.text = (loc - 1).ToString();
            resourcePriorityList[loc - 2].currentPriorityNumber = loc - 1;
            resourcePriorityHolderDict[loc - 1].MoveStop(resourcePriorityHolderDict[loc - 2]);
        }

        priority.transform.SetParent(resourcePriorityHolderDict[loc - 1].transform);
        Vector3 newLoc = resourcePriorityHolderDict[loc - 1].transform.position;
        resourcePriorityHolderDict[loc - 1].resource = priority;

        LeanTween.move(priority.gameObject, newLoc, 0.2f).setEaseOutSine().setOnComplete(() => { SetZeroLoc(priority); });
        //priority.transform.localPosition = Vector3.zero;
    }

    public void SetZeroLoc(UILaborResourcePriority resource)
    {
        resource.transform.localPosition = Vector3.zero;
    }

    public void RemoveFromResourcePriorityList(UILaborResourcePriority uiLaborResourcePriority)
    {
        int priority = resourcePriorityList.IndexOf(uiLaborResourcePriority);
        resourcePriorityList.Remove(uiLaborResourcePriority);
        
        UILaborResourcePriorityHolder holder = resourcePriorityHolderDict[priority];
        resourcePriorityHolderDict.Remove(priority);
        Destroy(holder.gameObject);

        //moving one down for the priority numbers for all resources below the one removed
        for (int i = priority; i < resourcePriorityList.Count; i++)
        {
            UILaborResourcePriority priorityBelow = resourcePriorityList[i];
            priorityBelow.SetPriority(priorityBelow.currentPriorityNumber - 1);
        }
    }

    public void SetLaborPriorities()
    {
        List<ResourceType> resourcePriorities = new();

        foreach (UILaborResourcePriority uiLaborResource in resourcePriorityList)
        {
            ResourceType resourceType = uiLaborResource.GetChosenResource();
            //uiLaborResource.RemoveWindow();
            if (resourceType == ResourceType.None || resourcePriorities.Contains(resourceType))
                continue;
            resourcePriorities.Add(resourceType);
        }

        //resourcePriorityList.Clear();
        city.ResourcePriorities = resourcePriorities;

        //if (resourcePriorities.Count > 0)
        //{
        //    city.AutoAssignmentsForLabor();
        //    cityBuilderManager.UpdateCityLaborUIs();
        //}

        ToggleVisibility(false);
    }
}
