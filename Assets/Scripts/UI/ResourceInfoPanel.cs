using TMPro;
using UnityEngine;

public class ResourceInfoPanel : MonoBehaviour
{
    [SerializeField]
    private SpriteRenderer resourceImageHolder;
    //[SerializeField]
    //private SpriteMask spriteMask;
    [SerializeField]
    private TMP_Text resourceAmount;
    [HideInInspector]
    public int amount;

    //void LateUpdate()
    //{
    //    transform.LookAt(transform.position + Camera.main.transform.rotation * Vector3.forward, Camera.main.transform.rotation * Vector3.up);
    //}

    public void SetResourcePanel(Sprite sprite, int amount, bool hasEnough, bool building)
    {
        resourceImageHolder.sprite = sprite;
        this.amount = amount;
        SetResourcePanelAmount(hasEnough, building);
    }

    public void SetResourcePanelAmount(bool hasEnough, bool building)
    {
        resourceAmount.text = amount.ToString();

        if (building)
        {
		    if (hasEnough)
			    resourceAmount.color = Color.white;
		    else
			    resourceAmount.color = Color.red;
        }
        else
        {
			resourceAmount.color = Color.green;
		}
	}
}
