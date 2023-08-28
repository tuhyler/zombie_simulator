using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIResearchReward : MonoBehaviour
{
    [SerializeField]
    public Image rewardIcon, rewardBackground;

    [SerializeField]
    private Sprite selectedSprite;
    private Sprite originalSprite;
    
    public ImprovementDataSO improvementData;
    public UnitBuildDataSO unitData;

    private UIResearchItem researchItem;
    private List<List<ResourceValue>> consumes = new();
    private List<ResourceValue> produces = new();
    private List<int> produceTime = new();

    //private UITooltipTrigger tooltipInfo;

    private void Awake()
    {
        researchItem = GetComponentInParent<UIResearchItem>();
        if (improvementData != null)
        {
            consumes.Add(improvementData.consumedResources);
            consumes.Add(improvementData.consumedResources1);
            consumes.Add(improvementData.consumedResources2);
            consumes.Add(improvementData.consumedResources3);
            consumes.Add(improvementData.consumedResources4);
            produces = improvementData.producedResources;
            produceTime = improvementData.producedResourceTime;
        }

        //tooltipInfo = GetComponent<UITooltipTrigger>();
        //if (improvementData != null)
        //{
        //    tooltipInfo.title = improvementData.improvementName;
        //    tooltipInfo.displayTitle = improvementData.improvementDisplayName;
        //    tooltipInfo.level = improvementData.improvementLevel;
        //    tooltipInfo.workEthic = improvementData.workEthicChange;
        //    tooltipInfo.produces = improvementData.producedResources;
        //    tooltipInfo.consumes.Add(improvementData.consumedResources);
        //    tooltipInfo.consumes.Add(improvementData.consumedResources1);
        //    tooltipInfo.consumes.Add(improvementData.consumedResources2);
        //    tooltipInfo.consumes.Add(improvementData.consumedResources3);
        //    tooltipInfo.consumes.Add(improvementData.consumedResources4);
        //    tooltipInfo.produceTime = improvementData.producedResourceTime;
        //    tooltipInfo.costs = improvementData.improvementCost;
        //    tooltipInfo.unit = false;
        //    tooltipInfo.description = improvementData.improvementDescription;
        //}
        //else if (unitData != null)
        //{
        //    tooltipInfo.title = unitData.unitType.ToString();
        //    tooltipInfo.displayTitle = unitData.unitName;
        //    tooltipInfo.level = unitData.unitLevel;
        //    tooltipInfo.description = unitData.unitDescription;
        //    tooltipInfo.health = unitData.health;
        //    tooltipInfo.speed = unitData.movementSpeed;
        //    tooltipInfo.strength = unitData.attackStrength;
        //    tooltipInfo.cargo = unitData.cargoCapacity;
        //    tooltipInfo.costs = unitData.unitCost;
        //    tooltipInfo.unit = true;
        //}

        if (improvementData != null)
            rewardIcon.sprite = improvementData.image;
        else if (unitData != null)
            rewardIcon.sprite = unitData.image;

        originalSprite = rewardBackground.sprite;
    }

    public void Select()
    {
        rewardBackground.sprite = selectedSprite;
    }

    public void Unselect()
    {
        rewardBackground.sprite = originalSprite;
    }

    public void Complete(Color completeColor)
    {
        rewardBackground.sprite = originalSprite;
        rewardBackground.color = completeColor;
    }

    public void OnClick()
    {
        if (!researchItem.researchTree.activeStatus)
            return;

        if (improvementData != null)
            researchItem.researchTree.researchTooltip.SetInfo(transform.position, improvementData.image, improvementData.improvementName, improvementData.improvementDisplayName, improvementData.improvementLevel, 
                improvementData.workEthicChange, improvementData.improvementDescription, improvementData.improvementCost, produces, consumes, produceTime, false, 0, 0, 0, 0);
        else if (unitData != null)
            researchItem.researchTree.researchTooltip.SetInfo(transform.position, unitData.image, unitData.unitType.ToString(), unitData.unitName, unitData.unitLevel, 0, unitData.unitDescription, unitData.unitCost, 
                produces, consumes, produceTime, true, unitData.health, unitData.movementSpeed, unitData.baseAttackStrength, unitData.cargoCapacity);

        researchItem.researchTree.researchTooltip.ToggleVisibility(true);
    }
}
