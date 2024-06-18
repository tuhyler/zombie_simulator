using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIResources : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField]
    public TMP_Text resourceAmount;
    [SerializeField]
    public Image resourceImage, background; 
    [SerializeField]
    public ResourceType resourceType;

    //for moving panels on resource grid
    [HideInInspector]
    public int resourceValue, loc; 
    [HideInInspector]
    public Transform originalParent;
    public Transform tempParent;

    private UIResourceManager resourceManager;

	private void Awake()
	{
		resourceAmount.outlineColor = Color.black;
		resourceAmount.outlineWidth = .2f;
	}

	public void SetResourceManager(UIResourceManager resourceManager)
    {
        this.resourceManager = resourceManager;
    }

    public void SetValue(int val)
    {
        resourceValue = val;

		if (val < 1000)
		{
			resourceAmount.text = val.ToString();
		}
		else if (val < 1000000)
		{
			resourceAmount.text = Math.Round(val * 0.001f, 1) + "k";
		}
		else if (val < 1000000000)
		{
			resourceAmount.text = Math.Round(val * 0.000001f, 1) + "M";
		}
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
		if (eventData.button == PointerEventData.InputButton.Left)
        {
		    resourceManager.dragging = true;
            originalParent = transform.parent;
            transform.SetParent(tempParent);
            transform.SetAsLastSibling();
            background.raycastTarget = false;
            resourceManager.cityBuilderManager.PlaySelectAudio(resourceManager.cityBuilderManager.pickUpClip);
        }
     }

    public void OnDrag(PointerEventData eventData)
    {
		if (eventData.button == PointerEventData.InputButton.Left)
        {
		    Vector3 p = Input.mousePosition;
            p.z = 935;
            Vector3 pos = Camera.main.ScreenToWorldPoint(p);
            transform.position = pos;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
		if (eventData.button == PointerEventData.InputButton.Left)
        {
		    resourceManager.dragging = false;
            transform.SetParent(originalParent);
            transform.localPosition = Vector3.zero;
            background.raycastTarget = true;
		    resourceManager.cityBuilderManager.PlaySelectAudio(resourceManager.cityBuilderManager.putDownClip);
        }
	}
}
