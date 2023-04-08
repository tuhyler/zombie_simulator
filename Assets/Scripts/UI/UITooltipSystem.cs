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

    public static void Show(Vector3 position, string title, int level, float workEthic, string description, List<ResourceValue> costs, List<ResourceValue> produces, List<List<ResourceValue>> consumes, bool unit)
    {
        current.tooltip.SetInfo(position, title, level, workEthic, description, costs, produces, consumes, unit);
        current.tooltip.gameObject.SetActive(true);
    }

    public static void Hide()
    {
        current.tooltip.gameObject.SetActive(false);
    }
}
