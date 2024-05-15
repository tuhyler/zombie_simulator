using UnityEngine;
using UnityEngine.UI;

public class UIResourceSquare : MonoBehaviour
{
    private ResourceType resourceType;
    private UIResourceSelectionGrid resourceGrid;
    private UITooltipTrigger tooltipTrigger;
    public Image resourceIcon;

    private void Awake()
    {
        tooltipTrigger = GetComponentInChildren<UITooltipTrigger>();
    }

    public void SetInfo(ResourceType type, string name)
    {
        resourceType = type;
        tooltipTrigger.SetMessage(name);
    }
    
    public void SetGrid(UIResourceSelectionGrid resourceGrid)
    {
        this.resourceGrid = resourceGrid;
    }

    public void ChooseResourceType()
    {
        resourceGrid.world.cityBuilderManager.PlaySelectAudio();
        resourceGrid.ChooseResourceType(resourceType);
        tooltipTrigger.CancelCall();
    }
}
