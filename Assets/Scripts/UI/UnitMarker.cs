using UnityEngine;

public class UnitMarker : MonoBehaviour
{
    [HideInInspector]
    public Unit unit;

    private bool activeStatus = true;

    private void Awake()
    {
		activeStatus = false;
		gameObject.SetActive(false);
	}

	private void LateUpdate()
    {
        transform.LookAt(transform.position + Camera.main.transform.rotation * Vector3.forward, Camera.main.transform.rotation * Vector3.up);
    }

    public void ToggleVisibility(bool v)
    {
        if (v == activeStatus)
            return;

        unit.outline.ToggleOutline(v);
        //unit.outline.enabled = v;
        activeStatus = v;
        gameObject.SetActive(v);
    }
}
