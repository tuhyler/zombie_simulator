using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIResearchReward : MonoBehaviour
{
    [SerializeField]
    private Image rewardIcon;
    
    public ImprovementDataSO improvementData;
    public UnitBuildDataSO unitData;

    private void Awake()
    {
        if (improvementData != null)
            rewardIcon.sprite = improvementData.image;
        else if (unitData != null)
            rewardIcon.sprite = unitData.image;
    }
}
