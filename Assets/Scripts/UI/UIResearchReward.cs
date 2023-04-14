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
            tooltipInfo.level = improvementData.improvementLevel;
            tooltipInfo.workEthic = improvementData.workEthicChange;
            tooltipInfo.produces = improvementData.producedResources;
            tooltipInfo.consumes.Add(improvementData.consumedResources);
            tooltipInfo.consumes.Add(improvementData.consumedResources1);
            tooltipInfo.consumes.Add(improvementData.consumedResources2);
            tooltipInfo.consumes.Add(improvementData.consumedResources3);
            tooltipInfo.consumes.Add(improvementData.consumedResources4);
            tooltipInfo.costs = improvementData.improvementCost;
            tooltipInfo.unit = false;
        }
        else if (unitData != null)
        {
            tooltipInfo.title = unitData.unitName;
            tooltipInfo.description = unitData.unitDescription;
            tooltipInfo.unit = true;
        }

        if (improvementData != null)
            rewardIcon.sprite = improvementData.littleImage;
        else if (unitData != null)
            rewardIcon.sprite = unitData.image;
    }
}
