using TMPro;
using Unity.VisualScripting;
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

    public void SetResourceManager(UIResourceManager resourceManager)
    {
        this.resourceManager = resourceManager;
    }

    public void SetValue(int val)
    {
        resourceValue = val;
        resourceAmount.text = val.ToString();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        resourceManager.dragging = true;
        originalParent = transform.parent;
        transform.SetParent(tempParent);
        transform.SetAsLastSibling();
        background.raycastTarget = false;
     }

    public void OnDrag(PointerEventData eventData)
    {
        Vector3 p = Input.mousePosition;
        p.z = 1;
        Vector3 pos = Camera.main.ScreenToWorldPoint(p);
        transform.position = pos;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        resourceManager.dragging = false;
        transform.SetParent(originalParent);
        transform.localPosition = Vector3.zero;
        background.raycastTarget = true;
    }
}
