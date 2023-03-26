using UnityEngine;
using UnityEngine.UI;

public class UIResearchReward : MonoBehaviour
{
    [SerializeField]
    private Image rewardIcon;
    
    public ImprovementDataSO improvementData;
    public UnitBuildDataSO unitData;

    private UITooltipTrigger tooltipInfo;

    private void Awake()
    {
        tooltipInfo = GetComponent<UITooltipTrigger>();
        if (improvementData != null)
        {
            tooltipInfo.title = improvementData.improvementName;
            tooltipInfo.produces = improvementData.producedResources;
            tooltipInfo.consumes = improvementData.consumedResources;
            tooltipInfo.costs = improvementData.improvementCost;
        }
        else if (unitData != null)
        {
            tooltipInfo.title = unitData.unitName;

        }

        if (improvementData != null)
            rewardIcon.sprite = improvementData.image;
        else if (unitData != null)
            rewardIcon.sprite = unitData.image;
    }
}
