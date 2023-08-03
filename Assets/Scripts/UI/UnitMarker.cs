using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitMarker : MonoBehaviour
{
    private Unit unit;
    public Unit Unit { get { return unit; } set { unit = value; } }

    private bool activeStatus = true;

    private void Awake()
    {
        ToggleVisibility(false);
    }

    private void LateUpdate()
    {
        transform.LookAt(transform.position + Camera.main.transform.rotation * Vector3.forward, Camera.main.transform.rotation * Vector3.up);
    }

    public void ToggleVisibility(bool v)
    {
        if (v == activeStatus)
            return;

        activeStatus = v;
        gameObject.SetActive(v);
    }
}
