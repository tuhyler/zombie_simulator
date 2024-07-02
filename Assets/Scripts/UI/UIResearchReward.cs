using System.Collections.Generic;
using System.Text.RegularExpressions;
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
    public UtilityCostSO utilityData;
    [HideInInspector]
    public HashSet<ResourceType> resourcesUnlocked;

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

            if (produces.Count > 0 || consumes.Count > 0 || improvementData.improvementCost.Count > 0)
                resourcesUnlocked = new();

			for (int i = 0; i < produces.Count; i++)
			{
                if (produces[i].resourceType == ResourceType.None)
                    continue;
                
				resourcesUnlocked.Add(produces[i].resourceType);
			}

            for (int i = 0; i < consumes.Count; i++)
            {
                for (int j = 0; j < consumes[i].Count; j++)
                {
                    if (consumes[i][j].resourceType == ResourceType.None)
                        continue;

                    resourcesUnlocked.Add(consumes[i][j].resourceType);
                }
            }

			for (int i = 0; i < improvementData.improvementCost.Count; i++)
			{
				if (improvementData.improvementCost[i].resourceType == ResourceType.None)
					continue;

				resourcesUnlocked.Add(improvementData.improvementCost[i].resourceType);
			}

			rewardIcon.sprite = improvementData.image;
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
            rewardIcon.sprite = unitData.image;

			if (unitData.cycleCost.Count > 0 || unitData.unitCost.Count > 0 || unitData.battleCost.Count > 0)
				resourcesUnlocked = new();

            for (int i = 0; i < unitData.cycleCost.Count; i++)
            {
				if (unitData.cycleCost[i].resourceType == ResourceType.None)
					continue;

				resourcesUnlocked.Add(unitData.cycleCost[i].resourceType);
			}

			for (int i = 0; i < unitData.unitCost.Count; i++)
			{
				if (unitData.unitCost[i].resourceType == ResourceType.None)
					continue;

				resourcesUnlocked.Add(unitData.unitCost[i].resourceType);
			}

			for (int i = 0; i < unitData.battleCost.Count; i++)
			{
				if (unitData.battleCost[i].resourceType == ResourceType.None)
					continue;

				resourcesUnlocked.Add(unitData.battleCost[i].resourceType);
			}
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
            rewardIcon.sprite = wonderData.image;

            if (wonderData.wonderCost.Count > 0)
                resourcesUnlocked = new();

			for (int i = 0; i < wonderData.wonderCost.Count; i++)
			{
				if (wonderData.wonderCost[i].resourceType == ResourceType.None)
					continue;

				resourcesUnlocked.Add(wonderData.wonderCost[i].resourceType);
			}
		}
        else if (utilityData != null)
        {
			if (utilityData.bridgeCost.Count > 0)
			{
				ResourceValue value;
				value.resourceType = ResourceType.None;
				value.resourceAmount = 0;
				produces.Add(value);
				produceTime.Add(0);
			}

			consumes.Add(utilityData.bridgeCost);
            rewardIcon.sprite = utilityData.image;

            if (utilityData.utilityCost.Count > 0 || utilityData.bridgeCost.Count > 0)
                resourcesUnlocked = new();

			for (int i = 0; i < utilityData.utilityCost.Count; i++)
			{
				if (utilityData.utilityCost[i].resourceType == ResourceType.None)
					continue;

				resourcesUnlocked.Add(utilityData.utilityCost[i].resourceType);
			}

			for (int i = 0; i < utilityData.bridgeCost.Count; i++)
			{
				if (utilityData.bridgeCost[i].resourceType == ResourceType.None)
					continue;

				resourcesUnlocked.Add(utilityData.bridgeCost[i].resourceType);
			}
		}

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
        //rewardBackground.sprite = originalSprite;
        rewardBackground.sprite = newBackground;
    }

    public void OnClick()
    {
        if (!researchItem.researchTree.activeStatus)
            return;

        if (improvementData != null)
            researchItem.researchTree.researchTooltip.SetInfo(improvementData.image, improvementData.improvementName, improvementData.improvementDisplayName, improvementData.improvementLevel,
                improvementData.workEthicChange, improvementData.improvementDescription, improvementData.improvementCost, produces, consumes, produceTime, false, 0, 0, 0, 0,
                improvementData.housingIncrease, improvementData.waterIncrease, improvementData.powerIncrease, improvementData.purchaseAmountChange, false, Era.None, false, 
                improvementData.rawResourceType == RawResourceType.Rocks, improvementData.cityBonus);
        else if (unitData != null)
            researchItem.researchTree.researchTooltip.SetInfo(unitData.image, Regex.Replace(unitData.unitType.ToString(), "((?<=[a-z])[A-Z]|[A-Z](?=[a-z]))", " $1"), unitData.unitDisplayName, 
                unitData.unitLevel, 0, unitData.unitDescription, unitData.unitCost, produces, consumes, produceTime, true, unitData.health, unitData.movementSpeed, unitData.baseAttackStrength, 
                unitData.cargoCapacity, 0, 0, 0, 0, false, Era.None, false);
        else if (wonderData != null)
            researchItem.researchTree.researchTooltip.SetInfo(wonderData.image, wonderData.wonderName, wonderData.wonderDisplayName, 0, 0, wonderData.wonderDescription, wonderData.wonderCost, produces, 
                consumes, produceTime, false, 0, 0, 0, 0, 0, 0, 0, 0, true, wonderData.wonderEra, false);
        else if (utilityData != null)
			researchItem.researchTree.researchTooltip.SetInfo(utilityData.image, utilityData.utilityName, utilityData.utilityDisplayName, utilityData.utilityLevel, 0, utilityData.utilityDescription, 
                utilityData.utilityCost, produces, consumes, produceTime, false, 0, 0, 0, 0, 0, 0, 0, 0, false, Era.None, true);

		researchItem.researchTree.researchTooltip.ToggleVisibility(true);
        researchItem.researchTree.world.cityBuilderManager.PlaySelectAudio();
	}
}
