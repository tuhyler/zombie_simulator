using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UIInfoPanelCityWarehouse : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI capacity, used;

    private void Start()
    {
        ToggleVisibility(false);
    }

    public void SetAllWarehouseData(int capacity, float used)
    {
        this.capacity.text = $"Storage Capacity: {capacity}";
        this.used.text = $"Storage Used {used}";
    }

    public void SetWarehouseStorageLevel(float used)
    {
        this.used.text = $"Storage Used {used}";
    }

    public void ToggleVisibility(bool val)
    {
        gameObject.SetActive(val);
    }
}
