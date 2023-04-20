using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UITooltipSystem : MonoBehaviour
{
    private static UITooltipSystem current;
    
    public UITooltip tooltip;

    private void Awake()
    {
        current = this;
    }

    public static void Show(Vector3 position, string title, string displayTitle, int level, float workEthic, string description, List<ResourceValue> costs, List<ResourceValue> produces, 
        List<List<ResourceValue>> consumes, List<int> produceTime, bool unit, int health, float speed, int strength, int cargo)
    {
        current.tooltip.SetInfo(position, title, displayTitle, level, workEthic, description, costs, produces, consumes, produceTime, unit, health, speed, strength, cargo);
        current.tooltip.gameObject.SetActive(true);
    }

    public static void Hide()
    {
        current.tooltip.gameObject.SetActive(false);
    }
}
