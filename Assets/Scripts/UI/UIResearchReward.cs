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
    public WonderDataSO wonderData;
    [HideInInspector]
    public List<ResourceType> resourcesUnlocked;

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

			for (int i = 0; i < produces.Count; i++)
			{
                if (produces[i].resourceType == ResourceType.None)
                    continue;
                
				resourcesUnlocked.Add(produces[i].resourceType);
			}
		}
		else if (unitData != null)
        {
            if (unitData.cycleCost.Count > 0)
            {
                ResourceValue value;
                value.resourceType = ResourceType.None;
                value.resourceAmount = 0;
                produces.Add(value);
                produceTime.Add(0);
            }

            consumes.Add(unitData.cycleCost);
        }
        else if (wonderData != null)
        {
            ResourceValue cost;
            cost.resourceType = ResourceType.Gold;
            cost.resourceAmount = wonderData.workerCost * wonderData.workersNeeded;

            ResourceValue labor;
            labor.resourceType = ResourceType.Labor;
            labor.resourceAmount = wonderData.workersNeeded;

            List<ResourceValue> costList = new() { cost, labor };
            consumes.Add(costList);

            ResourceValue value;
			value.resourceType = ResourceType.None;
			value.resourceAmount = 0;
			produces.Add(value);
			produceTime.Add(wonderData.buildTimePerPercent);
        }

        if (improvementData != null)
            rewardIcon.sprite = improvementData.image;
        else if (unitData != null)
            rewardIcon.sprite = unitData.image;
        else if (wonderData != null)
            rewardIcon.sprite = wonderData.image;

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

    public void Complete(Sprite newBackground)
    {
        rewardBackground.sprite = originalSprite;
        rewardBackground.sprite = newBackground;
    }

    public void OnClick()
    {
        if (!researchItem.researchTree.activeStatus)
            return;

        if (improvementData != null)
            researchItem.researchTree.researchTooltip.SetInfo(improvementData.image, improvementData.improvementName, improvementData.improvementDisplayName, improvementData.improvementLevel,
                improvementData.workEthicChange, improvementData.improvementDescription, improvementData.improvementCost, produces, consumes, produceTime, false, 0, 0, 0, 0,
                improvementData.housingIncrease, improvementData.waterIncrease, false, Era.None, improvementData.rawResourceType == RawResourceType.Rocks);
        else if (unitData != null)
            researchItem.researchTree.researchTooltip.SetInfo(unitData.image, unitData.unitType.ToString(), unitData.unitDisplayName, unitData.unitLevel, 0, unitData.unitDescription, unitData.unitCost,
                produces, consumes, produceTime, true, unitData.health, unitData.movementSpeed, unitData.baseAttackStrength, unitData.cargoCapacity, 0, 0, false, Era.None);
        else if (wonderData != null)
            researchItem.researchTree.researchTooltip.SetInfo(wonderData.image, wonderData.wonderName, wonderData.wonderDisplayName, 0, 0, wonderData.wonderDecription, wonderData.wonderCost, produces, 
                consumes, produceTime, false, 0, 0, 0, 0, 0, 0, true, wonderData.wonderEra);

        researchItem.researchTree.researchTooltip.ToggleVisibility(true);
        researchItem.researchTree.world.cityBuilderManager.PlaySelectAudio();
    }
}
